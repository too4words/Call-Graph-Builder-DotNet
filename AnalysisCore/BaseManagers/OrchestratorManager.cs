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

		public async Task ProcessMethodsAsync(IEnumerable<MethodDescriptor> methods)
		{
			var tasks = new List<Task>();

			foreach (var method in methods)
			{
				var task = this.ProcessMethodAsync(method);
				//await task;
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
		}

		private async Task ProcessMethodAsync(MethodDescriptor method)
		{
			Logger.LogS("OrchestratorManager", "ProcessMethod", "Analyzing: {0}", method);

			//var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(method);
			var methodEntityProc = await GetMethodEntityGrainAndActivateInProject(method);

			await methodEntityProc.UseDeclaredTypesForParameters();

			//PropagationEffects propagationEffects = null;
			//var ready = true;
			//
			//do
			//{
			//	propagationEffects = await methodEntityProc.PropagateAsync(PropagationKind.ADD_TYPES);
			//	ready = await WaitForReady(propagationEffects, method);
			//}
			//while (!ready);
			//
			//await this.ProcessEffectsAsync(propagationEffects, PropagationKind.ADD_TYPES);

			var propagationEffects = await methodEntityProc.PropagateAsync(PropagationKind.ADD_TYPES);

			await this.ProcessEffectsAsync(propagationEffects, PropagationKind.ADD_TYPES);
		}

		public Task ProcessEffectsAsync(PropagationEffects effects, PropagationKind propKind)
		{
			Logger.LogS("OrchestratorManager", "ProcessEffects", "Propagating effets computed in {0}", effects.SiloAddress);

			var processor = this.SelectEffectsProcessorAsync();
			processor.ProcessEffectsAsync(effects, propKind);
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
