using Orleans;
using OrleansInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReachingTypeAnalysis;

namespace ReachingTypeAnalysis.Analysis
{
	internal class OrleansOrchestratorManager : OrchestratorManager
	{
		private IGrainFactory grainFactory;

		public static IOrchestratorGrain GetOrchestratorGrain(IGrainFactory grainFactory)
		{
			var grain = grainFactory.GetGrain<IOrchestratorGrain>("Orchestrator");
#if COMPUTE_STATS
			//grain = new OrchestratorGrainCallerWrapper(grain);
#endif
			return grain;
		}

		private OrleansOrchestratorManager(IGrainFactory grainFactory)
		{
			this.grainFactory = grainFactory;
		}

		public static async Task<OrchestratorManager> CreateAsync(IGrainFactory grainFactory, ISolutionGrain solutionGrain)
		{
			var manager = new OrleansOrchestratorManager(grainFactory);
			await manager.InitializeAsync(solutionGrain, 10);
			return manager;
		}

		protected override IEffectsProcessorManager CreateEffectsProcessor(int index)
		{
			var name = string.Format("EffectsProcessor{0}", index);
			var grain = grainFactory.GetGrain<IEffectsProcessorGrain>(name);
#if COMPUTE_STATS
			//grain = new EffectsProcessorGrainCallerWrapper(grain);
#endif
			return grain;
		}
	}
}
