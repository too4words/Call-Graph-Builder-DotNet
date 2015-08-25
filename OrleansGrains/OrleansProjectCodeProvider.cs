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

		private OrleansProjectCodeProvider(IGrainFactory grainFactory)
        {
			this.grainFactory = grainFactory;
		}

		public static async Task<OrleansProjectCodeProvider> CreateFromProjectAsync(IGrainFactory grainFactory, string projectPath)
		{
			var provider = new OrleansProjectCodeProvider(grainFactory);
			await provider.LoadProjectAsync(projectPath);
			return provider;
		}

		public static async Task<OrleansProjectCodeProvider> CreateFromSourceAsync(IGrainFactory grainFactory, string source, string assemblyName)
		{
			var provider = new OrleansProjectCodeProvider(grainFactory);
			await provider.LoadSourceAsync(source, assemblyName);
			return provider;
		}

		public static async Task<OrleansProjectCodeProvider> CreateFromTestAsync(IGrainFactory grainFactory, string testName, string assemblyName)
		{
			var provider = new OrleansProjectCodeProvider(grainFactory);
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
			var methodEntityGrain = grainFactory.GetGrain<IMethodEntityGrain>(methodDescriptor.Marshall());
			await methodEntityGrain.ForceDeactivationAsync();
			return propagationEffects;
		}

        public async Task ForceDeactivationOfMethodEntitiesAsync()
        {
            var allMethodDescriptors = await base.GetAllMethodDescriptors();
            var tasks = new List<Task>();
            foreach (var methodDescriptor in allMethodDescriptors)
            {
                var methodEntityGrain = grainFactory.GetGrain<IMethodEntityGrain>(methodDescriptor.Marshall());
                tasks.Add(methodEntityGrain.ForceDeactivationAsync());
            }
            await Task.WhenAll(tasks);
        }

	}
}
