using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using Orleans.Streams;
using OrleansInterfaces;

namespace ReachingTypeAnalysis.Analysis
{
	public class EffectsDispatcherGrain : Grain, IEffectsDispatcherGrain
	{
		[NonSerialized]
		private IEffectsDispatcher effectsDispatcher;
		[NonSerialized]
		private ISolutionGrain solutionGrain;

		public override async Task OnActivateAsync()
		{
			await StatsHelper.RegisterActivation("EffectsDispatcherGrain", this.GrainFactory);

			this.solutionGrain = OrleansSolutionManager.GetSolutionGrain(this.GrainFactory);
			this.effectsDispatcher = new EffectsDispatcherManager(this.solutionGrain);
		}

		public override async Task OnDeactivateAsync()
		{
			await StatsHelper.RegisterDeactivation("EffectsDispatcherGrain", this.GrainFactory);
		}

		public async Task ProcessMethodAsync(MethodDescriptor method)
		{
			await StatsHelper.RegisterMsg("EffectsDispatcherGrain::ProcessMethod", this.GrainFactory);

			await this.effectsDispatcher.ProcessMethodAsync(method);
		}

		public async Task DispatchEffectsAsync(PropagationEffects effects)
		{
			await StatsHelper.RegisterMsg("EffectsDispatcherGrain::DispatchEffects", this.GrainFactory);

			await this.effectsDispatcher.DispatchEffectsAsync(effects);
		}

		public Task OnNextAsync(PropagationEffects item, StreamSequenceToken token = null)
		{
			return this.DispatchEffectsAsync(item);
		}

		public Task OnCompletedAsync()
		{
			Logger.LogWarning(this.GetLogger(),"EffectsDispatcherManager", "OnCompleted", "EffectsProcessor ID: {0}", this.GetPrimaryKeyString());
			return TaskDone.Done;
		}

		public Task OnErrorAsync(Exception ex)
		{
			Logger.LogWarning(this.GetLogger(), "EffectsDispatcherManager", "OnError", "Exception: {0}", ex);
			return TaskDone.Done;
		}
	}
}
