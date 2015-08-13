﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Orleans;
using System.Threading;
using OrleansInterfaces;

namespace ReachingTypeAnalysis.Analysis
{
    internal class OrleansSolutionManager : SolutionManager
    {
        private IGrainFactory grainFactory;

        private OrleansSolutionManager(IGrainFactory grainFactory)
        {
            this.grainFactory = grainFactory;
        }

        public static async Task<OrleansSolutionManager> CreateFromSolutionAsync(IGrainFactory grainFactory, string solutionPath)
        {
            var manager = new OrleansSolutionManager(grainFactory);
            await manager.LoadSolutionAsync(solutionPath);
            return manager;
        }

        public static async Task<OrleansSolutionManager> CreateFromSourceAsync(IGrainFactory grainFactory, string source)
        {
            var manager = new OrleansSolutionManager(grainFactory);
            await manager.LoadSourceAsync(source);
            return manager;
        }

        protected override Task CreateProjectCodeProviderAsync(string projectFilePath, string assemblyName)
        {
            var projectGrain = grainFactory.GetGrain<IProjectCodeProviderGrain>(assemblyName);
            return projectGrain.SetProjectPath(projectFilePath);
        }

        protected override Task CreateProjectCodeProviderFromSourceAsync(string source, string assemblyName)
        {
            var projectGrain = grainFactory.GetGrain<IProjectCodeProviderGrain>(assemblyName);
            return projectGrain.SetProjectSourceCode(source);
        }

        public override Task<IProjectCodeProvider> GetProjectCodeProviderAsync(string assemblyName)
        {
			IProjectCodeProvider provider = null;
			var isExistingProject = this.solution.Projects.Any(pro => pro.AssemblyName.Equals(assemblyName));

			if (isExistingProject)
			{
				provider = grainFactory.GetGrain<IProjectCodeProviderGrain>(assemblyName);
			}
			else
			{
				provider = this.GetDummyProjectCodeProvider();
			}

            return Task.FromResult(provider);
        }

        private IProjectCodeProvider GetDummyProjectCodeProvider()
        {
			// TODO: In the future we may want to have a different project code provider
			// for each unknown project in the solution instead of having only one
			// representing all of them.
            var provider = grainFactory.GetGrain<IProjectCodeProviderGrain>("DUMMY");
            return provider;
        }
    }
}
