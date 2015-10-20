// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using OrleansInterfaces;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Orleans.Concurrency;

namespace ReachingTypeAnalysis.Analysis
{
    // TODO: Add instantiated types
    public interface ISolutionState : IGrainState
    {
        string SolutionPath { get; set; }
        string Source { get; set; }
		string TestName { get; set; }
	}

    //[StorageProvider(ProviderName = "FileStore")]
    //[StorageProvider(ProviderName = "MemoryStore")]
	[StorageProvider(ProviderName = "AzureStore")]
	[Reentrant]
	public class SolutionGrain : Grain<ISolutionState>, ISolutionGrain, IEntityGrainObserverNotifications
    {
        [NonSerialized]
        //private ISolutionManager solutionManager;
        private OrleansSolutionManager solutionManager;
		[NonSerialized]
		private ObserverSubscriptionManager<IEntityGrainObserverNotifications> observers;
		[NonSerialized]
		private int projectsReadyCount;

        public override async Task OnActivateAsync()
        {
			await StatsHelper.RegisterActivation("SolutionGrain", this.GrainFactory);

			Logger.OrleansLogger = this.GetLogger();
            Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "OnActivate","Enter");

			this.observers = new ObserverSubscriptionManager<IEntityGrainObserverNotifications>();
			this.projectsReadyCount = 0;

