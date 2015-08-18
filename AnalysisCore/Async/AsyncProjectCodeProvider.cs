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
using AnalysisCore.Roslyn;

namespace ReachingTypeAnalysis.Roslyn
{
    public class AsyncProjectCodeProvider : BaseProjectCodeProvider
    {
		private IDictionary<MethodDescriptor, IMethodEntityWithPropagator> methodEntities;
        
        private AsyncProjectCodeProvider(Project project, Compilation compilation)
			: base(project, compilation)
        {
			this.methodEntities = new Dictionary<MethodDescriptor, IMethodEntityWithPropagator>();
		}

		public static async Task<IProjectCodeProvider> CreateFromProjectAsync(string projectPath)
		{
			var cancellationTokenSource = new CancellationTokenSource();
			var project = await Utils.ReadProjectAsync(projectPath);

			if (project != null)
			{
				var compilation = await Utils.CompileProjectAsync(project, cancellationTokenSource.Token);
				return new AsyncProjectCodeProvider(project, compilation);
			}

			Contract.Assert(false, "Can't read project at path = " + projectPath);
			return null;
		}

		public static async Task<IProjectCodeProvider> CreateFromSourceAsync(string source, string assemblyName)
		{
			var cancellationTokenSource = new CancellationTokenSource();
			var solution = Utils.CreateSolution(source);

			foreach (var project in solution.Projects)
			{
				if (project.AssemblyName.Equals(assemblyName))
				{
					var compilation = await Utils.CompileProjectAsync(project, cancellationTokenSource.Token);
					return new AsyncProjectCodeProvider(project, compilation);
				}
			}

			Contract.Assert(false, "Can't find project with assembly name = " + assemblyName);
			return null;
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
				this.methodEntities.Add(methodDescriptor, result);
			}

			return result;
        }

		public override async Task<PropagationEffects> RemoveMethodAsync(MethodDescriptor methodDescriptor)
		{
			var propagationEffects = await base.RemoveMethodAsync(methodDescriptor);
			this.methodEntities.Remove(methodDescriptor);
			return propagationEffects;
		}
	}
}
