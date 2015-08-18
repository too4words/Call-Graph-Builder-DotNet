// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using OrleansInterfaces;
using System.IO;
using System.Linq;

namespace ReachingTypeAnalysis.Analysis
{
    // TODO: Add instantiated types
    public interface ISolutionState : IGrainState
    {
        string SolutionPath { get; set; }
        string Source { get; set; }
    }

    //[StorageProvider(ProviderName = "FileStore")]
    [StorageProvider(ProviderName = "MemoryStore")]
    public class SolutionGrain : Grain<ISolutionState>, ISolutionGrain
    {
        [NonSerialized]
        ISolutionManager solutionManager;

        public override  async Task OnActivateAsync()
        {
            Logger.Log(this.GetLogger(), "SolGrain", "OnActivate","");

            if (this.State.SolutionPath != null)
            {
				this.solutionManager = await OrleansSolutionManager.CreateFromSolutionAsync(this.GrainFactory, this.State.SolutionPath);
            }
            else if (this.State.Source != null)
            {
				this.solutionManager = await OrleansSolutionManager.CreateFromSourceAsync(this.GrainFactory, this.State.Source);
            }
        }

        public async Task SetSolutionPathAsync(string solutionPath)
        {
			Logger.Log(this.GetLogger(), "SolGrain", "SetSolution", "Enter");

            this.State.SolutionPath = solutionPath;
			this.solutionManager = await OrleansSolutionManager.CreateFromSolutionAsync(this.GrainFactory, this.State.SolutionPath);

			await this.WriteStateAsync();
			Logger.Log(this.GetLogger(), "SolGrain", "SetSolution", "Exit");
		}

        public async Task SetSolutionSourceAsync(string source)
        {
            Logger.Log(this.GetLogger(), "SolGrain", "SetSolSource", "Enter");

            this.State.Source = source;
			this.solutionManager = await OrleansSolutionManager.CreateFromSourceAsync(this.GrainFactory, this.State.Source);

            await this.WriteStateAsync();
            Logger.Log(this.GetLogger(), "SolGrain", "SetSolSource", "Exit");
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
			Logger.Log(this.GetLogger(), "SolGrain", "GetRoots", "Enter");

            var roots = await this.solutionManager.GetRootsAsync();

			Logger.Log(this.GetLogger(), "SolGrain", "GetRoots", "Exit");
            return roots; 
        }

		public Task<IEnumerable<IProjectCodeProvider>> GetProjectCodeProvidersAsync()
		{
			return this.solutionManager.GetProjectCodeProvidersAsync();
		}

		// TODO: remove this hack!
		public Task<IEnumerable<string>> GetDrives()
		{
			var drivers = DriveInfo.GetDrives().Select(d => d.Name).ToList();
			return Task.FromResult(drivers.AsEnumerable());
		}
	}    
}
