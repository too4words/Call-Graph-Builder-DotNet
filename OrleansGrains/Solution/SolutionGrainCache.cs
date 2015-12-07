using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Orleans;
using Orleans.Runtime;
using System.Threading;
using System.Collections.Concurrent;
using OrleansInterfaces;

namespace ReachingTypeAnalysis.Analysis
{
	internal class SolutionGrainCache : ISolutionGrain
	{
		private ISolutionGrain solutionGrain;
        private static ConcurrentDictionary<MethodDescriptor, IMethodEntityWithPropagator> methodEntitiesMap = new ConcurrentDictionary<MethodDescriptor, IMethodEntityWithPropagator>();
        private static ConcurrentDictionary<string, IProjectCodeProvider> projectsAssemblyMaps = new ConcurrentDictionary<string, IProjectCodeProvider>();

        internal SolutionGrainCache(ISolutionGrain solutionGrain)
		{
			this.solutionGrain = solutionGrain;
		}

        public async Task<IProjectCodeProvider> GetProjectCodeProviderAsync(string assemblyName)
        {
            IProjectCodeProvider codeProvider = null;

            if (!projectsAssemblyMaps.TryGetValue(assemblyName, out codeProvider))
            {
                codeProvider = await solutionGrain.GetProjectCodeProviderAsync(assemblyName);
                projectsAssemblyMaps.TryAdd(assemblyName, codeProvider);
            }

            return codeProvider;
        }

        public async Task<IProjectCodeProvider> GetProjectCodeProviderAsync(MethodDescriptor methodDescriptor)
        {
			var typeDescriptor = methodDescriptor.ContainerType;
			var assemblyName = typeDescriptor.AssemblyName;
			var provider = await this.GetProjectCodeProviderAsync(assemblyName);

			return provider;
		}

        public async Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
        {
            IMethodEntityWithPropagator methodEntity = null;

            if (!methodEntitiesMap.TryGetValue(methodDescriptor, out methodEntity))
            {
                methodEntity = await solutionGrain.GetMethodEntityAsync(methodDescriptor);
                methodEntitiesMap.TryAdd(methodDescriptor, methodEntity);
            }

            return methodEntity;
        }

        public Task<IEnumerable<MethodDescriptor>> GetRootsAsync(AnalysisRootKind rootKind = AnalysisRootKind.Default)
		{
			return solutionGrain.GetRootsAsync(rootKind);
		}

		public Task<IEnumerable<MethodDescriptor>> GetReachableMethodsAsync()
		{
			return solutionGrain.GetReachableMethodsAsync();
		}

        public Task<int> GetReachableMethodsCountAsync()
        {
            return solutionGrain.GetReachableMethodsCountAsync();
        }

        public Task<bool> IsReachableAsync(MethodDescriptor methodDescriptor)
        {
            return solutionGrain.IsReachableAsync(methodDescriptor);
        }

        public Task<IEnumerable<IProjectCodeProvider>> GetProjectCodeProvidersAsync()
		{
			return solutionGrain.GetProjectCodeProvidersAsync();
		}

		public Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			return solutionGrain.GetModificationsAsync(modifiedDocuments);
		}

		public Task ReloadAsync()
		{
			return solutionGrain.ReloadAsync();
		}

		public Task SetSolutionPathAsync(string solutionPath)
		{
			return solutionGrain.SetSolutionPathAsync(solutionPath);
		}

		public Task SetSolutionSourceAsync(string solutionSource)
		{
			return solutionGrain.SetSolutionSourceAsync(solutionSource);
		}

		public Task SetSolutionFromTestAsync(string testName)
		{
			return solutionGrain.SetSolutionFromTestAsync(testName);
		}

		public Task ForceDeactivationAsync()
		{
			return solutionGrain.ForceDeactivationAsync();
		}

        public Task<MethodDescriptor> GetMethodDescriptorByIndexAsync(int index)
        {
           return solutionGrain.GetMethodDescriptorByIndexAsync(index);
        }

		public Task<EntityGrainStatus> GetStatusAsync()
		{
			return solutionGrain.GetStatusAsync();
		}

		public Task StartObservingAsync(IObservableEntityGrain target)
		{
			return solutionGrain.StartObservingAsync(target);
		}

		public Task StopObservingAsync(IObservableEntityGrain target)
		{
			return solutionGrain.StopObservingAsync(target);
		}

        public Task<int> UpdateCounter(int value)
        {
            return solutionGrain.UpdateCounter(value);
        }

		public Task<MethodDescriptor> GetRandomMethodAsync()
		{
			return solutionGrain.GetRandomMethodAsync();
		}
	}
}
