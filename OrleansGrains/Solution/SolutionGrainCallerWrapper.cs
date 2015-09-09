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
		private IGrainFactory grainFactory;
		private ISolutionGrain solutionGrain;

		internal SolutionGrainCallerWrapper(IGrainFactory grainFactory, ISolutionGrain solutionGrain)
		{
			this.grainFactory = grainFactory;
			this.solutionGrain = solutionGrain;
		}

		private void SetRequestContext()
		{
			RequestContext.Set("CallerAddr", StatsHelper.GetMyIPAddr());
		}

		public Task<IEnumerable<MethodDescriptor>> GetRootsAsync()
		{
			this.SetRequestContext();
			return solutionGrain.GetRootsAsync();
		}

		public Task<IEnumerable<MethodDescriptor>> GetPublicMethodsAsync()
		{
			this.SetRequestContext();
			return solutionGrain.GetPublicMethodsAsync();
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

		public Task AddInstantiatedTypesAsync(IEnumerable<TypeDescriptor> types)
		{
			this.SetRequestContext();
			return solutionGrain.AddInstantiatedTypesAsync(types);
		}

		public Task<ISet<TypeDescriptor>> GetInstantiatedTypesAsync()
		{
			this.SetRequestContext();
			return solutionGrain.GetInstantiatedTypesAsync();
		}

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

		public Task ForceDeactivation()
		{
			this.SetRequestContext();
			return solutionGrain.ForceDeactivation();
		}

		public Task<IEnumerable<string>> GetDrives()
		{
			this.SetRequestContext();
			return solutionGrain.GetDrives();
		}
	}
}
