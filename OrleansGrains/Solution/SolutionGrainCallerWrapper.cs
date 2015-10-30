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
	internal class SolutionGrainCallerWrapper : ISolutionGrain
	{
		private ISolutionGrain solutionGrain;

		internal SolutionGrainCallerWrapper(ISolutionGrain solutionGrain)
		{
			this.solutionGrain = solutionGrain;
		}

		private void SetRequestContext()
		{
			RequestContext.Set(StatsHelper.CALLER_ADDR_CONTEXT, StatsHelper.CreateMyIPAddrContext());
		}

		public Task<IEnumerable<MethodDescriptor>> GetRootsAsync(AnalysisRootKind rootKind = AnalysisRootKind.Default)
		{
			this.SetRequestContext();
			return solutionGrain.GetRootsAsync(rootKind);
		}

		public Task<IEnumerable<MethodDescriptor>> GetReachableMethodsAsync()
		{
			this.SetRequestContext();
			return solutionGrain.GetReachableMethodsAsync();
		}
        public Task<int> GetReachableMethodsCountAsync()
        {
            this.SetRequestContext();
            return solutionGrain.GetReachableMethodsCountAsync();
        }

        public Task<IEnumerable<IProjectCodeProvider>> GetProjectCodeProvidersAsync()
		{
			this.SetRequestContext();
			return solutionGrain.GetProjectCodeProvidersAsync();
		}

		public Task<IProjectCodeProvider> GetProjectCodeProviderAsync(string assemblyName)
		{
			this.SetRequestContext();
			return solutionGrain.GetProjectCodeProviderAsync(assemblyName);
		}

		public Task<IProjectCodeProvider> GetProjectCodeProviderAsync(MethodDescriptor methodDescriptor)
		{
			this.SetRequestContext();
			return solutionGrain.GetProjectCodeProviderAsync(methodDescriptor);
		}

		public Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			this.SetRequestContext();
			return solutionGrain.GetMethodEntityAsync(methodDescriptor);
		}

		//public Task AddInstantiatedTypesAsync(IEnumerable<TypeDescriptor> types)
		//{
		//	this.SetRequestContext();
		//	return solutionGrain.AddInstantiatedTypesAsync(types);
		//}

		//public Task<ISet<TypeDescriptor>> GetInstantiatedTypesAsync()
		//{
		//	this.SetRequestContext();
		//	return solutionGrain.GetInstantiatedTypesAsync();
		//}

		public Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			this.SetRequestContext();
			return solutionGrain.GetModificationsAsync(modifiedDocuments);
		}

		public Task ReloadAsync()
		{
			this.SetRequestContext();
			return solutionGrain.ReloadAsync();
		}

		public Task SetSolutionPathAsync(string solutionPath)
		{
			this.SetRequestContext();
			return solutionGrain.SetSolutionPathAsync(solutionPath);
		}

		public Task SetSolutionSourceAsync(string solutionSource)
		{
			this.SetRequestContext();
			return solutionGrain.SetSolutionSourceAsync(solutionSource);
		}

		public Task SetSolutionFromTestAsync(string testName)
		{
			this.SetRequestContext();
			return solutionGrain.SetSolutionFromTestAsync(testName);
		}

		public Task ForceDeactivationAsync()
		{
			this.SetRequestContext();
			return solutionGrain.ForceDeactivationAsync();
		}

        public Task<MethodDescriptor> GetMethodDescriptorByIndexAsync(int index)
        {
            // This is used for the random queries. We don't need to use stats
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

		//public Task<IEnumerable<string>> GetDrives()
		//{
		//	this.SetRequestContext();
		//	return solutionGrain.GetDrivesAsync();
		//}
	}
}
