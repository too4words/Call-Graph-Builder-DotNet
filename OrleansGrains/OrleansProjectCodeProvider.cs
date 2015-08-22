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

		private OrleansProjectCodeProvider(IGrainFactory grainFactory, Project project, Compilation compilation)
			: base(project, compilation)
        {
			this.grainFactory = grainFactory;
		}

		public static async Task<IProjectCodeProvider> CreateFromProjectAsync(IGrainFactory grainFactory, string projectPath)
		{
			var cancellationTokenSource = new CancellationTokenSource();
			var project = await Utils.ReadProjectAsync(projectPath);

			if (project != null)
			{
				var compilation = await Utils.CompileProjectAsync(project, cancellationTokenSource.Token);
				return new OrleansProjectCodeProvider(grainFactory, project, compilation);
			}

			Contract.Assert(false, "Can't read project at path = " + projectPath);
			return null;
		}

		public static async Task<IProjectCodeProvider> CreateFromSourceAsync(IGrainFactory grainFactory, string source, string assemblyName)
		{
			var cancellationTokenSource = new CancellationTokenSource();
			var solution = Utils.CreateSolution(source);

			foreach (var project in solution.Projects)
			{
				if (project.AssemblyName.Equals(assemblyName))
				{
					var compilation = await Utils.CompileProjectAsync(project, cancellationTokenSource.Token);
					return new OrleansProjectCodeProvider(grainFactory, project, compilation);
				}
			}

			Contract.Assert(false, "Can't find project with assembly name = " + assemblyName);
			return null;
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
	}
}
