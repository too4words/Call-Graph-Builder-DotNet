using Orleans;
using OrleansInterfaces;
using ReachingTypeAnalysis.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReachingTypeAnalysis;

namespace ReachingTypeAnalysis.Analysis
{
	public class EffectsProcessorGrain : Grain, IEffectsProcessorGrain
	{
		[NonSerialized]
		private EffectsProcessorManager effectsProcessorManager;
		[NonSerialized]
		private ISolutionGrain solutionGrain;

		public override async Task OnActivateAsync()
		{
			await StatsHelper.RegisterActivation("EffectsProcessorGrain", this.GrainFactory);

			this.solutionGrain = OrleansSolutionManager.GetSolutionGrain(this.GrainFactory);
			this.effectsProcessorManager = new EffectsProcessorManager(this.solutionGrain);
		}

		public override Task OnDeactivateAsync()
		{
			return StatsHelper.RegisterDeactivation("EffectsProcessorGrain", this.GrainFactory);
		}

		public Task ProcessEffectsAsync(PropagationEffects effects, PropagationKind propKind)
		{
			StatsHelper.RegisterMsg("EffectsProcessorGrain::ProcessEffects", this.GrainFactory);

			return this.effectsProcessorManager.ProcessEffectsAsync(effects, propKind);
		}
	}
}
