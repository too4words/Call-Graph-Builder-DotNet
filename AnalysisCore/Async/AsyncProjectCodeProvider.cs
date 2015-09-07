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
using Orleans;

namespace ReachingTypeAnalysis.Roslyn
{
    public class AsyncProjectCodeProvider : BaseProjectCodeProvider
    {
		private ConcurrentDictionary<MethodDescriptor, IMethodEntityWithPropagator> methodEntities;
		private ConcurrentDictionary<MethodDescriptor, IMethodEntityWithPropagator> newMethodEntities;
		//private IDictionary<MethodDescriptor, IMethodEntityWithPropagator> methodEntities;
		//private IDictionary<MethodDescriptor, IMethodEntityWithPropagator> newMethodEntities;

		private AsyncProjectCodeProvider(ISolutionManager solutionManager)
			: base(solutionManager)
        {
			this.methodEntities = new ConcurrentDictionary<MethodDescriptor, IMethodEntityWithPropagator>();
		}

		public static async Task<AsyncProjectCodeProvider> CreateFromProjectAsync(string projectPath, ISolutionManager solutionManager)
		{
			var provider = new AsyncProjectCodeProvider(solutionManager);
			await provider.LoadProjectAsync(projectPath);
			return provider;
		}

		public static async Task<AsyncProjectCodeProvider> CreateFromSourceAsync(string source, string assemblyName, ISolutionManager solutionManager)
		{
			var provider = new AsyncProjectCodeProvider(solutionManager);
			await provider.LoadSourceAsync(source, assemblyName);
			return provider;
		}

		public static async Task<AsyncProjectCodeProvider> CreateFromTestAsync(string testName, string assemblyName, ISolutionManager solutionManager)
		{
			var provider = new AsyncProjectCodeProvider(solutionManager);
			await provider.LoadTestAsync(testName, assemblyName);
			return provider;
		}

		public override async Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			IMethodEntityWithPropagator result;

			
				if (!this.methodEntities.TryGetValue(methodDescriptor, out result))
				{
					var methodEntity = await this.CreateMethodEntityAsync(methodDescriptor.BaseDescriptor) as MethodEntity;

					if (methodDescriptor.IsAnonymousDescriptor)
					{
						methodEntity = methodEntity.GetAnonymousMethodEntity((AnonymousMethodDescriptor)methodDescriptor);
					}

					result = new MethodEntityWithPropagator(methodEntity, this);
					//lock (this.methodEntities)
					{
						this.methodEntities.TryAdd(methodDescriptor, result);
					}
				}
			
			return result;
        }

		public override async Task<PropagationEffects> RemoveMethodAsync(MethodDescriptor methodDescriptor)
		{
			var propagationEffects = await base.RemoveMethodAsync(methodDescriptor);
			//this.methodEntities.Remove(methodDescriptor);
			return propagationEffects;
		}

		public override async Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			var modifications = await base.GetModificationsAsync(modifiedDocuments);
			this.newMethodEntities = new ConcurrentDictionary<MethodDescriptor, IMethodEntityWithPropagator>(methodEntities);

			foreach (var modification in modifications)
			{
				if (modification.ModificationKind == ModificationKind.MethodRemoved ||
					modification.ModificationKind == ModificationKind.MethodUpdated)
				{
					IMethodEntityWithPropagator previousValue = null;
					newMethodEntities.TryRemove(modification.MethodDescriptor, out previousValue);
				}
			}

			return modifications;
		}

		public override Task ReloadAsync()
		{
			if (newMethodEntities != null)
			{
				this.methodEntities = newMethodEntities;
				this.newMethodEntities = null;
			}

			return base.ReloadAsync();
		}
    }
}
