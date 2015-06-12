using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using OrleansInterfaces;
using Orleans;

namespace ReachingTypeAnalysis.Roslyn
{
    internal class ProjectCodeProvider
    {
        internal Compilation Compilation { get; private set; }
        internal Project Project { get; private set; }
        //public SemanticModel SemanticModel { get; private set; }
        public static Solution Solution { get; internal set; }

        internal ProjectCodeProvider(Project project, Compilation compilation)
        {
            this.Project = project;
            this.Compilation = project.GetCompilationAsync().Result;
        }

        async internal static Task<ProjectCodeProvider> ProjectCodeProviderAsync(string fullPath)
        {
            foreach (var id in Solution.ProjectIds)
            {
                var project = Solution.GetProject(id);
                if (project.FilePath.Equals(fullPath))
                {
                    return new ProjectCodeProvider(project, await project.GetCompilationAsync());
                }
            }
            Contract.Assert(false, "Can't find path = " + fullPath);
            return null;
        }

        /*
		public static BaseMethodDeclarationSyntax FindMethodSyntax(SemanticModel model, SyntaxTree tree, MethodDescriptor method, out IMethodSymbol symbol)
		{
			var root = tree.GetRoot();
			var visitor = new MethodFinder(method, model);
			visitor.Visit(root);

			Contract.Assert(visitor.Result != null);
			symbol = (IMethodSymbol)model.GetDeclaredSymbol(visitor.Result);

			return visitor.Result;
		}*/

        public static IEnumerable<MethodDescriptor> GetMainMethods(Solution solution)
		{
			var cancellationToken = new System.Threading.CancellationToken();
			foreach (var project in solution.Projects)
			{
				var compilation = project.GetCompilationAsync().Result;
				var mainMethod = compilation.GetEntryPoint(cancellationToken);
				if (mainMethod != null)
				{
					// only return if there's a main method
					yield return Utils.CreateMethodDescriptor(mainMethod);
				}
			}
		}

        public IMethodSymbol FindMethod(MethodDescriptor methodDescriptor)
        {
            return RoslynSymbolFactory.FindMethodInCompilation(methodDescriptor, this.Compilation);
        }

