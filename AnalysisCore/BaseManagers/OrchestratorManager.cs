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
	internal abstract class OrchestratorManager : IOrchestratorManager
	{
		protected ISolutionManager solutionManager;
		private IList<IEffectsProcessorManager> processors;
		private int currentProcessorIndex;
		private int messageCount;

		public OrchestratorManager()
		{
			this.processors = new List<IEffectsProcessorManager>();
		}

		public Task InitializeAsync(ISolutionManager solutionManager, int processorsCount)
		{
			this.solutionManager = solutionManager;

			for (var i = 0; i < processorsCount; ++i)
			{
				var processor = this.CreateEffectsProcessor(i);
				this.processors.Add(processor);
			}

			return TaskDone.Done;
		}

		protected abstract IEffectsProcessorManager CreateEffectsProcessor(int index);

		public Task<int> GetCounterAsync()
		{
			return Task.FromResult(messageCount);
		}

		public Task ProcessMethodsAsync(IEnumerable<MethodDescriptor> methods)
		{
			foreach (var method in methods)
			{
				messageCount++;

				var processor = this.SelectEffectsProcessorAsync();
				processor.ProcessMethodAsync(method); // do not await this call!!
				//await processor.ProcessMethodAsync(method);
			}

			return TaskDone.Done;
		}

		public Task ProcessEffectsAsync(PropagationEffects effects, PropagationKind propKind)
		{
			Logger.LogS("OrchestratorManager", "ProcessEffects", "Propagating effets computed in {0}", effects.SiloAddress);

			messageCount += effects.CalleesInfo.Sum(ci => ci.PossibleCallees.Count);
			messageCount += effects.CallersInfo.Sum(ci => ci.ResultPossibleTypes.Count > 0 ? 1 : 0);

			var processor = this.SelectEffectsProcessorAsync();
			processor.ProcessEffectsAsync(effects, propKind); // do not await this call!!
			//await processor.ProcessEffectsAsync(effects, propKind);

			messageCount--;
			return TaskDone.Done;
		}

		private IEffectsProcessorManager SelectEffectsProcessorAsync()
		{
			this.currentProcessorIndex = (this.currentProcessorIndex + 1) % this.processors.Count;
			var processor = this.processors[this.currentProcessorIndex];
			return processor;
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
