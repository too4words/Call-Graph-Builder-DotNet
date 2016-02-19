using Orleans;
using ReachingTypeAnalysis;
using ReachingTypeAnalysis.Analysis;
using ReachingTypeAnalysis.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Analysis
{
	internal class EffectsProcessorManager : IEffectsProcessorManager
	{
		private ISolutionManager solutionManager;

		public EffectsProcessorManager(ISolutionManager solutionManager)
		{
			this.solutionManager = solutionManager;
		}

		public async Task ProcessEffectsAsync(PropagationEffects effects, PropagationKind propKind)
		{
			Logger.LogS("EffectsProcessorManager", "ProcessEffects", "Propagating effets computed in {0}", effects.SiloAddress);

			await this.ProcessCalleesAsync(effects.CalleesInfo, propKind);

			if (effects.ResultChanged)
			{
				await this.ProcessReturnAsync(effects.CallersInfo, propKind);
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

			//Logger.LogWarning(GrainClient.Logger, "Orchestrator", "CreateAndSendCallMsg", "Enqueuing: {0}", callee);

			//await WaitQueue(QUEUE_THRESHOLD);
			//this.messageWorkList.Enqueue(callerMessage);
			//this.messageWorkList.Add(callerMessage);

			//return TaskDone.Done;
			return AnalyzeCalleeAsync(callMessageInfo.Callee, callerMessage, propKind);
		}

		private async Task AnalyzeCalleeAsync(MethodDescriptor callee, CallerMessage callerMessage, PropagationKind propKind)
		{
			Logger.LogS("EffectsProcessorManager", "AnalyzeCallee", "Analyzing: {0}", callee);

			//Logger.LogS("AnalysisOrchestator", "AnalyzeCalleeAsync", "Analyzing call to {0} ", callee);

			//var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(callee);
			var methodEntityProc = await this.GetMethodEntityGrainAndActivateInProject(callee);

			methodEntityProc.PropagateAsync(callerMessage.CallMessageInfo);

			//PropagationEffects propagationEffects = null;
			//var ready = true;
			//
			//do
			//{
			//	propagationEffects = await methodEntityProc.PropagateAsync(callerMessage.CallMessageInfo);
			//	ready = await WaitForReady(propagationEffects,callee);
			//}
			//while (!ready);
			//
			//await this.PropagateEffectsAsync(propagationEffects, propKind, methodEntityProc);

			//var exit = false;
			//for (int i = 0; i < 3 && !exit; i++)
			//{
			//	try
			//	{
			//		exit = true;
			//await methodEntityProc.PropagateAndProcessAsync(callerMessage.CallMessageInfo);
			//var propagationEffects = await methodEntityProc.PropagateAsync(callerMessage.CallMessageInfo);
			//await this.PropagateEffectsAsync(propagationEffects, propKind, methodEntityProc);
			//	}
			//	catch (Exception e)
			//	{
			//		exit = false;
			//	}
			//}
			//var propagationEffects = await methodEntityProc.PropagateAsync(callerMessage.CallMessageInfo);
			//await this.PropagateEffectsAsync(propagationEffects, propKind, methodEntityProc);

			Logger.LogS("EffectsProcessorManager", "AnalyzeCallee", "End Analyzing call to {0} ", callee);
		}

		private async Task ProcessReturnAsync(IEnumerable<ReturnInfo> callersInfo, PropagationKind propKind)
		{
			var tasks = new List<Task>();

			foreach (var callerInfo in callersInfo)
			{
				if (callerInfo.ResultPossibleTypes.Count > 0)
				{
					var task = this.DispachReturnMessageAsync(callerInfo, propKind);
					//await task;
					tasks.Add(task);
				}
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

			//await WaitQueue(QUEUE_THRESHOLD);
			//this.messageWorkList.Enqueue(calleeMessage);
			//this.messageWorkList.Add(calleeMessage);

			//return TaskDone.Done;
			return AnalyzeReturnAsync(returnMessageInfo.Caller, calleeMessage, propKind);
		}

		private async Task AnalyzeReturnAsync(MethodDescriptor caller, CalleeMessage calleeMessage, PropagationKind propKind)
		{
			Logger.LogS("EffectsProcessorManager", "AnalyzeReturn", "Analyzing return to {0} ", caller);

			//var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(caller);
			var methodEntityProc = await this.GetMethodEntityGrainAndActivateInProject(caller);

			methodEntityProc.PropagateAsync(calleeMessage.ReturnMessageInfo);

			//PropagationEffects propagationEffects = null;
			//var ready = true;
			//
			//do
			//{
			//	propagationEffects = await methodEntityProc.PropagateAsync(calleeMessage.ReturnMessageInfo);
			//	ready = await WaitForReady(propagationEffects, caller);
			//}
			//while (!ready);
			//
			//await this.PropagateEffectsAsync(propagationEffects, propKind, methodEntityProc);

			//var exit = false;
			//for (int i = 0; i < 3 && !exit; i++)
			//{
			//	try
			//	{
			//		exit = true;
			//var propagationEffects = await methodEntityProc.PropagateAsync(calleeMessage.ReturnMessageInfo);
			//await this.PropagateEffectsAsync(propagationEffects, propKind, methodEntityProc);
			//	}
			//	catch (Exception e)
			//	{
			//		exit = false;
			//	}
			//}

			//var propagationEffects = await methodEntityProc.PropagateAsync(calleeMessage.ReturnMessageInfo);
			//await this.PropagateEffectsAsync(propagationEffects, propKind, methodEntityProc);

			Logger.LogS("EffectsProcessorManager", "AnalyzeReturn", "End Analyzing return to {0} ", caller);
		}

		private async Task<IMethodEntityWithPropagator> GetMethodEntityGrainAndActivateInProject(MethodDescriptor method)
		{
			var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(method); //as IMethodEntityGrain;
			// Force MethodGrain placement near projects
			//var codeProvider = await this.solutionManager.GetProjectCodeProviderAsync(method);
			//var methodEntityProc = await codeProvider.GetMethodEntityAsync(method) as IMethodEntityGrain;
			return methodEntityProc;
		}
	}
}
