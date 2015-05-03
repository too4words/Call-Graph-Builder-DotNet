using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Roslyn
{
	internal class CodeProvider
	{
		internal Compilation Compilation { get; private set; }
		internal SyntaxTree SyntaxTree { get; private set; }
		public SemanticModel SemanticModel { get; private set; }
		public static Solution Solution { get; internal set; }

		internal CodeProvider(SyntaxTree tree, Compilation compilation)
		{
			this.SyntaxTree = tree;	//project.GetCompilationAsync().Result;
			this.Compilation = compilation;

			this.SemanticModel = this.Compilation.GetSemanticModel(tree);
		}

		public BaseMethodDeclarationSyntax FindMethodSyntax(MethodDescriptor method, out IMethodSymbol symbol)
		{
			var root = this.SyntaxTree.GetRoot();
			var visitor = new MethodFinder(method, this.SemanticModel);
			visitor.Visit(root);

			Contract.Assert(visitor.Result != null);
			symbol = (IMethodSymbol)this.SemanticModel.GetDeclaredSymbol(visitor.Result);

			return visitor.Result;
		}

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
					yield return new MethodDescriptor(mainMethod);
				}
			}
		}
        public IMethodSymbol FindMethod(MethodDescriptor methodDescriptor)
        {
            return RoslynSymbolFactory.FindMethodInCompilation(methodDescriptor, this.Compilation);
        }

		public async Task<Tuple<BaseMethodDeclarationSyntax, IMethodSymbol>> FindMethodSyntaxAsync(MethodDescriptor method)
		{
			var root = await this.SyntaxTree.GetRootAsync();
			var visitor = new MethodFinder(method, this.SemanticModel);
			visitor.Visit(root);

			if (visitor.Result != null)
			{
				return new Tuple<BaseMethodDeclarationSyntax, IMethodSymbol>(
					visitor.Result,
					(IMethodSymbol)this.SemanticModel.GetDeclaredSymbol(visitor.Result)
					);
			}
			else
			{
				return null;
			}
		}

		internal static async Task<CodeProvider> GetAsync(MethodDescriptor methodDescriptor)
		{
			Contract.Assert(CodeProvider.Solution != null);
			var cancellationSource = new CancellationTokenSource();
			var continuations = new List<Task<Compilation>>();
			foreach (var project in CodeProvider.Solution.Projects)
			{
				var compilation = await project.GetCompilationAsync(cancellationSource.Token);
				foreach (var tree in compilation.SyntaxTrees)
				{
					var codeProvider = new CodeProvider(tree, compilation);
					var pair = await codeProvider.FindMethodSyntaxAsync(methodDescriptor);
					if (pair != null)
					{
						// found it
						cancellationSource.Cancel();
						return codeProvider;
					}
				}
			}

			throw new ArgumentException("Cannot find a provider for " + methodDescriptor);
		}

		internal static CodeProvider GetProviderContainingEntryPoint(Compilation compilation, out IMethodSymbol mainSymbol)
		{
			mainSymbol = compilation.GetEntryPoint(new System.Threading.CancellationToken());

			foreach (var tree in compilation.SyntaxTrees)
			{
				var finder = new MethodFinder(mainSymbol, compilation.GetSemanticModel(tree));
				finder.Visit(tree.GetRoot());
				if (finder.Result != null)
				{
					return new CodeProvider(tree, compilation);
				}
			}

			return null;
		}

		internal static async Task<Tuple<CodeProvider, IMethodSymbol>> GetProviderContainingEntryPointAsync(Compilation compilation, CancellationToken cancellationToken = default(CancellationToken))
		{
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
							return new Tuple<CodeProvider, IMethodSymbol>(new CodeProvider(tree, compilation), mainSymbol);
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

		internal static async Task<Tuple<CodeProvider, IMethodSymbol>> GetProviderContainingEntryPointAsync(Solution solution, CancellationToken cancellationToken = default(CancellationToken))
		{
			var projectIDs = solution.GetProjectDependencyGraph().GetTopologicallySortedProjects(cancellationToken);
			var continuations = new BlockingCollection<Task<Tuple<CodeProvider, IMethodSymbol>>>();
			foreach (var projectId in projectIDs)
			{
				var project = solution.GetProject(projectId);
				var compilation = await project.GetCompilationAsync(cancellationToken);
				var pair = await CodeProvider.GetProviderContainingEntryPointAsync(compilation);
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
        /// <summary>
        /// This function is currently invoked in 2 places where there is no codeProvider. 6
        /// </summary>
        /// <param name="typeDescriptor1"></param>
        /// <param name="typeDescriptor2"></param>
        /// <param name="codeProvider"></param>
        /// <returns></returns>
        internal static bool IsSubtype(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2, CodeProvider codeProvider)
        {
            throw new NotImplementedException();
            // The problem is I need a compilation :-)
            //var roslynType1 = RoslynSymbolFactory.GetTypeByName(typeDescriptor1.TypeName, codeProvider.Compilation);
            //var roslynType2 = RoslynSymbolFactory.GetTypeByName(typeDescriptor2.TypeName, codeProvider.Compilation);
            //TypeHelper.InheritsByName(roslynType1, roslynType2);
        }

        internal bool IsSubtype(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        {
            var roslynType1 = RoslynSymbolFactory.GetTypeByName(typeDescriptor1.TypeName, this.Compilation);
            var roslynType2 = RoslynSymbolFactory.GetTypeByName(typeDescriptor2.TypeName, this.Compilation);
            return TypeHelper.InheritsByName(roslynType1, roslynType2);
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
}
