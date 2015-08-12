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
	public partial class ProjectCodeProvider
	{
		#region Used By OnDemandDispatcher (Sync Analysis)

		async internal static Task<MethodEntity> FindProviderAndCreateMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			return (await FindCodeProviderAndEntity(methodDescriptor)).Item2;
		}

		async internal static Task<Tuple<IProjectCodeProvider, MethodEntity>> FindCodeProviderAndEntity(MethodDescriptor methodDescriptor)
		{
			return await FindCodeProviderAndEntity(methodDescriptor, ProjectCodeProvider.Solution);
		}

		async internal static Task<Tuple<IProjectCodeProvider, MethodEntity>> FindCodeProviderAndEntity(MethodDescriptor methodDescriptor, Solution solution)
		{
			var pair = await ProjectCodeProvider.GetProjectProviderAndSyntaxAsync(methodDescriptor, solution);

			IProjectCodeProvider provider = pair.Item1;
			GeneralRoslynMethodParser syntaxProcessor = null;

			if (pair.Item2 != null)
			{
				var PProvider = (ProjectCodeProvider)pair.Item1;
				var tree = pair.Item2;
				var model = PProvider.Compilation.GetSemanticModel(tree);
				syntaxProcessor = new MethodParser(model, PProvider, tree, methodDescriptor);
			}
			else
			{
				syntaxProcessor = new LibraryMethodParser(methodDescriptor);
			}

			var methodEntity = syntaxProcessor.ParseMethod();

			return new Tuple<IProjectCodeProvider, MethodEntity>(provider, methodEntity);
		}

		public static IEnumerable<MethodDescriptor> GetMainMethods(Solution solution)
		{
			var cancellationTokenSource = new CancellationTokenSource();

			foreach (var project in solution.Projects)
			{
				var compilation = CompileProjectAsync(project, cancellationTokenSource.Token).Result;
				if (compilation == null) continue;

				var mainMethod = compilation.GetEntryPoint(cancellationTokenSource.Token);

				if (mainMethod != null)
				{
					// only return if there's a main method
					yield return Utils.CreateMethodDescriptor(mainMethod);
				}
			}
		}

		internal static async Task<Tuple<ProjectCodeProvider, IMethodSymbol, SyntaxTree>> GetProviderContainingEntryPointAsync(Solution solution)
		{
			var cancellationTokenSource = new CancellationTokenSource();
			var dependencyGraph = solution.GetProjectDependencyGraph();
			var projectIds = dependencyGraph.GetTopologicallySortedProjects(cancellationTokenSource.Token);

			foreach (var projectId in projectIds)
			{
				var project = solution.GetProject(projectId);
				var pair = await ProjectCodeProvider.GetProviderContainingEntryPointAsync(project, cancellationTokenSource.Token);

				if (pair != null)
				{
					return pair;
				}
			}

			return null;
		}
		internal static async Task<Tuple<ProjectCodeProvider, IMethodSymbol, SyntaxTree>> GetProviderContainingEntryPointAsync(Project project, CancellationToken cancellationToken)
		{
			var compilation = await CompileProjectAsync(project, cancellationToken);
			if (compilation == null) return null;

			var mainSymbol = compilation.GetEntryPoint(cancellationToken);

			if (mainSymbol == null)
			{
				return null;
			}
			else
			{
				try
				{
					foreach (var tree in compilation.SyntaxTrees)
					{
						var finder = new MethodFinder(mainSymbol, compilation.GetSemanticModel(tree));
						var root = await tree.GetRootAsync(cancellationToken);

						finder.Visit(root);

						if (finder.Result != null)
						{
							return new Tuple<ProjectCodeProvider, IMethodSymbol, SyntaxTree>
							(
								new ProjectCodeProvider(project, compilation), mainSymbol, tree
							);
						}
					}
				}
				catch (OperationCanceledException)
				{
					Console.Error.WriteLine("Cancelling...");
				}

				return null;
			}
		}

		#endregion

		#region Also used by OnDemand Async MethodProcessor (we need to get rid of the Global Solution here)

		internal static async Task<Tuple<IProjectCodeProvider, SyntaxTree>> GetProjectProviderAndSyntaxAsync(MethodDescriptor methodDescriptor)
		{
			Contract.Assert(ProjectCodeProvider.Solution != null);
			return await GetProjectProviderAndSyntaxAsync(methodDescriptor, ProjectCodeProvider.Solution);
		}

		internal static async Task<Tuple<IProjectCodeProvider, SyntaxTree>> GetProjectProviderAndSyntaxAsync(MethodDescriptor methodDescriptor, Solution solution)
		{
			Contract.Assert(solution != null);
			var cancellationSource = new CancellationTokenSource();
			var continuations = new List<Task<Compilation>>();

			foreach (var project in solution.Projects)
			{
				var compilation = await CompileProjectAsync(project, cancellationSource.Token);
				if (compilation == null) continue;

				foreach (var tree in compilation.SyntaxTrees)
				{
					var model = compilation.GetSemanticModel(tree);
					var codeProvider = new ProjectCodeProvider(project, compilation);
					var pair = await ProjectCodeProvider.FindMethodSyntaxAsync(model, tree, methodDescriptor);

					if (pair != null)
					{
						// found it
						cancellationSource.Cancel();
						return new Tuple<IProjectCodeProvider, SyntaxTree>(codeProvider, tree);
					}
				}
			}

			// In some cases (e.g, default constructors or library methods, we are not going to find the code in the solution)
			// We should not throw an exception. Maybe return a dummy code Provider to let the analysis evolve
			// or an informative message in order to let the caller continue. We can declare the exception
			// throw new ArgumentException("Cannot find a provider for " + methodDescriptor);
			return new Tuple<IProjectCodeProvider, SyntaxTree>(new DummyCodeProvider(), null);
		}


		#endregion

	}
}
