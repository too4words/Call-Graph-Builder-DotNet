// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using OrleansInterfaces;
using ReachingTypeAnalysis.Communication;
using SolutionTraversal.CallGraph;
using System.Linq;
using Orleans;
using System.Diagnostics;

namespace ReachingTypeAnalysis.Analysis
{
    /// <summary>
    /// This class is a prototype version of the propagation analysis avoiding the rentrancy in grains.
    /// All methods are static.
    /// Essentially, now the MethodGrain only performs the Propagation within its graph but doesn't trigger the 
    /// propagation to other methods. This is done by this orchestator
    /// This version of the prototype "flattens" the OrleansDispacther and MethodEntityProcessor and make extensive use of await
    /// This should be improved in next versions.
    /// </summary>
    internal class AnalysisOrchestator
	{
		private ISolutionManager solutionManager;
		//private ISet<Message> messageWorkList = new HashSet<Message>();
		private Queue<Message> messageWorkList = new Queue<Message>();

		public AnalysisOrchestator(ISolutionManager solutionManager)
		{
			this.solutionManager = solutionManager;
		}

		public async Task AnalyzeAsync(IEnumerable<MethodDescriptor> rootMethods)
		{
			foreach (var method in rootMethods)
			{
				var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(method);
				var propagationEffects = await methodEntityProc.PropagateAsync(PropagationKind.ADD_TYPES);

				await PropagateEffectsAsync(propagationEffects, PropagationKind.ADD_TYPES);
			}

			await ProcessMessages();
		}

        public async Task AnalyzeAsync(MethodDescriptor method, IEnumerable<PropGraphNodeDescriptor> reworkSet = null)
		{
			Logger.LogS("AnalysisOrchestator", "AnalyzeAsync", "Analyzing {0} ", method);
			if (reworkSet == null)
			{
				reworkSet = new HashSet<PropGraphNodeDescriptor>();
			}
			var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(method);
			var propagationEffects = await methodEntityProc.PropagateAsync(PropagationKind.ADD_TYPES, reworkSet);
			await PropagateEffectsAsync(propagationEffects, PropagationKind.ADD_TYPES);
			await ProcessMessages();
		}

		private async Task ProcessMessages()
		{
			while (messageWorkList.Count > 0)
			{
				var message = messageWorkList.Dequeue();

				if (message is CallerMessage)
				{
					var callerMessage = (CallerMessage)message;
					var callerMessageInfo = callerMessage.CallMessageInfo;
					await this.AnalyzeCalleeAsync(callerMessageInfo.Callee, callerMessage, callerMessageInfo.PropagationKind);
				}
				else if (message is CalleeMessage)
				{
					var calleeMessage = (CalleeMessage)message;
					await this.AnalyzeReturnAsync(calleeMessage.ReturnMessageInfo.Caller, calleeMessage, calleeMessage.ReturnMessageInfo.PropagationKind);
				}
			}
		}

		private async Task PropagateEffectsAsync(PropagationEffects propagationEffects, PropagationKind propKind)
		{
			Logger.LogS("AnalysisOrchestator", "DoPropagationOfEffects", "");

			await this.ProcessCalleesAsync(propagationEffects.CalleesInfo, propKind);

			if (propagationEffects.ResultChanged)
			{
				await this.ProcessReturnAsync(propagationEffects.CallersInfo, propKind);
			}
		}

		private async Task ProcessCalleesAsync(IEnumerable<CallInfo> calleesInfo, PropagationKind propKind)
		{
			var tasks = new List<Task>();

			foreach (var calleeInfo in calleesInfo)
			{
				//TODO: Need to Refactor to get rid of this ifs!
				if (calleeInfo is MethodCallInfo)
				{
					// TODO: This should work in any order because the technique is flow insensitive 
					// But actually for some reason the ordering is affecting the result
					// I think the problem is the conservative treatment when types(receiver).Count = 0 
					// (FIXED??)
                    
					var task = this.DispatchCallMessageForMethodCallAsync(calleeInfo as MethodCallInfo, propKind);
					//await task;
					tasks.Add(task);
				}
				else if (calleeInfo is DelegateCallInfo)
				{
					var task = this.DispatchCallMessageForDelegateCallAsync(calleeInfo as DelegateCallInfo, propKind);
					//await task;
					tasks.Add(task);
				}
			}

			await Task.WhenAll(tasks);
		}

