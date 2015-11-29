// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using OrleansInterfaces;
using ReachingTypeAnalysis.Communication;
using System.Linq;
using Orleans;
using System.Diagnostics;
using System;
using System.Collections.Concurrent;

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
	internal class SiloOrchestrator
	{
        public static SiloOrchestrator Instance = new SiloOrchestrator();

        //public static SiloOrchestrator CreateInstance()
        //{
        //    if (instance == null)
        //    {
        //        instance = new SiloOrchestrator(solutionManager);
        //    }
        //    return instance;
        //}


        //public static SiloOrchestrator CreateInstance(ISolutionGrain solutionManager)
        //{
        //    if(instance==null)
        //    {
        //        instance = new SiloOrchestrator(solutionManager);
        //    }
        //    return instance;
        //}

        public ISolutionGrain solutionManager { get; set; }
		//private ISet<Message> messageWorkList;
		//private Queue<Message> messageWorkList;
		private ConcurrentQueue<Message> messageWorkList;
        public SiloOrchestrator()
        {
            this.messageWorkList = new ConcurrentQueue<Message>();
        }

        public SiloOrchestrator(ISolutionGrain solutionManager)
		{
			this.solutionManager = solutionManager;
			//this.messageWorkList = new HashSet<Message>();
			//this.messageWorkList = new Queue<Message>();
			this.messageWorkList = new ConcurrentQueue<Message>();
		}

        public async Task AnalyzeAsync(IEnumerable<MethodDescriptor> rootMethods)
		{
			foreach (var method in rootMethods)
			{
				//Logger.LogInfo(GrainClient.Logger, "Orchestrator", "AnalyzeAsync", "Analyzing: {0}", method);
		
				var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(method);

				await methodEntityProc.UseDeclaredTypesForParameters();

				var propagationEffects = await methodEntityProc.PropagateAsync(PropagationKind.ADD_TYPES);

				await this.PropagateEffectsAsync(propagationEffects, PropagationKind.ADD_TYPES, methodEntityProc);
			}
            await this.ProcessMessages();

		}

		internal async Task ProcessMessages()
		{
			// TODO: This version is much faster, works well with orleans but there are some data races for the async case
			var tasks = new List<Task>();

			while (messageWorkList.Count > 0)
			{
				while (messageWorkList.Count > 0)
				{
					//var message = messageWorkList.Dequeue();
					Message message = null;

					if (messageWorkList.TryDequeue(out message))
					{
                        await this.solutionManager.UpdateCounter(-1);
						Logger.LogWarning(Logger.OrleansLogger, "SiloOrchestrator", "ProcessMessage", "Deqeued: {0} Count: {1}", message, messageWorkList.Count);
                        //Logger.LogS("Orchestrator", "ProcessMessage", "Deqeued: {0} Count: {1}", message, messageWorkList.Count);


						if (message is CallerMessage)
						{
							var callerMessage = (CallerMessage)message;
							var callerMessageInfo = callerMessage.CallMessageInfo;
							var task = this.AnalyzeCalleeAsync(callerMessageInfo.Callee, callerMessage, callerMessageInfo.PropagationKind);
							//await task;
							tasks.Add(task);
						}
						else if (message is CalleeMessage)
						{
							var calleeMessage = (CalleeMessage)message;
							var calleeMessageInfo = calleeMessage.ReturnMessageInfo;
                            var task = this.AnalyzeReturnAsync(calleeMessageInfo.Caller, calleeMessage, calleeMessageInfo.PropagationKind);
							//await task;
							tasks.Add(task);
						}
					}
				}

				await Task.WhenAll(tasks);
			}
		}

		internal async Task PropagateEffectsAsync(PropagationEffects propagationEffects, PropagationKind propKind, IMethodEntityWithPropagator methodEntityProp = null)
		{
		    Logger.LogInfo(Logger.OrleansLogger, "SiloOrchestrator", "PropagatEffFects", "Propagating effets computed in {0}", propagationEffects.SiloAddress);

			await this.ProcessCalleesAsync(propagationEffects.CalleesInfo, propKind);

			if (propagationEffects.ResultChanged)
			{
				await this.ProcessReturnAsync(propagationEffects.CallersInfo, propKind);
			}
            ProcessMessages();
		}

		private async Task ProcessCalleesAsync(IEnumerable<CallInfo> calleesInfo, PropagationKind propKind)
		{
			var tasks = new List<Task>();

			foreach (var calleeInfo in calleesInfo)
			{
				//TODO: Need to Refactor to get rid of this ifs!
				if (calleeInfo is MethodCallInfo)
				{
					var task = this.DispatchCallMessageForMethodCallAsync(calleeInfo as MethodCallInfo, propKind);
					//await task;
					tasks.Add(task);
				}
				else if (calleeInfo is DelegateCallInfo)
				{
					var task = this.DispatchCallMessageForDelegateCallAsync(calleeInfo as DelegateCallInfo, propKind);
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
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
		}

		private  async Task DispatchCallMessageForDelegateCallAsync(DelegateCallInfo delegateCallInfo, PropagationKind propKind)
		{
			var tasks = new List<Task>();

			foreach (var callee in delegateCallInfo.PossibleCallees)
			{
				var task = this.CreateAndSendCallMessageAsync(delegateCallInfo, callee, propKind);
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
		}

		private async Task CreateAndSendCallMessageAsync(CallInfo callInfo, MethodDescriptor callee, PropagationKind propKind)
		{
			var callMessageInfo = new CallMessageInfo(callInfo.Caller, callee, callInfo.ReceiverPossibleTypes,
				callInfo.ArgumentsPossibleTypes, callInfo.InstantiatedTypes, callInfo.CallNode, callInfo.LHS, propKind);

			var source = new MethodEntityDescriptor(callInfo.Caller);
			var callerMessage = new CallerMessage(source, callMessageInfo);

			//Logger.LogWarning(GrainClient.Logger, "Orchestrator", "CreateAndSendCallMsg", "Enqueuing: {0}", callee);
            await this.solutionManager.UpdateCounter(1);
			//await WaitQueue(QUEUE_THRESHOLD);
			this.messageWorkList.Enqueue(callerMessage);
			//this.messageWorkList.Add(callerMessage);

			//return TaskDone.Done;
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
			//Logger.LogInfo(GrainClient.Logger, "Orchestrator", "AnalyzeCalleeAsync", "Analyzing: {0}", callee);

			var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(callee);
			var propagationEffects = await methodEntityProc.PropagateAsync(callerMessage.CallMessageInfo);
			await this.PropagateEffectsAsync(propagationEffects, propKind, methodEntityProc);

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

		private  Task DispachReturnMessageAsync(ReturnInfo returnInfo, PropagationKind propKind)
		{
			return this.CreateAndSendReturnMessageAsync(returnInfo, propKind);
		}

		private async Task CreateAndSendReturnMessageAsync(ReturnInfo returnInfo, PropagationKind propKind)
		{
			var returnMessageInfo = new ReturnMessageInfo(returnInfo.CallerContext.Caller, returnInfo.Callee, returnInfo.ResultPossibleTypes, returnInfo.InstantiatedTypes,
				returnInfo.CallerContext.CallNode, returnInfo.CallerContext.LHS, propKind);

			var source = new MethodEntityDescriptor(returnInfo.Callee);
			var calleeMessage = new CalleeMessage(source, returnMessageInfo);

            await this.solutionManager.UpdateCounter(1);
			//await WaitQueue(QUEUE_THRESHOLD);
			this.messageWorkList.Enqueue(calleeMessage);
			//this.messageWorkList.Add(calleeMessage);

			//return TaskDone.Done;
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
			await this.PropagateEffectsAsync(propagationEffects, propKind, methodEntityProc);

			Logger.LogS("AnalysisOrchestator", "AnalyzeReturnAsync", "End Analyzing return to {0} ", caller);
		}
	}
}
