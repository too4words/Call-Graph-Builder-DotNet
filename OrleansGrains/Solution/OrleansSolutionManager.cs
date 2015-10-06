using System;
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
		private SolutionGrain solutionGrain;
        private IGrainFactory grainFactory;
        private ISet<MethodDescriptor> methodDescriptors;

		private OrleansSolutionManager(SolutionGrain solutionGrain, IGrainFactory grainFactory)
		{
			this.solutionGrain = solutionGrain;
			this.grainFactory = grainFactory;
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
#if COMPUTE_STATS
            methodDescriptors.Add(methodDescriptor);
#endif
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
        public override Task<int> GetReachableMethodsCountAsync()
        {
            return Task.FromResult(this.methodDescriptors.Count);
        }
        public override Task<IEnumerable<MethodDescriptor>> GetReachableMethodsAsync()
        {
            return Task.FromResult(this.methodDescriptors.AsEnumerable());
        }
        public Task<MethodDescriptor> GetMethodDescriptorByIndexAsync(int methodNumber)
        {
            //HashSet<MethodDescriptor> set = (HashSet<MethodDescriptor>) this.methodDescriptors;
            return Task.FromResult(this.methodDescriptors.ElementAt(methodNumber));
        }
    }
}