		private async Task DispatchCallMessageForMethodCallAsync(MethodCallInfo methodCallInfo, PropagationKind propKind)
		{
			var tasks = new List<Task>();

			foreach (var callee in methodCallInfo.PossibleCallees)
			{
				var task = this.CreateAndSendCallMessageAsync(methodCallInfo, callee, propKind);
				//await task;
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
		}

		private async Task DispatchCallMessageForDelegateCallAsync(DelegateCallInfo delegateCallInfo, PropagationKind propKind)
		{
			var tasks = new List<Task>();

			foreach (var callee in delegateCallInfo.PossibleCallees)
			{
				var task = this.CreateAndSendCallMessageAsync(delegateCallInfo, callee, propKind);
				//await task;
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
		}

		private Task CreateAndSendCallMessageAsync(CallInfo callInfo, MethodDescriptor callee, PropagationKind propKind)
		{
			var callMessageInfo = new CallMessageInfo(callInfo.Caller, callee, callInfo.ReceiverPossibleTypes,
				callInfo.ArgumentsPossibleTypes, callInfo.InstantiatedTypes, callInfo.CallNode, callInfo.LHS, propKind);

			var source = new MethodEntityDescriptor(callInfo.Caller);
			var callerMessage = new CallerMessage(source, callMessageInfo);
            this.messageWorkList.Enqueue(callerMessage);
            //this.messageWorkList.Add(callerMessage);
            return TaskDone.Done;
			//return AnalyzeCalleeAsync(callMessageInfo.Callee, callerMessage, propKind);
		}

		/// <summary>
		/// This method "replaces" the send + dispatch + processCallMessage for calless that used the methodProcessor and dispatcher
		/// The idea is trying to avoid reentrant calls to grains
		/// We wil need to improve the code / design but this is a first approach to solve that problem
		/// </summary>
		/// <param name="callee"></param>
		/// <param name="callerMessage"></param>
		/// <param name="propKind"></param>
		/// <returns></returns>
		private async Task AnalyzeCalleeAsync(MethodDescriptor callee, CallerMessage callerMessage, PropagationKind propKind)
		{
			Logger.LogS("AnalysisOrchestator", "AnalyzeCalleeAsync", "Analyzing call to {0} ", callee);

			var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(callee);
            var propagationEffects = await methodEntityProc.PropagateAsync(callerMessage.CallMessageInfo);
			await this.PropagateEffectsAsync(propagationEffects, propKind);

            Logger.LogS("AnalysisOrchestator", "AnalyzeCalleeAsync", "End Analyzing call to {0} ", callee);
		}

		private async Task ProcessReturnAsync(IEnumerable<ReturnInfo> callersInfo, PropagationKind propKind)
		{
			var tasks = new List<Task>();

			foreach (var callerInfo in callersInfo)
			{
				var task = this.DispachReturnMessageAsync(callerInfo, propKind);
				//await task;
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
		}

		private Task DispachReturnMessageAsync(ReturnInfo returnInfo, PropagationKind propKind)
		{
			return this.CreateAndSendReturnMessageAsync(returnInfo, propKind);
        }

		private Task CreateAndSendReturnMessageAsync(ReturnInfo returnInfo, PropagationKind propKind)
		{
			var returnMessageInfo = new ReturnMessageInfo(returnInfo.CallerContext.Caller, returnInfo.Callee, returnInfo.ResultPossibleTypes, returnInfo.InstantiatedTypes,
				returnInfo.CallerContext.CallNode, returnInfo.CallerContext.LHS, propKind);

			var source = new MethodEntityDescriptor(returnInfo.Callee);
			var calleeMessage = new CalleeMessage(source, returnMessageInfo);
            this.messageWorkList.Enqueue(calleeMessage);
            //this.messageWorkList.Add(calleeMessage);
            return TaskDone.Done;
			//return AnalyzeReturnAsync(returnMessageInfo.Caller, calleeMessage, propKind);
		}

		/// <summary>
		/// This method "replaces" the send + dispatch + processReturnMessage for calless that used the methodProcessor and dispatcher
		/// The idea is trying to avoid reentrant calls to grains
		/// We wil need to improve the code / design but this is a first approach to solve that problem
		/// </summary>
		/// <param name="caller"></param>
		/// <param name="calleeMessage"></param>
		/// <param name="propKind"></param>
		/// <returns></returns>
		private async Task AnalyzeReturnAsync(MethodDescriptor caller, CalleeMessage calleeMessage, PropagationKind propKind)
		{
			Logger.LogS("AnalysisOrchestator", "AnalyzeReturnAsync", "Analyzing return to {0} ", caller);

			var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(caller);
			var propagationEffects = await methodEntityProc.PropagateAsync(calleeMessage.ReturnMessageInfo);
			await this.PropagateEffectsAsync(propagationEffects, propKind);

            Logger.LogS("AnalysisOrchestator", "AnalyzeReturnAsync", "End Analyzing return to {0} ", caller);
		}

		#region Incremental Analysis

		public async Task RemoveMethodAsync(MethodDescriptor method, string newSource)
		{
			var projectProvider = await this.solutionManager.GetProjectCodeProviderAsync(method);

			var propagationEffects = await projectProvider.RemoveMethodAsync(method);


			await this.PropagateEffectsAsync(propagationEffects, PropagationKind.REMOVE_TYPES);
			await this.ProcessMessages();
			await this.UnregisterCallerAsync(propagationEffects.CalleesInfo);

			await projectProvider.ReplaceDocumentSourceAsync(newSource, TestConstants.DocumentPath);

			// TODO: Is this necessary? 
			// await this.PropagateFromCallersAsync(propagationEffects.CallersInfo);

			// await this.UnregisterCallee(propagationEffects.CallersInfo);
		}

		private async Task UnregisterCallerAsync(IEnumerable<CallInfo> calleesInfo)
		{
			var tasks = new List<Task>();

			foreach (var calleeInfo in calleesInfo)
			{
				foreach (var callee in calleeInfo.PossibleCallees)
				{
					var calleeEntity = await this.solutionManager.GetMethodEntityAsync(callee);
					var callContext = new CallContext(calleeInfo.Caller, calleeInfo.LHS, calleeInfo.CallNode);
					var task = calleeEntity.UnregisterCallerAsync(callContext);
					//await task;
					tasks.Add(task);
				}
			}

			await Task.WhenAll(tasks);
		}

		//private async Task UnregisterCallee(IEnumerable<ReturnInfo> callersInfo)
		//{
		//    var tasks = new List<Task>();

		//    foreach (var callerInfo in callersInfo)
		//    {
		//        var callerMethodEntity = await this.solutionManager.GetMethodEntityAsync(callerInfo.CallerContext.Caller);
		//        var task = callerMethodEntity.UnregisterCalleeAsync(callerInfo.CallerContext);
		//        //await task;
		//        tasks.Add(task);
		//    }

		//    await Task.WhenAll(tasks);
		//}

		public async Task UpdateMethodAsync(MethodDescriptor method, string newSource)
		{
			var projectProvider = await this.solutionManager.GetProjectCodeProviderAsync(method);
			
			var propagationEffects = await projectProvider.RemoveMethodAsync(method);

			await this.PropagateEffectsAsync(propagationEffects, PropagationKind.REMOVE_TYPES);
			await this.ProcessMessages();
			await this.UnregisterCallerAsync(propagationEffects.CalleesInfo);

			await projectProvider.ReplaceDocumentSourceAsync(newSource, TestConstants.DocumentPath);

			// TODO: if there is no caller (e.g., main in the future public method) you should call yourself

			await this.PropagateFromCallersAsync(propagationEffects.CallersInfo);


			await this.AddMethodAsync(method, newSource);

			
			// await this.UnregisterCallee(propagationEffects.CallersInfo);
		}

		private async Task PropagateFromCallersAsync(IEnumerable<ReturnInfo> callersInfo)
		{
			var tasks = new List<Task>();

			foreach (var callerInfo in callersInfo)
			{
				var reworkSet = new HashSet<PropGraphNodeDescriptor>();
				var callContext = callerInfo.CallerContext;
				reworkSet.Add(callContext.CallNode);
				var task = this.AnalyzeAsync(callContext.Caller, reworkSet);
				//await task;
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
		}

		public async Task AddMethodAsync(MethodDescriptor method, string newSource)
		{
			var projectProvider = await this.solutionManager.GetProjectCodeProviderAsync(method);

			await projectProvider.ReplaceDocumentSourceAsync(newSource, TestConstants.DocumentPath);

			// TODO: is the method is an overrride reanalize callers of *base* method
			// For the case of overload we need to detect the callers of all possible overloads for all "compatible" types
		}

		public async Task ApplyModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			/*
			 * 1) Compute diffs
			 *		In every code provider compute diff (optionally keep a copy of compilation and semantic models). Do not replace old compilation and models
			 *		Return list of method descriptors and action (or 3 lists, remove, update, add)
			 * 2) Perfom propagation
			 *		Start from Deletes (and updates seeing as delete)
			 *		a) For each method m: compute propagation effects (call method entity remove)
			 *			Enqueue as Remove_Types
			 *			Process all together
			 *		b) Swtich all providers to their new verions
			 *			Replace compilation and semantic models in providers 
			 *			Replace solution (in grain and manager)
			 *		c) finalize remove code (propagate from callers)
			 *		Now propagate Adds	
			*/

			var calleesInfo = new List<CallInfo>();
			var callersInfo = new List<ReturnInfo>();
			var modifications = await this.solutionManager.GetModificationsAsync(modifiedDocuments);

			var methodsRemoved = from m in modifications
								 where m.ModificationKind == ModificationKind.MethodRemoved ||
									   m.ModificationKind == ModificationKind.MethodUpdated
								 select m.MethodDescriptor;

			var methodsAdded = from m in modifications
							   where m.ModificationKind == ModificationKind.MethodAdded ||
									 m.ModificationKind == ModificationKind.MethodUpdated
							   select m.MethodDescriptor;

			foreach (var method in methodsRemoved)
			{
				var projectProvider = await this.solutionManager.GetProjectCodeProviderAsync(method);
				var propagationEffects = await projectProvider.RemoveMethodAsync(method);

				calleesInfo.AddRange(propagationEffects.CalleesInfo);
				callersInfo.AddRange(propagationEffects.CallersInfo);

				await this.PropagateEffectsAsync(propagationEffects, PropagationKind.REMOVE_TYPES);
			}

			await this.ProcessMessages();
			await this.UnregisterCallerAsync(calleesInfo);
			await this.solutionManager.ReloadAsync();
			await this.PropagateFromCallersAsync(callersInfo);

			foreach (var method in methodsAdded)
			{
				// TODO: is the method is an overrride reanalyze callers of *base* method
				// For the case of overload we need to detect the callers of all possible overloads for all "compatible" types
			}
		}

		#endregion
	}
}
