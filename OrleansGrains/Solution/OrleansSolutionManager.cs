﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Orleans;
using Orleans.Runtime;
using System.Threading;
using OrleansInterfaces;

using AssemblyName = System.String;

namespace ReachingTypeAnalysis.Analysis
{
    internal class OrleansSolutionManager : SolutionManager
    {
		private SolutionGrain solutionGrain;
        private IGrainFactory grainFactory;
		private ISet<AssemblyName> projectProviders;
		private ISet<AssemblyName> newProjectProviders;
		private ISet<MethodDescriptor> methodDescriptors;
		private ISet<MethodDescriptor> newMethodDescriptors;

		private OrleansSolutionManager(SolutionGrain solutionGrain, IGrainFactory grainFactory)
		{
			this.solutionGrain = solutionGrain;
			this.grainFactory = grainFactory;
			this.projectProviders = new HashSet<AssemblyName>();
            this.methodDescriptors = new HashSet<MethodDescriptor>();
		}

		public static ISolutionGrain GetSolutionGrain(IGrainFactory grainFactory)
		{
			var grain = grainFactory.GetGrain<ISolutionGrain>("Solution");

#if COMPUTE_STATS
			grain = new SolutionGrainCallerWrapper(grain);
#endif
			return grain;
		}

		public static async Task<OrleansSolutionManager> CreateFromSolutionAsync(SolutionGrain solutionGrain, IGrainFactory grainFactory, string solutionPath)
        {
			var manager = new OrleansSolutionManager(solutionGrain, grainFactory);
            await manager.LoadSolutionAsync(solutionPath);
            return manager;
        }

        public static async Task<OrleansSolutionManager> CreateFromSourceAsync(SolutionGrain solutionGrain, IGrainFactory grainFactory, string source)
        {
			var manager = new OrleansSolutionManager(solutionGrain, grainFactory);
            await manager.LoadSourceAsync(source);
            return manager;
        }

		public static async Task<OrleansSolutionManager> CreateFromTestAsync(SolutionGrain solutionGrain, IGrainFactory grainFactory, string testName)
        {
			var manager = new OrleansSolutionManager(solutionGrain, grainFactory);
            await manager.LoadTestAsync(testName);
            return manager;
        }

		private ISet<AssemblyName> ProjectProviders
		{
			get { return useNewFieldsVersion ? newProjectProviders : projectProviders; }
		}

		private ISet<MethodDescriptor> MethodDescriptors
		{
			get { return useNewFieldsVersion ? newMethodDescriptors : methodDescriptors; }
		}

		protected override async Task CreateProjectCodeProviderAsync(string projectFilePath, string assemblyName)
        {
			var projectGrain = OrleansProjectCodeProvider.GetProjectGrain(grainFactory, assemblyName);
			var projectGrainReference = projectGrain;

			if (projectGrain is ProjectCodeProviderGrainCallerWrapper)
			{
				var projectGrainWrapper = projectGrain as ProjectCodeProviderGrainCallerWrapper;
				projectGrainReference = projectGrainWrapper.AsReference();
			}

			await solutionGrain.StartObservingAsync(projectGrainReference);
            await projectGrain.SetProjectPathAsync(projectFilePath);

			this.ProjectProviders.Add(assemblyName);
        }

        protected override async Task CreateProjectCodeProviderFromSourceAsync(string source, string assemblyName)
        {
			var projectGrain = OrleansProjectCodeProvider.GetProjectGrain(grainFactory, assemblyName);
			var projectGrainReference = projectGrain;

			if (projectGrain is ProjectCodeProviderGrainCallerWrapper)
			{
				var projectGrainWrapper = projectGrain as ProjectCodeProviderGrainCallerWrapper;
				projectGrainReference = projectGrainWrapper.AsReference();
			}

			await solutionGrain.StartObservingAsync(projectGrainReference);
            await projectGrain.SetProjectSourceAsync(source);

			this.ProjectProviders.Add(assemblyName);
		}

		protected override async Task CreateProjectCodeProviderFromTestAsync(string testName, string assemblyName)
		{
			var projectGrain = OrleansProjectCodeProvider.GetProjectGrain(grainFactory, assemblyName);
			var projectGrainReference = projectGrain;

			if (projectGrain is ProjectCodeProviderGrainCallerWrapper)
			{
				var projectGrainWrapper = projectGrain as ProjectCodeProviderGrainCallerWrapper;
				projectGrainReference = projectGrainWrapper.AsReference();
			}

			await solutionGrain.StartObservingAsync(projectGrainReference);
			await projectGrain.SetProjectFromTestAsync(testName);

			this.ProjectProviders.Add(assemblyName);
		}

        public override Task<IProjectCodeProvider> GetProjectCodeProviderAsync(string assemblyName)
        {
			IProjectCodeProvider provider = null;
			var isExistingProject = this.ProjectProviders.Contains(assemblyName);

			if (isExistingProject)
			{
				provider = OrleansProjectCodeProvider.GetProjectGrain(grainFactory, assemblyName);
			}
			else
			{
				provider = this.GetDummyProjectCodeProvider();
				//this.ProjectProviders.Add(assemblyName);
			}

            return Task.FromResult(provider);
        }

        private IProjectCodeProvider GetDummyProjectCodeProvider()
        {
			// TODO: In the future we may want to have a different project code provider
			// for each unknown project in the solution instead of having only one
			// representing all of them.
            var provider = OrleansProjectCodeProvider.GetProjectGrain(grainFactory, "DUMMY");
			this.ProjectProviders.Add("DUMMY");
			return provider;
        }

		public override Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
        {
//#if COMPUTE_STATS
            this.MethodDescriptors.Add(methodDescriptor);
//#endif
            var methodEntityGrain = OrleansMethodEntity.GetMethodEntityGrain(grainFactory, methodDescriptor);
			return Task.FromResult<IMethodEntityWithPropagator>(methodEntityGrain);
		}

//#if COMPUTE_STATS
		public override Task<int> GetReachableMethodsCountAsync()
		{
			return Task.FromResult(this.MethodDescriptors.Count);
		}

		public override Task<IEnumerable<MethodDescriptor>> GetReachableMethodsAsync()
		{
			return Task.FromResult(this.MethodDescriptors.AsEnumerable());
		}
//#endif

		public Task<MethodDescriptor> GetMethodDescriptorByIndexAsync(int methodNumber)
		{
			//HashSet<MethodDescriptor> set = (HashSet<MethodDescriptor>) this.methodDescriptors;
			return Task.FromResult(this.MethodDescriptors.ElementAt(methodNumber));
		}

		public override Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			this.newProjectProviders = new HashSet<AssemblyName>(this.ProjectProviders);
			this.newMethodDescriptors = new HashSet<MethodDescriptor>(this.MethodDescriptors);
			return base.GetModificationsAsync(modifiedDocuments);
		}

		public override async Task ReloadAsync()
		{
			if (newProjectProviders != null)
			{
				this.projectProviders = newProjectProviders;
				this.newProjectProviders = null;

				this.methodDescriptors = newMethodDescriptors;
				this.newMethodDescriptors = null;
			}

			await base.ReloadAsync();
		}

		public async Task ForceDeactivationOfProjects()
        {
            var tasks = new List<Task>();

            foreach (var assemblyName in this.ProjectProviders)
            {
                var provider = OrleansProjectCodeProvider.GetProjectGrain(grainFactory, assemblyName);
				var task = provider.ForceDeactivationAsync();
				//await task;
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }
    }
}
