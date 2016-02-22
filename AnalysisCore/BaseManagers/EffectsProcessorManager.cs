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

		public async Task ProcessMethodAsync(MethodDescriptor method)
		{
			Logger.LogS("EffectsProcessorManager", "ProcessMethod", "Analyzing: {0}", method);

			//var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(method);
			var methodEntityProc = await this.GetMethodEntityGrainAndActivateInProject(method);

			await methodEntityProc.UseDeclaredTypesForParameters();

			methodEntityProc.PropagateAsync(PropagationKind.ADD_TYPES); // do not await this call!!
			//await methodEntityProc.PropagateAsync(PropagationKind.ADD_TYPES);

			Logger.LogS("EffectsProcessorManager", "ProcessMethod", "End Analyzing {0} ", method);
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
				var task = this.DispatchCallMessageAsync(calleeInfo, propKind);
				//await task;
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
		}

		private async Task DispatchCallMessageAsync(CallInfo callInfo, PropagationKind propKind)
		{
			var tasks = new List<Task>();

			foreach (var callee in callInfo.PossibleCallees)
			{
				var task = this.CreateAndSendCallMessageAsync(callInfo, callee, propKind);
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

			return AnalyzeCalleeAsync(callMessageInfo.Callee, callerMessage, propKind);
		}

		private async Task AnalyzeCalleeAsync(MethodDescriptor callee, CallerMessage callerMessage, PropagationKind propKind)
		{
			Logger.LogS("EffectsProcessorManager", "AnalyzeCallee", "Analyzing: {0}", callee);

			//Logger.LogS("AnalysisOrchestator", "AnalyzeCalleeAsync", "Analyzing call to {0} ", callee);

			//var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(callee);
			var methodEntityProc = await this.GetMethodEntityGrainAndActivateInProject(callee);

			methodEntityProc.PropagateAsync(callerMessage.CallMessageInfo); // do not await this call!!
			//await methodEntityProc.PropagateAsync(callerMessage.CallMessageInfo);

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

			return AnalyzeReturnAsync(returnMessageInfo.Caller, calleeMessage, propKind);
		}

		private async Task AnalyzeReturnAsync(MethodDescriptor caller, CalleeMessage calleeMessage, PropagationKind propKind)
		{
			Logger.LogS("EffectsProcessorManager", "AnalyzeReturn", "Analyzing return to {0} ", caller);

			//var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(caller);
			var methodEntityProc = await this.GetMethodEntityGrainAndActivateInProject(caller);

			methodEntityProc.PropagateAsync(calleeMessage.ReturnMessageInfo); // do not await this call!!
			//await methodEntityProc.PropagateAsync(calleeMessage.ReturnMessageInfo);

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