			//Task.Run(async () =>
			//await Task.Factory.StartNew(async () =>
			//{
			try
			{
				this.RaiseStateChangedEvent(EntityGrainStatus.Busy);

				if (!String.IsNullOrEmpty(this.State.SolutionPath))
				{
					this.solutionManager = await OrleansSolutionManager.CreateFromSolutionAsync(this, this.GrainFactory, this.State.SolutionPath);
				}
				else if (!String.IsNullOrEmpty(this.State.Source))
				{
					this.solutionManager = await OrleansSolutionManager.CreateFromSourceAsync(this, this.GrainFactory, this.State.Source);
				}
				else if (!String.IsNullOrEmpty(this.State.TestName))
				{
					this.solutionManager = await OrleansSolutionManager.CreateFromTestAsync(this, this.GrainFactory, this.State.TestName);
				}

				//if (this.solutionManager != null)
				//{
				//	await this.WaitForAllProjects();
				//}

				this.RaiseStateChangedEvent(EntityGrainStatus.Ready);
			}
			catch (Exception ex)
			{
				Logger.LogError(this.GetLogger(), "SolutionGrain", "OnActivate", "Error:\n{0}", ex);
				throw ex;
			}
			//});

			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "OnActivate", "Exit");
		}

		public override Task OnDeactivateAsync()
		{
			return StatsHelper.RegisterDeactivation("SolutionGrain", this.GrainFactory); 
		}

		#region IObservableEntityGrain

		public Task AddObserverAsync(IEntityGrainObserverNotifications observer)
		{
			this.observers.Subscribe(observer);
			return TaskDone.Done;
		}

		public Task RemoveObserverAsync(IEntityGrainObserverNotifications observer)
		{
			this.observers.Unsubscribe(observer);
			return TaskDone.Done;
		}

		private void RaiseStateChangedEvent(EntityGrainStatus newState)
		{
			this.observers.Notify(observer => observer.OnStatusChanged(this.AsReference<ISolutionGrain>(), newState));
		}

		private async Task WaitForAllProjects()
		{
			while (this.projectsReadyCount < this.solutionManager.ProjectsCount)
			{
				await Task.Delay(100);
			}
		}

		#endregion

		#region IEntityGrainObserver

		public async Task StartObservingAsync(IObservableEntityGrain target)
		{
			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "StartObserving", "Enter");

			await target.AddObserverAsync(this);

			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "StartObserving", "Exit");
		}

		public async Task StopObservingAsync(IObservableEntityGrain target)
		{
			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "StopObserving", "Enter");

			await target.RemoveObserverAsync(this);

			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "StopObserving", "Exit");
		}

		public void OnStatusChanged(IObservableEntityGrain sender, EntityGrainStatus newState)
		{
			if (newState == EntityGrainStatus.Ready)
			{
				this.projectsReadyCount++;
			}
		}

		#endregion

        public async Task SetSolutionPathAsync(string solutionPath)
        {
			await StatsHelper.RegisterMsg("SolutionGrain::SetSolutionPath", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "SetSolutionPath", "Enter");

            this.State.SolutionPath = solutionPath;
			this.State.Source = null;
            this.State.TestName = null;

            await this.WriteStateAsync();
			this.projectsReadyCount = 0;

			//Task.Run(async () =>
			//await Task.Factory.StartNew(async () =>
			//{
			try
			{
				this.RaiseStateChangedEvent(EntityGrainStatus.Busy);

				this.solutionManager = await OrleansSolutionManager.CreateFromSolutionAsync(this, this.GrainFactory, this.State.SolutionPath);

				//await this.WaitForAllProjects();

				this.RaiseStateChangedEvent(EntityGrainStatus.Ready);
			}
			catch (Exception ex)
			{
				Logger.LogError(this.GetLogger(), "SolutionGrain", "SetSolutionPath", "Error:\n{0}", ex);
				throw ex;
			}
			//});

			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "SetSolutionPath", "Exit");
		}

        public async Task SetSolutionSourceAsync(string source)
        {
			await StatsHelper.RegisterMsg("SolutionGrain::SetSolutionSource", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "SetSolutionSource", "Enter");

            this.State.Source = source;
			this.State.SolutionPath = null;
            this.State.TestName = null;

            await this.WriteStateAsync();
			this.projectsReadyCount = 0;

			//Task.Run(async () =>
			//await Task.Factory.StartNew(async () =>
			//{
			try
			{
				this.RaiseStateChangedEvent(EntityGrainStatus.Busy);

				this.solutionManager = await OrleansSolutionManager.CreateFromSourceAsync(this, this.GrainFactory, this.State.Source);

				//await this.WaitForAllProjects();

				this.RaiseStateChangedEvent(EntityGrainStatus.Ready);
			}
			catch (Exception ex)
			{
				Logger.LogError(this.GetLogger(), "SolutionGrain", "SetSolutionSource", "Error:\n{0}", ex);
				throw ex;
			}
			//});

            Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "SetSolutionSource", "Exit");
        }

		public async Task SetSolutionFromTestAsync(string testName)
		{
			await StatsHelper.RegisterMsg("SolutionGrain::SetSolutionFromTest", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "SetSolutionFromTest", "Enter");

			this.State.TestName = testName;
			this.State.SolutionPath = null;
            this.State.Source = null;

			await this.WriteStateAsync();
			this.projectsReadyCount = 0;

			//Task.Run(async () =>
			//await Task.Factory.StartNew(async () =>
			//{
			try
			{
				this.RaiseStateChangedEvent(EntityGrainStatus.Busy);

				this.solutionManager = await OrleansSolutionManager.CreateFromTestAsync(this, this.GrainFactory, this.State.TestName);

				//await this.WaitForAllProjects();

				this.RaiseStateChangedEvent(EntityGrainStatus.Ready);
			}
			catch (Exception ex)
			{
				Logger.LogError(this.GetLogger(), "SolutionGrain", "SetSolutionFromTest", "Error:\n{0}", ex);
				throw ex;
			}
			//});

			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "SetSolutionFromTest", "Exit");
		}

		public Task<IProjectCodeProvider> GetProjectCodeProviderAsync(string assemblyName)
		{
			StatsHelper.RegisterMsg("SolutionGrain::GetProjectCodeProvider", this.GrainFactory);

			return this.solutionManager.GetProjectCodeProviderAsync(assemblyName);
		}

		public Task<IProjectCodeProvider> GetProjectCodeProviderAsync(MethodDescriptor methodDescriptor)
        {
			StatsHelper.RegisterMsg("SolutionGrain::GetProjectCodeProvider", this.GrainFactory);

            return this.solutionManager.GetProjectCodeProviderAsync(methodDescriptor);
        }

		public Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			//StatsHelper.RegisterMsg("SolutionGrain::GetMethodEntity:"+methodDescriptor, this.GrainFactory);
			StatsHelper.RegisterMsg("SolutionGrain::GetMethodEntity", this.GrainFactory);

			return this.solutionManager.GetMethodEntityAsync(methodDescriptor);
		}

		//public Task AddInstantiatedTypesAsync(IEnumerable<TypeDescriptor> types)
		//{
		//	StatsHelper.RegisterMsg("SolutionGrain::AddInstantiatedTypes", this.GrainFactory);

		//	return solutionManager.AddInstantiatedTypesAsync(types);
		//}

		//public Task<ISet<TypeDescriptor>> GetInstantiatedTypesAsync()
		//{
		//	StatsHelper.RegisterMsg("SolutionGrain::GetInstantiatedTypes", this.GrainFactory);

		//	return this.solutionManager.GetInstantiatedTypesAsync();
		//}

        public async Task<IEnumerable<MethodDescriptor>> GetRootsAsync()
        {
			await StatsHelper.RegisterMsg("SolutionGrain::GetRoots", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "GetRoots", "Enter");
		
			Stopwatch sw = new Stopwatch();
			sw.Start();
            var roots = await this.solutionManager.GetRootsAsync();

			Logger.LogInfo(this.GetLogger(), "SolutionGrain", "GetRoots", "End Time elapsed {0}", sw.Elapsed);
			
			return roots; 
        }

		public Task<IEnumerable<IProjectCodeProvider>> GetProjectCodeProvidersAsync()
		{
			StatsHelper.RegisterMsg("SolutionGrain::GetProjectCodeProviders", this.GrainFactory);

			return this.solutionManager.GetProjectCodeProvidersAsync();
		}

		public Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			StatsHelper.RegisterMsg("SolutionGrain::GetModifications", this.GrainFactory);

			return this.solutionManager.GetModificationsAsync(modifiedDocuments);
        }

		public Task ReloadAsync()
		{
			StatsHelper.RegisterMsg("SolutionGrain::Reload", this.GrainFactory);

			return this.solutionManager.ReloadAsync();
        }

		public Task<IEnumerable<MethodDescriptor>> GetPublicMethodsAsync()
		{
			StatsHelper.RegisterMsg("SolutionGrain::GetPublicMethods", this.GrainFactory);

			return this.solutionManager.GetPublicMethodsAsync();
		}

		public Task<IEnumerable<MethodDescriptor>> GetReachableMethodsAsync()
		{
			StatsHelper.RegisterMsg("SolutionGrain::GetReachableMethodsAsync", this.GrainFactory);

			return this.solutionManager.GetReachableMethodsAsync();
		}

        public Task<int> GetReachableMethodsCountAsync()
        {
            StatsHelper.RegisterMsg("SolutionGrain::GetReachableMethodsCount", this.GrainFactory);

            return this.solutionManager.GetReachableMethodsCountAsync();
        }

        public async Task ForceDeactivationAsync()
		{
			//await StatsHelper.RegisterMsg("SolutionGrain::ForceDeactivation", this.GrainFactory);

			await this.solutionManager.ForceDeactivationOfProjects();
			await this.ClearStateAsync();

			//this.State.SolutionPath = null;
			//this.State.Source = null;
			//this.State.TestName = null;
			//await this.WriteStateAsync();

			this.DeactivateOnIdle();
		}

		public Task<MethodDescriptor> GetMethodDescriptorByIndexAsync(int index)
		{
			return this.solutionManager.GetMethodDescriptorByIndexAsync(index);
		}

		public Task<EntityGrainStatus> GetStatusAsync()
		{
			var status = this.solutionManager != null &&
						 this.projectsReadyCount == this.solutionManager.ProjectsCount ?
							EntityGrainStatus.Ready :
							EntityGrainStatus.Busy;

			return Task.FromResult(status);
		}

		// TODO: remove this hack!
		//public Task<IEnumerable<string>> GetDrivesAsync()
		//{
		//	StatsHelper.RegisterMsg("SolutionGrain::GetDrives", this.GrainFactory);

		//	var drivers = DriveInfo.GetDrives().Select(d => d.Name).ToList();
		//	return Task.FromResult(drivers.AsEnumerable());
		//}
	}    
}
