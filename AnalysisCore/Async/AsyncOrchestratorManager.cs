using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReachingTypeAnalysis;

namespace ReachingTypeAnalysis.Analysis
{
	internal class AsyncOrchestratorManager : OrchestratorManager
	{
		protected override IEffectsProcessorManager CreateEffectsProcessor(int index)
		{
			var processor = new EffectsProcessorManager(this.solutionManager);
			return processor;
		}

		internal static async Task<IOrchestratorManager> CreateAsync(ISolutionManager solutionManager)
		{
			var manager = new AsyncOrchestratorManager();
			await manager.InitializeAsync(solutionManager, 10);
			return manager;
		}
	}
}
