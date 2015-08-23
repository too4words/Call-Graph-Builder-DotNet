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
using TestSources;

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

        public override  async Task OnActivateAsync()
        {
			Logger.OrleansLogger = this.GetLogger();

            Logger.LogVerbose(this.GetLogger(), "SolGrain", "OnActivate","");

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
        }

        public async Task SetSolutionPathAsync(string solutionPath)
        {
			Logger.LogVerbose(this.GetLogger(), "SolGrain", "SetSolution", "Enter");

            this.State.SolutionPath = solutionPath;
			this.solutionManager = await OrleansSolutionManager.CreateFromSolutionAsync(this.GrainFactory, this.State.SolutionPath);

			this.State.Source = null;
            this.State.TestName = null;

            await this.WriteStateAsync();
			Logger.LogVerbose(this.GetLogger(), "SolGrain", "SetSolution", "Exit");
		}

        public async Task SetSolutionSourceAsync(string source)
        {
            Logger.LogVerbose(this.GetLogger(), "SolGrain", "SetSolSource", "Enter");

            this.State.Source = source;
			this.solutionManager = await OrleansSolutionManager.CreateFromSourceAsync(this.GrainFactory, this.State.Source);
			this.State.SolutionPath = null;
            this.State.TestName = null;

            await this.WriteStateAsync();
            Logger.LogVerbose(this.GetLogger(), "SolGrain", "SetSolSource", "Exit");
        }
		public async Task SetSolutionFromTestAsync(string testName)
		{
			this.State.TestName = testName;
			var source = BasicTestsSources.Test[testName];
			this.solutionManager = await OrleansSolutionManager.CreateFromTestAsync(this.GrainFactory, testName);
			this.State.SolutionPath = null;
            this.State.Source = null;
			await this.WriteStateAsync();
			Logger.LogVerbose(this.GetLogger(), "SolGrain", "SetSolTest", "Exit");
		}

		public Task<IProjectCodeProvider> GetProjectCodeProviderAsync(string assemblyName)
		{
			return this.solutionManager.GetProjectCodeProviderAsync(assemblyName);
		}

		public Task<IProjectCodeProvider> GetProjectCodeProviderAsync(MethodDescriptor methodDescriptor)
        {
            return this.solutionManager.GetProjectCodeProviderAsync(methodDescriptor);
        }

		public Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			return this.solutionManager.GetMethodEntityAsync(methodDescriptor);
		}

		public Task AddInstantiatedTypesAsync(IEnumerable<TypeDescriptor> types)
        {
            return solutionManager.AddInstantiatedTypesAsync(types);
        }

        public Task<ISet<TypeDescriptor>> GetInstantiatedTypesAsync()
        {
            return this.solutionManager.GetInstantiatedTypesAsync();
        }

        public async Task<IEnumerable<MethodDescriptor>> GetRootsAsync()
        {
			Logger.LogVerbose(this.GetLogger(), "SolGrain", "GetRoots", "Enter");
		
			Stopwatch sw = new Stopwatch();
			sw.Start();
            var roots = await this.solutionManager.GetRootsAsync();

			Logger.LogInfo(this.GetLogger(), "SolGrain", "GetRoots", "End Time elapsed {0}", sw.Elapsed);
			
			return roots; 
        }

		public Task<IEnumerable<IProjectCodeProvider>> GetProjectCodeProvidersAsync()
		{
			return this.solutionManager.GetProjectCodeProvidersAsync();
		}

		public Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			return this.solutionManager.GetModificationsAsync(modifiedDocuments);
        }

		public Task ReloadAsync()
		{
			return this.solutionManager.ReloadAsync();
        }

		public async Task ForceDeactivation()
        {
            await this.solutionManager.ForceDeactivationOfProjects();

            //this.State.Etag = null;
            this.State.SolutionPath = null;
            this.State.Source = null;
            await this.WriteStateAsync();

            //await this.ClearStateAsync();

            this.DeactivateOnIdle();
        }

		// TODO: remove this hack!
		public Task<IEnumerable<string>> GetDrives()
		{
			var drivers = DriveInfo.GetDrives().Select(d => d.Name).ToList();
			return Task.FromResult(drivers.AsEnumerable());
		}
	}    
}
