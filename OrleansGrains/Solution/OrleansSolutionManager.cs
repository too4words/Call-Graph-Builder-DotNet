﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Orleans;
using Orleans.Runtime;
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

		public static ISolutionGrain GetSolutionGrain(IGrainFactory grainFactory)
		{
			var grain = grainFactory.GetGrain<ISolutionGrain>("Solution");

#if COMPUTE_STATS
			grain = new SolutionGrainCallerWrapper(grain);
#endif
			return grain;
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

        public static async Task<OrleansSolutionManager> CreateFromTestAsync(IGrainFactory grainFactory, string testName)
        {
            var manager = new OrleansSolutionManager(grainFactory);
            await manager.LoadTestAsync(testName);
            return manager;
        }

        protected override Task CreateProjectCodeProviderAsync(string projectFilePath, string assemblyName)
        {
            var projectGrain = OrleansProjectCodeProvider.GetProjectGrain(grainFactory, assemblyName);
            return projectGrain.SetProjectPathAsync(projectFilePath);
        }

        protected override Task CreateProjectCodeProviderFromSourceAsync(string source, string assemblyName)
        {
            var projectGrain = OrleansProjectCodeProvider.GetProjectGrain(grainFactory, assemblyName);
            return projectGrain.SetProjectSourceAsync(source);
        }

		protected override Task CreateProjectCodeProviderFromTestAsync(string testName, string assemblyName)
		{
			var projectGrain = OrleansProjectCodeProvider.GetProjectGrain(grainFactory, assemblyName);
			return projectGrain.SetProjectFromTestAsync(testName);
		}

        public override Task<IProjectCodeProvider> GetProjectCodeProviderAsync(string assemblyName)
        {
			IProjectCodeProvider provider = null;
			var isExistingProject = this.Projects.Any(pro => pro.AssemblyName.Equals(assemblyName));

			if (isExistingProject)
			{
				provider = OrleansProjectCodeProvider.GetProjectGrain(grainFactory, assemblyName);
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
            var provider = OrleansProjectCodeProvider.GetProjectGrain(grainFactory, "DUMMY");
            return provider;
        }

		public override Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			var methodEntityGrain = OrleansMethodEntity.GetMethodEntityGrain(grainFactory, methodDescriptor);
			return Task.FromResult<IMethodEntityWithPropagator>(methodEntityGrain);
		}

        public async Task ForceDeactivationOfProjects()
        {
            var tasks = new List<Task>();

            foreach (var project in this.Projects)
            {
                var provider = OrleansProjectCodeProvider.GetProjectGrain(grainFactory, project.AssemblyName);
				var task = provider.ForceDeactivationAsync();
				//await task;
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
			//this.instantiatedTypes = new HashSet<TypeDescriptor>();
			//this.projects = null;
			//this.solutionPath = null;
        }
	}
}