        public MethodDescriptor FindMethodImplementation(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
        {
            var roslynMethod = FindMethod(methodDescriptor);
            if (roslynMethod != null)
            {
                var roslynType = RoslynSymbolFactory.GetTypeByName(typeDescriptor.TypeName, this.Compilation);
                var implementedMethod = Utils.FindMethodImplementation(roslynMethod, roslynType);
                return Utils.CreateMethodDescriptor(implementedMethod);
            }
            // If we cannot resolve the method, we return the same method.
            return methodDescriptor;
        }

		public static async Task<Tuple<BaseMethodDeclarationSyntax, IMethodSymbol>> FindMethodSyntaxAsync(
            SemanticModel model, SyntaxTree tree, MethodDescriptor method)
		{
			var root = await tree.GetRootAsync();
			var visitor = new MethodFinder(method, model);
			visitor.Visit(root);

			if (visitor.Result != null)
			{
				return new Tuple<BaseMethodDeclarationSyntax, IMethodSymbol>(
					visitor.Result,
					(IMethodSymbol)model.GetDeclaredSymbol(visitor.Result)
					);
			}
			else
			{
				return null;
			}
		}

		internal static async Task<Tuple<ProjectCodeProvider, SyntaxTree>> GetAsync(MethodDescriptor methodDescriptor)
		{
			Contract.Assert(ProjectCodeProvider.Solution != null);
			var cancellationSource = new CancellationTokenSource();
			var continuations = new List<Task<Compilation>>();
			foreach (var project in ProjectCodeProvider.Solution.Projects)
			{
				var compilation = await project.GetCompilationAsync(cancellationSource.Token);
                
				foreach (var tree in compilation.SyntaxTrees)
				{
                    var model = compilation.GetSemanticModel(tree);
                    var codeProvider = new ProjectCodeProvider(project, compilation);
					var pair = await ProjectCodeProvider.FindMethodSyntaxAsync(model, tree, methodDescriptor);
					if (pair != null)
					{
						// found it
						cancellationSource.Cancel();
                        return new Tuple<ProjectCodeProvider, SyntaxTree>(codeProvider, tree);
					}
				}
			}
            // In some cases (e.g, default constructors or library methods, we are not going to find the code in the solution)
            // We should not throw an exception. Maybe return a dummy code Provider to let the analysis evolve
            // or an informative message in order to let the caller continue. We can declare the exception
            // throw new ArgumentException("Cannot find a provider for " + methodDescriptor);
            return null;
		}

		internal static ProjectCodeProvider GetProviderContainingEntryPoint(Project project, out IMethodSymbol mainSymbol)
		{
            var compilation = project.GetCompilationAsync().Result;
			mainSymbol = compilation.GetEntryPoint(new System.Threading.CancellationToken());

			foreach (var tree in compilation.SyntaxTrees)
			{
				var finder = new MethodFinder(mainSymbol, compilation.GetSemanticModel(tree));
				finder.Visit(tree.GetRoot());
				if (finder.Result != null)
				{
					return new ProjectCodeProvider(project, compilation);
				}
			}

			return null;
		}

		internal static async Task<Tuple<ProjectCodeProvider, IMethodSymbol, SyntaxTree>> GetProviderContainingEntryPointAsync(
            Project project, CancellationToken cancellationToken = default(CancellationToken))
		{
            var compilation = project.GetCompilationAsync().Result;
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

		internal static async Task<Tuple<ProjectCodeProvider, IMethodSymbol, SyntaxTree>> GetProviderContainingEntryPointAsync(Solution solution, CancellationToken cancellationToken = default(CancellationToken))
		{
			var projectIDs = solution.GetProjectDependencyGraph().GetTopologicallySortedProjects(cancellationToken);
			var continuations = new BlockingCollection<Task<Tuple<ProjectCodeProvider, IMethodSymbol>>>();
			foreach (var projectId in projectIDs)
			{
				var project = solution.GetProject(projectId);
				var pair = await ProjectCodeProvider.GetProviderContainingEntryPointAsync(project);
				if (pair != null)
				{
					return pair;
				}
			}

			//foreach (var continuation in continuations) {
			//	var pair = await continuation;
			//	if (pair != null)
			//	{
			//		return pair;
			//	}
			//}

			return null;
		}

        internal virtual bool IsSubtype(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        {
            var roslynType1 = RoslynSymbolFactory.GetTypeByName(typeDescriptor1.TypeName, this.Compilation);
            var roslynType2 = RoslynSymbolFactory.GetTypeByName(typeDescriptor2.TypeName, this.Compilation);

            return TypeHelper.InheritsByName(roslynType1, roslynType2);
        }

        internal virtual Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        {
            var roslynType1 = RoslynSymbolFactory.GetTypeByName(typeDescriptor1.TypeName, this.Compilation);
            var roslynType2 = RoslynSymbolFactory.GetTypeByName(typeDescriptor2.TypeName, this.Compilation);

            return Task.FromResult(TypeHelper.InheritsByName(roslynType1, roslynType2));
        }

        /*
		var cancellationToken = new System.Threading.CancellationToken();
			var projectIDs = this.Solution.GetProjectDependencyGraph().GetTopologicallySortedProjects();
			Project project = null;
			Compilation compilation = null;
			foreach (var projectID in projectIDs)
			{
				var candidateProject = Solution.GetProject(projectID);
				compilation = candidateProject.GetCompilationAsync().Result;
				var diag = compilation.GetDiagnostics().Where(e => e.Severity == DiagnosticSeverity.Error);
				if (diag.Count() > 0)
				{
					throw new ArgumentException("Errors: {0}", string.Join("\n", diag));
				}
				if (compilation.GetEntryPoint(cancellationToken) != null)
				{
					project = candidateProject;
					break;
				}
			}

			var roslynMainMethod = compilation.GetEntryPoint(cancellationToken);
	*/

    }
    internal class NullCodeProvider : ProjectCodeProvider 
    {

        internal NullCodeProvider() : base(null, null) { }
        internal override bool IsSubtype(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        {
 	         return false;
        }
        internal override Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        {
            return Task.FromResult<bool>(false);
        }
    }
}
