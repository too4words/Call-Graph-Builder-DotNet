using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using ReachingTypeAnalysis.Analysis;
using System.IO;
using ReachingTypeAnalysis.Roslyn;
using Orleans;
using OrleansInterfaces;

namespace ReachingTypeAnalysis.Analysis
{
    public class OrleansProjectCodeProvider : BaseProjectCodeProvider
    {
		private IGrainFactory grainFactory;
		private ISet<MethodDescriptor> reachableMethods;
		private ISet<MethodDescriptor> methodsToRemove;

		private OrleansProjectCodeProvider(IGrainFactory grainFactory, ISolutionManager solutionManager)
			: base(solutionManager)
		{
			this.grainFactory = grainFactory;
			this.reachableMethods = new HashSet<MethodDescriptor>();
			this.methodsToRemove = new HashSet<MethodDescriptor>();
        }

		public static IProjectCodeProviderGrain GetProjectGrain(IGrainFactory grainFactory, string assemblyName)
		{
			var grain = grainFactory.GetGrain<IProjectCodeProviderGrain>(assemblyName);

#if COMPUTE_STATS
			grain = new ProjectCodeProviderGrainCallerWrapper(grain);
#endif
			return grain;
		}

		public static async Task<OrleansProjectCodeProvider> CreateFromProjectAsync(IGrainFactory grainFactory, string projectPath)
		{
			var solutionManager = OrleansSolutionManager.GetSolutionGrain(grainFactory);
			var provider = new OrleansProjectCodeProvider(grainFactory, solutionManager);
			await provider.LoadProjectAsync(projectPath);
			return provider;
		}

		public static async Task<OrleansProjectCodeProvider> CreateFromSourceAsync(IGrainFactory grainFactory, string source, string assemblyName)
		{
			var solutionManager = OrleansSolutionManager.GetSolutionGrain(grainFactory);
			var provider = new OrleansProjectCodeProvider(grainFactory, solutionManager);
			await provider.LoadSourceAsync(source, assemblyName);
			return provider;
		}

		public static async Task<OrleansProjectCodeProvider> CreateFromTestAsync(IGrainFactory grainFactory, string testName, string assemblyName)
		{
			var solutionManager = OrleansSolutionManager.GetSolutionGrain(grainFactory);
			var provider = new OrleansProjectCodeProvider(grainFactory, solutionManager);
			await provider.LoadTestAsync(testName, assemblyName);
			return provider;
		}

		public override Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			reachableMethods.Add(methodDescriptor);

			var methodEntityGrain = OrleansMethodEntity.GetMethodEntityGrain(grainFactory, methodDescriptor);
			return Task.FromResult<IMethodEntityWithPropagator>(methodEntityGrain);
		}

		public override Task<IEnumerable<MethodDescriptor>> GetReachableMethodsAsync()
		{
			return Task.FromResult(reachableMethods.AsEnumerable());
		}

		public override async Task<PropagationEffects> RemoveMethodAsync(MethodDescriptor methodDescriptor)
		{
			var propagationEffects = await base.RemoveMethodAsync(methodDescriptor);
			//var methodEntityGrain = OrleansMethodEntity.GetMethodEntityGrain(grainFactory, methodDescriptor);
			//await methodEntityGrain.ForceDeactivationAsync();
			return propagationEffects;
		}

		public override async Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			var modifications = await base.GetModificationsAsync(modifiedDocuments);

			foreach (var modification in modifications)
			{
				if (modification.ModificationKind == ModificationKind.MethodRemoved ||
					modification.ModificationKind == ModificationKind.MethodUpdated)
				{
					methodsToRemove.Add(modification.MethodDescriptor);
				}
			}

			return modifications;
		}

		public override async Task ReloadAsync()
		{
			var tasks = new List<Task>();

			foreach (var methodDescriptor in methodsToRemove)
			{
				var methodEntityGrain = OrleansMethodEntity.GetMethodEntityGrain(grainFactory, methodDescriptor);
				var task = methodEntityGrain.ForceDeactivationAsync();
				//await task;
				tasks.Add(task);
			}

			methodsToRemove.Clear();

			await Task.WhenAll(tasks);			
			await base.ReloadAsync();
		}

		public async Task ForceDeactivationOfMethodEntitiesAsync()
        {
            var tasks = new List<Task>();

			foreach (var methodDescriptor in reachableMethods)
            {
                var methodEntityGrain = OrleansMethodEntity.GetMethodEntityGrain(grainFactory, methodDescriptor);
				var task = methodEntityGrain.ForceDeactivationAsync();
				//await task;
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }
	}
}
