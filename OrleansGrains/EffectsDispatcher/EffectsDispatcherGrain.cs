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
	[ImplicitStreamSubscription(AnalysisConstants.StreamNamespace)]
	public class EffectsDispatcherGrain : Grain, IEffectsDispatcherGrain
	{
		[NonSerialized]
		private IEffectsDispatcher effectsDispatcher;
		[NonSerialized]
		private ISolutionGrain solutionGrain;
		[NonSerialized]
		private ObserverSubscriptionManager<IAnalysisObserver> subscriptionManager;
		[NonSerialized]
		private IDisposable timer;
		[NonSerialized]
		private DateTime lastProcessingTime;

		public override async Task OnActivateAsync()
		{
			await StatsHelper.RegisterActivation("EffectsDispatcherGrain", this.GrainFactory);

			this.lastProcessingTime = DateTime.MaxValue;
			this.solutionGrain = OrleansSolutionManager.GetSolutionGrain(this.GrainFactory);
			this.effectsDispatcher = new EffectsDispatcherManager(this.solutionGrain);

			this.subscriptionManager = new ObserverSubscriptionManager<IAnalysisObserver>();

			var streamProvider = this.GetStreamProvider(AnalysisConstants.StreamProvider);
			var stream = streamProvider.GetStream<PropagationEffects>(this.GetPrimaryKey(), AnalysisConstants.StreamNamespace);
			await stream.SubscribeAsync(this);

			var period = TimeSpan.FromMilliseconds(AnalysisConstants.DispatcherTimerPeriod);
			this.timer = this.RegisterTimer(this.OnTimerTick, null, period, period);

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

			await base.OnActivateAsync();
		}

		public override async Task OnDeactivateAsync()
		{
			await StatsHelper.RegisterDeactivation("EffectsDispatcherGrain", this.GrainFactory);

			this.timer.Dispose();
		}

		public Task Subscribe(IAnalysisObserver observer)
		{
			this.subscriptionManager.Subscribe(observer);
			return TaskDone.Done;
		}

		public Task Unsubscribe(IAnalysisObserver observer)
		{
			this.subscriptionManager.Unsubscribe(observer);
			return TaskDone.Done;
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
			Logger.LogWarning(this.GetLogger(), "EffectsDispatcherGrain", "OnCompleted", "EffectsDispatcherGrain ID: {0}", this.GetPrimaryKey());
			return TaskDone.Done;
		}

		public Task OnErrorAsync(Exception ex)
		{
			Logger.LogWarning(this.GetLogger(), "EffectsDispatcherGrain", "OnError", "Exception: {0}", ex);
			return TaskDone.Done;
		}

		private Task OnTimerTick(object state)
		{
			var idleTime = DateTime.UtcNow - lastProcessingTime;

			if (idleTime.TotalMilliseconds > AnalysisConstants.DispatcherIdleThreshold)
			{
				// Notify that this dispatcher is idle.
				this.subscriptionManager.Notify(s => s.OnEffectsDispatcherIdle(this));
			}

			return TaskDone.Done;
		}
	}
}
