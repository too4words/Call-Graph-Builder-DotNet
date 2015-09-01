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
		private ISet<MethodDescriptor> methodsToRemove;

		private OrleansProjectCodeProvider(IGrainFactory grainFactory, ISolutionManager solutionManager)
			: base(solutionManager)
		{
			this.grainFactory = grainFactory;
			this.methodsToRemove = new HashSet<MethodDescriptor>();
        }

		public static async Task<OrleansProjectCodeProvider> CreateFromProjectAsync(IGrainFactory grainFactory, string projectPath)
		{
			var solutionManager = GrainClient.GrainFactory.GetGrain<ISolutionGrain>("Solution");
			var provider = new OrleansProjectCodeProvider(grainFactory, solutionManager);
			await provider.LoadProjectAsync(projectPath);
			return provider;
		}

		public static async Task<OrleansProjectCodeProvider> CreateFromSourceAsync(IGrainFactory grainFactory, string source, string assemblyName)
		{
			var solutionManager = GrainClient.GrainFactory.GetGrain<ISolutionGrain>("Solution");
			var provider = new OrleansProjectCodeProvider(grainFactory, solutionManager);
			await provider.LoadSourceAsync(source, assemblyName);
			return provider;
		}

		public static async Task<OrleansProjectCodeProvider> CreateFromTestAsync(IGrainFactory grainFactory, string testName, string assemblyName)
		{
			var solutionManager = GrainClient.GrainFactory.GetGrain<ISolutionGrain>("Solution");
			var provider = new OrleansProjectCodeProvider(grainFactory, solutionManager);
			await provider.LoadTestAsync(testName, assemblyName);
			return provider;
		}

		public override Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			var methodEntityGrain = grainFactory.GetGrain<IMethodEntityGrain>(methodDescriptor.Marshall());
			return Task.FromResult<IMethodEntityWithPropagator>(methodEntityGrain);
		}

		public override async Task<PropagationEffects> RemoveMethodAsync(MethodDescriptor methodDescriptor)
		{
			var propagationEffects = await base.RemoveMethodAsync(methodDescriptor);
			//var methodEntityGrain = grainFactory.GetGrain<IMethodEntityGrain>(methodDescriptor.Marshall());
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
				var methodEntityGrain = grainFactory.GetGrain<IMethodEntityGrain>(methodDescriptor.Marshall());
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
			var allMethodDescriptors = await base.GetAllMethodDescriptors();

			foreach (var methodDescriptor in allMethodDescriptors)
            {
                var methodEntityGrain = grainFactory.GetGrain<IMethodEntityGrain>(methodDescriptor.Marshall());
				var task = methodEntityGrain.ForceDeactivationAsync();
				//await task;
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }
	}
}
