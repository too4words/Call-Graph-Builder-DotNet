using Orleans;
using OrleansInterfaces;
using ReachingTypeAnalysis.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Analysis
{
	internal class OrleansEffectsDispatcherManager : IEffectsDispatcher
	{
		private ISolutionManager solutionManager;

		public OrleansEffectsDispatcherManager(ISolutionManager solutionManager)
		{
			this.solutionManager = solutionManager;
		}

		public static IEffectsDispatcherGrain GetEffectsDispatcherGrain(IGrainFactory grainFactory, Guid dispatcherGuid)
		{
			var grain = grainFactory.GetGrain<IEffectsDispatcherGrain>(dispatcherGuid);
#if COMPUTE_STATS
			//grain = new EffectsDispatcherGrainCallerWrapper(grain);
#endif
			return grain;
		}

		public async Task ProcessMethodAsync(MethodDescriptor method)
		{
			Logger.LogS("EffectsDispatcherManager", "ProcessMethod", "Analyzing: {0}", method);

			//var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(method);
			var methodEntityProc = await this.GetMethodEntityGrainAndActivateInProject(method);

			await methodEntityProc.UseDeclaredTypesForParameters();
			await methodEntityProc.PropagateAndProcessAsync(PropagationKind.ADD_TYPES);

			Logger.LogS("EffectsDispatcherManager", "ProcessMethod", "End Analyzing {0} ", method);
		}

		public async Task DispatchEffectsAsync(PropagationEffects effects)
		{
			Logger.LogS("EffectsDispatcherManager", "DispatchEffects", "Propagating effects computed in {0}", effects.SiloAddress);

			await this.ProcessCalleesAsync(effects.CalleesInfo, effects.Kind);

			if (effects.ResultChanged)
			{
				await this.ProcessReturnAsync(effects.CallersInfo, effects.Kind);
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

			return this.AnalyzeCalleeAsync(callMessageInfo.Callee, callerMessage, propKind);
		}

		private async Task AnalyzeCalleeAsync(MethodDescriptor callee, CallerMessage callerMessage, PropagationKind propKind)
		{
			Logger.LogS("EffectsDispatcherManager", "AnalyzeCallee", "Analyzing: {0}", callee);

			//var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(callee);
			var methodEntityProc = await this.GetMethodEntityGrainAndActivateInProject(callee);

			await methodEntityProc.PropagateAndProcessAsync(callerMessage.CallMessageInfo);

			Logger.LogS("EffectsDispatcherManager", "AnalyzeCallee", "End Analyzing call to {0} ", callee);
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

			return this.AnalyzeReturnAsync(returnMessageInfo.Caller, calleeMessage, propKind);
		}

		private async Task AnalyzeReturnAsync(MethodDescriptor caller, CalleeMessage calleeMessage, PropagationKind propKind)
		{
			Logger.LogS("EffectsDispatcherManager", "AnalyzeReturn", "Analyzing return to {0} ", caller);

			//var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(caller);
			var methodEntityProc = await this.GetMethodEntityGrainAndActivateInProject(caller);

			await methodEntityProc.PropagateAndProcessAsync(calleeMessage.ReturnMessageInfo);

			Logger.LogS("EffectsDispatcherManager", "AnalyzeReturn", "End Analyzing return to {0} ", caller);
		}

		private async Task<IMethodEntityGrain> GetMethodEntityGrainAndActivateInProject(MethodDescriptor method)
		{
			var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(method); //as IMethodEntityGrain;
			// Force MethodGrain placement near projects
			//var codeProvider = await this.solutionManager.GetProjectCodeProviderAsync(method);
			//var methodEntityProc = await codeProvider.GetMethodEntityAsync(method) as IMethodEntityGrain;
			return methodEntityProc as IMethodEntityGrain;
		}
	}
}
