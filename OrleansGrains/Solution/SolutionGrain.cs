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
    public class SolutionGrain : Grain<ISolutionState>, ISolutionGrain
    {
        [NonSerialized]
        //ISolutionManager solutionManager;
        OrleansSolutionManager solutionManager;

        public override async Task OnActivateAsync()
        {
			await StatsHelper.RegisterActivation("SolutionGrain", this.GrainFactory);

			Logger.OrleansLogger = this.GetLogger();
            Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "OnActivate","Enter");

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

			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "OnActivate", "Exit");
		}
		public override Task OnDeactivateAsync()
		{
			return StatsHelper.RegisterDeactivation("SolutionGrain", this.GrainFactory); 
		}

        public async Task SetSolutionPathAsync(string solutionPath)
        {
			await StatsHelper.RegisterMsg("SolutionGrain::SetSolutionPath", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "SetSolutionPath", "Enter");

            this.State.SolutionPath = solutionPath;
			this.solutionManager = await OrleansSolutionManager.CreateFromSolutionAsync(this.GrainFactory, this.State.SolutionPath);
			this.State.Source = null;
            this.State.TestName = null;

            await this.WriteStateAsync();
			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "SetSolutionPath", "Exit");
		}

        public async Task SetSolutionSourceAsync(string source)
        {
			await StatsHelper.RegisterMsg("SolutionGrain::SetSolutionSource", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "SetSolutionSource", "Enter");

            this.State.Source = source;
			this.solutionManager = await OrleansSolutionManager.CreateFromSourceAsync(this.GrainFactory, this.State.Source);
			this.State.SolutionPath = null;
            this.State.TestName = null;

            await this.WriteStateAsync();
            Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "SetSolutionSource", "Exit");
        }

		public async Task SetSolutionFromTestAsync(string testName)
		{
			await StatsHelper.RegisterMsg("SolutionGrain::SetSolutionFromTest", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "SetSolutionFromTest", "Enter");

			this.State.TestName = testName;
			this.solutionManager = await OrleansSolutionManager.CreateFromTestAsync(this.GrainFactory, testName);
			this.State.SolutionPath = null;
            this.State.Source = null;

			await this.WriteStateAsync();
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

		public async Task ForceDeactivation()
        {
			//await StatsHelper.RegisterMsg("SolutionGrain::ForceDeactivation", this.GrainFactory);

            await this.solutionManager.ForceDeactivationOfProjects();

			await this.ClearStateAsync();

            //this.State.Etag = null;
            this.State.SolutionPath = null;
            this.State.Source = null;
			this.State.TestName = null;
			await this.WriteStateAsync();

			this.DeactivateOnIdle();
        }

		// TODO: remove this hack!
		public Task<IEnumerable<string>> GetDrives()
		{
			StatsHelper.RegisterMsg("SolutionGrain::GetDrives", this.GrainFactory);

			var drivers = DriveInfo.GetDrives().Select(d => d.Name).ToList();
			return Task.FromResult(drivers.AsEnumerable());
		}
	}    
}
