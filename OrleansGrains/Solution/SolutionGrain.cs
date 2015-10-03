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
	public class SolutionGrain : Grain<ISolutionState>, ISolutionGrain, IEntityGrainObserver
    {
        [NonSerialized]
        //private ISolutionManager solutionManager;
        private OrleansSolutionManager solutionManager;
		[NonSerialized]
		private ObserverSubscriptionManager<IEntityGrainObserver> observers;
		[NonSerialized]
		private int projectsReadyCount;

        public override async Task OnActivateAsync()
        {
			await StatsHelper.RegisterActivation("SolutionGrain", this.GrainFactory);

			Logger.OrleansLogger = this.GetLogger();
            Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "OnActivate","Enter");

			this.observers = new ObserverSubscriptionManager<IEntityGrainObserver>();

			Task.Run(async () =>
			{
				this.RaiseStateChangedEvent(EntityGrainState.Busy);
				this.projectsReadyCount = 0;

				if (!String.IsNullOrEmpty(this.State.SolutionPath))
				{
					this.solutionManager = await OrleansSolutionManager.CreateFromSolutionAsync(this.GrainFactory, this.State.SolutionPath);
				}
				else if (!String.IsNullOrEmpty(this.State.Source))
				{
					this.solutionManager = await OrleansSolutionManager.CreateFromSourceAsync(this.GrainFactory, this.State.Source);
				}
				else if (!String.IsNullOrEmpty(this.State.TestName))
				{
					this.solutionManager = await OrleansSolutionManager.CreateFromTestAsync(this.GrainFactory, this.State.TestName);
				}

				if (this.solutionManager != null)
				{
					while (this.projectsReadyCount < this.solutionManager.ProjectsCount)
					{
						await Task.Delay(100);
					}
				}

				this.RaiseStateChangedEvent(EntityGrainState.Ready);
			});

			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "OnActivate", "Exit");
		}

		public override Task OnDeactivateAsync()
		{
			return StatsHelper.RegisterDeactivation("SolutionGrain", this.GrainFactory); 
		}

		#region Observer pattern methods

		public Task Subscribe(IEntityGrainObserver observer)
		{
			this.observers.Subscribe(observer);
			return TaskDone.Done;
		}

		public Task Unsubscribe(IEntityGrainObserver observer)
		{
			this.observers.Unsubscribe(observer);
			return TaskDone.Done;
		}

		private void RaiseStateChangedEvent(EntityGrainState newState)
		{
			this.observers.Notify(observer => observer.OnStateChanged(this, newState));
		}

		#endregion

		#region On any ProjectCodeProvider state changed

		public void OnStateChanged(IGrain sender, EntityGrainState newState)
		{
			if (newState == EntityGrainState.Ready)
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

			Task.Run(async () =>
			{
				this.RaiseStateChangedEvent(EntityGrainState.Busy);
				this.projectsReadyCount = 0;

				this.solutionManager = await OrleansSolutionManager.CreateFromSolutionAsync(this.GrainFactory, this.State.SolutionPath);

				while (this.projectsReadyCount < this.solutionManager.ProjectsCount)
				{
					await Task.Delay(100);
				}

				this.RaiseStateChangedEvent(EntityGrainState.Ready);
			});

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

			Task.Run(async () =>
			{
				this.RaiseStateChangedEvent(EntityGrainState.Busy);
				this.projectsReadyCount = 0;

				this.solutionManager = await OrleansSolutionManager.CreateFromSourceAsync(this.GrainFactory, this.State.Source);

				while (this.projectsReadyCount < this.solutionManager.ProjectsCount)
				{
					await Task.Delay(100);
				}

				this.RaiseStateChangedEvent(EntityGrainState.Ready);
			});

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

			Task.Run(async () =>
			{
				this.RaiseStateChangedEvent(EntityGrainState.Busy);
				this.projectsReadyCount = 0;

				this.solutionManager = await OrleansSolutionManager.CreateFromTestAsync(this.GrainFactory, this.State.TestName);

				while (this.projectsReadyCount < this.solutionManager.ProjectsCount)
				{
					await Task.Delay(100);
				}

				this.RaiseStateChangedEvent(EntityGrainState.Ready);
			});

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
			StatsHelper.RegisterMsg("SolutionGrain::GetMethodEntity", this.GrainFactory);

			return this.solutionManager.GetMethodEntityAsync(methodDescriptor);
		}

		public Task AddInstantiatedTypesAsync(IEnumerable<TypeDescriptor> types)
        {
			StatsHelper.RegisterMsg("SolutionGrain::AddInstantiatedTypes", this.GrainFactory);

            return solutionManager.AddInstantiatedTypesAsync(types);
        }

        public Task<ISet<TypeDescriptor>> GetInstantiatedTypesAsync()
        {
			StatsHelper.RegisterMsg("SolutionGrain::GetInstantiatedTypes", this.GrainFactory);

			return this.solutionManager.GetInstantiatedTypesAsync();
        }

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

        public async Task ForceDeactivation()
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

		// TODO: remove this hack!
		public Task<IEnumerable<string>> GetDrives()
		{
			StatsHelper.RegisterMsg("SolutionGrain::GetDrives", this.GrainFactory);

			var drivers = DriveInfo.GetDrives().Select(d => d.Name).ToList();
			return Task.FromResult(drivers.AsEnumerable());
		}

        public Task<MethodDescriptor> GetMethodDescriptorByIndexAsync(int methodNumber)
        {
            return this.solutionManager.GetMethodDescriptorByIndexAsync(methodNumber);
        }
	}    
}
