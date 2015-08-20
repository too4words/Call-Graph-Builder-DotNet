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

		public async Task AnalyzeAsync(IEnumerable<MethodDescriptor> rootMethods, IEnumerable<PropGraphNodeDescriptor> reworkSet = null)
		{
			if(reworkSet==null)
			{
				reworkSet = new HashSet<PropGraphNodeDescriptor>();
			}

			foreach (var method in rootMethods)
			{
				var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(method);
				var propagationEffects = await methodEntityProc.PropagateAsync(PropagationKind.ADD_TYPES, reworkSet);

				await PropagateEffectsAsync(propagationEffects, PropagationKind.ADD_TYPES);
			}

			await ProcessMessages();
		}

        public Task AnalyzeAsync(MethodDescriptor method, IEnumerable<PropGraphNodeDescriptor> reworkSet = null)
		{
			Logger.Instance.Log("AnalysisOrchestator", "AnalyzeAsync", "Analyzing {0} ", method);
			return AnalyzeAsync(new MethodDescriptor[] { method }, reworkSet);
		}


		public async Task RemoveMethodAsync(MethodDescriptor method, string newSource)
		{
			var projectProvider = await this.solutionManager.GetProjectCodeProviderAsync(method);

			await ((BaseProjectCodeProvider)projectProvider).ReplaceSourceAsync(newSource, "MyFile.cs");

			var propagationEffects = await projectProvider.RemoveMethodAsync(method);

			await this.PropagateEffectsAsync(propagationEffects, PropagationKind.REMOVE_TYPES);
			await this.ProcessMessages();
			await this.UnregisterCallerAsync(propagationEffects.CalleesInfo);
			// await this.UnregisterCallee(propagationEffects.CallersInfo);
		}

		private async Task UnregisterCallerAsync(IEnumerable<CallInfo> calleesInfo)
		{
			var tasks = new List<Task>();

			foreach (var calleeInfo in calleesInfo)
			{
				foreach (var callee in calleeInfo.PossibleCallees)
				{
					var calleeEntityProc = await this.solutionManager.GetMethodEntityAsync(callee);
					var callContext = new CallContext(calleeInfo.Caller, calleeInfo.LHS, calleeInfo.CallNode);
					var task = calleeEntityProc.UnregisterCallerAsync(callContext);
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

			await ((BaseProjectCodeProvider)projectProvider).ReplaceSourceAsync(newSource, "MyFile.cs");

			var propagationDeleteEffects = await projectProvider.RemoveMethodAsync(method);

			await this.PropagateEffectsAsync(propagationDeleteEffects, PropagationKind.REMOVE_TYPES);
			await this.ProcessMessages();
			await this.UnregisterCallerAsync(propagationDeleteEffects.CalleesInfo);

			await AddMethodAsync(method, newSource);

			foreach(var callerInfo in propagationDeleteEffects.CallersInfo)
			{
				var callContex = callerInfo.CallerContext;
				var reworkSet = new HashSet<PropGraphNodeDescriptor>();
				reworkSet.Add(callContex.CallNode);
				await AnalyzeAsync(callContex.Caller, reworkSet);
			}
			// await this.UnregisterCallee(propagationEffects.CallersInfo);
		}

		public async Task AddMethodAsync(MethodDescriptor method, string newSource)
		{
			var projectProvider = await this.solutionManager.GetProjectCodeProviderAsync(method);

			await ((BaseProjectCodeProvider)projectProvider).ReplaceSourceAsync(newSource, "MyFile.cs");
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
			Logger.Instance.Log("AnalysisOrchestator", "DoPropagationOfEffects", "");

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
			Logger.Instance.Log("AnalysisOrchestator", "AnalyzeCalleeAsync", "Analyzing call to {0} ", callee);

			var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(callee);
            var propagationEffects = await methodEntityProc.PropagateAsync(callerMessage.CallMessageInfo);
			await this.PropagateEffectsAsync(propagationEffects, propKind);

            Logger.Instance.Log("AnalysisOrchestator", "AnalyzeCalleeAsync", "End Analyzing call to {0} ", callee);
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
			Logger.Instance.Log("AnalysisOrchestator", "AnalyzeReturnAsync", "Analyzing return to {0} ", caller);

			var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(caller);
			var propagationEffects = await methodEntityProc.PropagateAsync(calleeMessage.ReturnMessageInfo);
			await this.PropagateEffectsAsync(propagationEffects, propKind);

            Logger.Instance.Log("AnalysisOrchestator", "AnalyzeReturnAsync", "End Analyzing return to {0} ", caller);
		}
    }
}
