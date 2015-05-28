using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

        public MethodDescriptor FindMethodImplementation(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
        {
            var roslynMethod = FindMethod(methodDescriptor);
            if (roslynMethod != null)
            {
                var roslynType = RoslynSymbolFactory.GetTypeByName(typeDescriptor.TypeName, this.Compilation);
                var implementedMethod = Utils.FindMethodImplementation(roslynMethod, roslynType);
                return new MethodDescriptor(implementedMethod);
            }
            // If we cannot resolve the method, we return the same method.
            return methodDescriptor;
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
            // In some cases (e.g, default constructors or library methods, we are not going to find the code in the solution)
            // We should not throw an exception. Maybe return a dummy code Provider to let the analysis evolve
            // or an informative message in order to let the caller continue. We can declare the exception
			// throw new ArgumentException("Cannot find a provider for " + methodDescriptor);
            return null;
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

        internal bool IsSubtype(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        {
            var roslynType1 = RoslynSymbolFactory.GetTypeByName(typeDescriptor1.TypeName, this.Compilation);
            var roslynType2 = RoslynSymbolFactory.GetTypeByName(typeDescriptor2.TypeName, this.Compilation);
            return TypeHelper.InheritsByName(roslynType1, roslynType2);
        }

        internal Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
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
    internal class MethodFinder : CSharpSyntaxWalker
    {
        private MethodDescriptor MethodDescriptor;
        private SemanticModel SemanticModel;
        internal BaseMethodDeclarationSyntax Result { get; private set; }

        internal MethodFinder(MethodDescriptor descriptor, SemanticModel semanticModel)
        {
            this.MethodDescriptor = descriptor;
            this.SemanticModel = semanticModel;
        }

        internal MethodFinder(IMethodSymbol symbol, SemanticModel semanticModel)
        {
            Contract.Assert(symbol != null);
            Contract.Assert(semanticModel != null);

            this.MethodDescriptor = new MethodDescriptor(symbol);
            this.SemanticModel = semanticModel;
        }
        //public override object VisitCompilationUnit(CompilationUnitSyntax node)
        //{
        //    foreach (var member in node.Members)
        //    {
        //        Visit(member);
        //    }
        //    return null;
        //}
        //public override object VisitClassDeclaration(ClassDeclarationSyntax node)
        //{
        //    foreach (var member in node.Members)
        //    {
        //        Visit(member);
        //    }

        //    return null;
        //}

        public override void Visit(SyntaxNode syntax)
        {
            var kind = syntax.Kind();
            switch (kind)
            {
                case SyntaxKind.MethodDeclaration:
                    {
                        var node = (MethodDeclarationSyntax)syntax;
                        var symbol = this.SemanticModel.GetDeclaredSymbol(node);
                        var thisDescriptor = new MethodDescriptor(symbol);
                        if (thisDescriptor.Equals(this.MethodDescriptor))
                        {
                            // found it!
                            this.Result = node;
                        }
                        break;
                    }
                case SyntaxKind.ConstructorDeclaration:
                    {
                        var node = (ConstructorDeclarationSyntax)syntax;
                        var symbol = this.SemanticModel.GetDeclaredSymbol(node);
                        var thisDescriptor = new MethodDescriptor(symbol);
                        if (thisDescriptor.Equals(this.MethodDescriptor))
                        {
                            // found it!
                            this.Result = node;
                        }
                        break;
                    }
            }

            base.Visit(syntax);
        }
    }

}
