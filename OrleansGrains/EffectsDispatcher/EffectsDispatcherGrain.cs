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
	//[ImplicitStreamSubscription(AnalysisConstants.StreamNamespace)]
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
		[NonSerialized]
		private EffectsDispatcherStatus status;
		[NonSerialized]
		private bool isDispatchingEffects;

		public override async Task OnActivateAsync()
		{
			await StatsHelper.RegisterActivation("EffectsDispatcherGrain", this.GrainFactory);

			this.isDispatchingEffects = false;
			this.status = EffectsDispatcherStatus.Inactive;			
			this.lastProcessingTime = DateTime.UtcNow; // DateTime.MinValue; // DateTime.MaxValue;
			this.solutionGrain = OrleansSolutionManager.GetSolutionGrain(this.GrainFactory);
			this.effectsDispatcher = new OrleansEffectsDispatcherManager(this.GrainFactory, this.solutionGrain);

			this.subscriptionManager = new ObserverSubscriptionManager<IAnalysisObserver>();

			var streamProvider = this.GetStreamProvider(AnalysisConstants.StreamProvider);
			var stream = streamProvider.GetStream<PropagationEffects>(this.GetPrimaryKey(), AnalysisConstants.StreamNamespace);
			//await stream.SubscribeAsync(this);

			// Explicit subscription code
			var subscriptionHandles = await stream.GetAllSubscriptionHandles();

			if (subscriptionHandles != null && subscriptionHandles.Count > 0)
			{
				var tasks = new List<Task>();

				foreach (var subscriptionHandle in subscriptionHandles)
				{
					var task = subscriptionHandle.ResumeAsync(this);
					//await task;
					tasks.Add(task);
				}

				await Task.WhenAll(tasks);
			}
			else
			{
				await stream.SubscribeAsync(this);
			}

			var period = TimeSpan.FromMilliseconds(AnalysisConstants.DispatcherTimerPeriod);
			this.timer = this.RegisterTimer(this.OnTimerTick, null, period, period);

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

			Logger.LogInfoForDebug(this.GetLogger(), "@@[Dispatcher {0}] Dequeuing effects", this.GetPrimaryKey());

			this.lastProcessingTime = DateTime.UtcNow;
			this.isDispatchingEffects = true;

			if (this.status != EffectsDispatcherStatus.Busy)
			{
				Logger.LogForRelease(this.GetLogger(), "@@[Dispatcher {0}] Becoming busy (before was {1})", this.GetPrimaryKey(), this.status);

				// Notify that the dispatcher is busy
				this.status = EffectsDispatcherStatus.Busy;
				this.subscriptionManager.Notify(s => s.OnEffectsDispatcherStatusChanged(this, this.status));
			}

			await this.effectsDispatcher.DispatchEffectsAsync(effects);

            this.lastProcessingTime = DateTime.UtcNow;
            this.isDispatchingEffects = false;
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

		public async Task ForceDeactivationAsync()
		{
			//await StatsHelper.RegisterMsg("EffectsDispatcherGrain::ForceDeactivation", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "EffectsDispatcherGrain", "ForceDeactivation", "force for {0} ", this.GetPrimaryKey());
			//await this.ClearStateAsync();

			// Clear all fields before calling WriteStateAsync
			//await this.WriteStateAsync();

			// Unsubscribe from client
			this.subscriptionManager.Clear();

			// Unsubscribe from stream
			var streamProvider = this.GetStreamProvider(AnalysisConstants.StreamProvider);
			var stream = streamProvider.GetStream<PropagationEffects>(this.GetPrimaryKey(), AnalysisConstants.StreamNamespace);

			// Explicit subscription code
			var subscriptionHandles = await stream.GetAllSubscriptionHandles();

			if (subscriptionHandles != null && subscriptionHandles.Count > 0)
			{
				var tasks = new List<Task>();

				foreach (var subscriptionHandle in subscriptionHandles)
				{
					var task = subscriptionHandle.UnsubscribeAsync();
					//await task;
					tasks.Add(task);
				}

				await Task.WhenAll(tasks);
			}

			this.DeactivateOnIdle();
		}

		private Task OnTimerTick(object state)
		{
			var idleTime = DateTime.UtcNow - lastProcessingTime;

			if (!this.isDispatchingEffects && this.status == EffectsDispatcherStatus.Inactive &&
				idleTime.TotalMilliseconds > AnalysisConstants.DispatcherInactiveThreshold)
			{
				Logger.LogForDebug(this.GetLogger(), "@@[Dispatcher {0}] Was inactive for too long", this.GetPrimaryKey());

				// Notify that this dispatcher was inactive for too long.
				this.subscriptionManager.Notify(s => s.OnEffectsDispatcherStatusChanged(this, this.status));
			}

			if (!this.isDispatchingEffects && this.status == EffectsDispatcherStatus.Busy &&
				idleTime.TotalMilliseconds > AnalysisConstants.DispatcherIdleThreshold)
			{
				Logger.LogForDebug(this.GetLogger(), "@@[Dispatcher {0}] Becoming idle (before was {1})", this.GetPrimaryKey(), this.status);

				// Notify that this dispatcher is idle.
				this.status = EffectsDispatcherStatus.Idle;
				this.subscriptionManager.Notify(s => s.OnEffectsDispatcherStatusChanged(this, this.status));
			}

			return TaskDone.Done;
		}
	}
}
