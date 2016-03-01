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
	[ImplicitStreamSubscription("EffectsStream")]
	public class EffectsDispatcherGrain : Grain, IEffectsDispatcherGrain
	{
		[NonSerialized]
		private IEffectsDispatcher effectsDispatcher;
		[NonSerialized]
		private ISolutionGrain solutionGrain;
		[NonSerialized]
		private DateTime lastProcessingTime;

		public override async Task OnActivateAsync()
		{
			await StatsHelper.RegisterActivation("EffectsDispatcherGrain", this.GrainFactory);

			this.solutionGrain = OrleansSolutionManager.GetSolutionGrain(this.GrainFactory);
			this.effectsDispatcher = new EffectsDispatcherManager(this.solutionGrain);
			this.lastProcessingTime = DateTime.MinValue;

			var streamProvider = this.GetStreamProvider("SimpleMessageStreamProvider");
			var stream = streamProvider.GetStream<PropagationEffects>(this.GetPrimaryKey(), "EffectsStream");
			await stream.SubscribeAsync(this);

			//// Explicit subscription code
			//var subscriptionHandles = await stream.GetAllSubscriptionHandles();

			//if (subscriptionHandles != null && subscriptionHandles.Count > 0)
			//{
			//	var tasks = new List<Task>();

			//	foreach (var subscriptionHandle in subscriptionHandles)
			//	{
			//		var task = subscriptionHandle.ResumeAsync(this);
			//		//await task;
			//		tasks.Add(task);
			//	}

			//	await Task.WhenAll(tasks);
			//}
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

			this.lastProcessingTime = DateTime.UtcNow;
		}

		public Task OnNextAsync(PropagationEffects item, StreamSequenceToken token = null)
		{
			return this.DispatchEffectsAsync(item);
		}

		public Task OnCompletedAsync()
		{
			Logger.LogWarning(this.GetLogger(), "EffectsDispatcherGrain", "OnCompleted", "EffectsDispatcherGrain ID: {0}", this.GetPrimaryKeyString());
			return TaskDone.Done;
		}

		public Task OnErrorAsync(Exception ex)
		{
			Logger.LogWarning(this.GetLogger(), "EffectsDispatcherGrain", "OnError", "Exception: {0}", ex);
			return TaskDone.Done;
		}
	}
}
