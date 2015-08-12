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
		private IAnalysisStrategy strategy;
        //private ISet<Message> messageWorkList = new HashSet<Message>();
        private Queue<Message> messageWorkList = new Queue<Message>();
        private IDictionary<MethodDescriptor, MethodEntity> anonymousMethods;

		public MethodEntity GetAnonymousMethodEntity(MethodDescriptor methodDescriptor)
        {
            return anonymousMethods[methodDescriptor];
        }

		public AnalysisOrchestator(IAnalysisStrategy strategy)
		{
			this.strategy = strategy;
			this.anonymousMethods = new Dictionary<MethodDescriptor, MethodEntity>();
		}

		public async Task AnalyzeAsync(IEnumerable<MethodDescriptor> rootMethods)
		{
			foreach (var methodDescriptor in rootMethods)
			{
				var entityDescriptor = new MethodEntityDescriptor(methodDescriptor);
				var methodEntityProc = await strategy.GetMethodEntityAsync(methodDescriptor);
				var propagationEffects = await methodEntityProc.PropagateAsync(PropagationKind.ADD_TYPES);
				await PropagateEffectsAsync(propagationEffects, PropagationKind.ADD_TYPES);
			}

			await ProcessMessages();
		}

        public async Task AnalyzeAsync(MethodDescriptor method)
		{
			Logger.Instance.Log("AnalysisOrchestator", "AnalyzeAsync", "Analyzing {0} ", method);

			var entityDescriptor = new MethodEntityDescriptor(method);
			var methodEntityProc = await strategy.GetMethodEntityAsync(method);
            var propagationEffects = await methodEntityProc.PropagateAsync(PropagationKind.ADD_TYPES);
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
					await AnalyzeCalleeAsync(callerMessageInfo.Callee, callerMessage, callerMessageInfo.PropagationKind);
				}
				else if (message is CalleeMessage)
				{
					var calleeMessage = (CalleeMessage)message;
					await AnalyzeReturnAsync(calleeMessage.ReturnMessageInfo.Caller, calleeMessage, calleeMessage.ReturnMessageInfo.PropagationKind);
				}
			}
		}

		private async Task PropagateEffectsAsync(PropagationEffects propagationEffects, PropagationKind propKind)
		{
			Logger.Instance.Log("AnalysisOrchestator", "DoPropagationOfEffects", "");

			await ProcessCalleesAsync(propagationEffects.CalleesInfo, propKind);

			if (propagationEffects.ResultChanged)
			{
				await ProcessReturnAsync(propagationEffects.CallersInfo, propKind);
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
                    
					var task = DispatchCallMessageForMethodCallAsync(calleeInfo as MethodCallInfo, propKind);
					//await task;
					tasks.Add(task);
				}
				else if (calleeInfo is DelegateCallInfo)
				{
					var task = DispatchCallMessageForDelegateCallAsync(calleeInfo as DelegateCallInfo, propKind);
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
				var task = CreateAndSendCallMessageAsync(methodCallInfo, callee, propKind);
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
				var task = CreateAndSendCallMessageAsync(delegateCallInfo, callee, propKind);
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

			var methodEntityProc = await strategy.GetMethodEntityAsync(callee);
            var propagationEffects = await methodEntityProc.PropagateAsync(callerMessage.CallMessageInfo);
            await PropagateEffectsAsync(propagationEffects, PropagationKind.ADD_TYPES);

            Logger.Instance.Log("AnalysisOrchestator", "AnalyzeCalleeAsync", "End Analyzing call to {0} ", callee);
		}

		private async Task ProcessReturnAsync(IEnumerable<ReturnInfo> callersInfo, PropagationKind propKind)
		{
			var tasks = new List<Task>();

			foreach (var callerInfo in callersInfo)
			{
				var task = DispachReturnMessageAsync(callerInfo, propKind);
				//await task;
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
		}

		private Task DispachReturnMessageAsync(ReturnInfo returnInfo, PropagationKind propKind)
		{
			return CreateAndSendReturnMessageAsync(returnInfo, propKind);
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

			var methodEntityProc = await strategy.GetMethodEntityAsync(caller);
			var propagationEffects = await methodEntityProc.PropagateAsync(calleeMessage.ReturnMessageInfo);
			await PropagateEffectsAsync(propagationEffects, propKind);

            Logger.Instance.Log("AnalysisOrchestator", "AnalyzeReturnAsync", "End Analyzing return to {0} ", caller);
		}
    }

}
