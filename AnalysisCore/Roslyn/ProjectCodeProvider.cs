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

namespace ReachingTypeAnalysis.Roslyn
{
    public class ProjectCodeProvider: IProjectCodeProvider
    {
        internal Compilation Compilation { get; private set; }
        internal Project Project { get; private set; }
        //public SemanticModel SemanticModel { get; private set; }
        public static Solution Solution { get; internal set; }
        
        internal ProjectCodeProvider(Project project, Compilation compilation)
        {
            this.Project = project;
            this.Compilation = compilation;
        }

        internal static async Task<IProjectCodeProvider> ProjectCodeProviderAsync(string fullPath)
        {
			var cancellationTokenSource = new CancellationTokenSource();
            var project = await Utils.ReadProjectAsync(fullPath);

            if (project !=  null)
            {
				var compilation = await CompileProjectAsync(project, cancellationTokenSource.Token);
                return new ProjectCodeProvider(project, compilation);
            }
			//foreach (var id in Solution.ProjectIds)
			//{
			//    var project = Solution.GetProject(id);
			//    if(project.FilePath.Equals(fullPath))
			//    {
			//        var compilation = await CompileProjectAsync(project, cancellationTokenSource.Token);
			//        return new ProjectCodeProvider(project, compilation);
			//    }
			//}
			Contract.Assert(false, "Can't find path = " + fullPath);
            return null;
        }

        internal static async Task<ProjectCodeProvider> ProjectCodeProviderByNameAsync(Solution solution, string name)
        {
			var cancellationTokenSource = new CancellationTokenSource();

            foreach (var id in solution.ProjectIds)
            {
                var project = solution.GetProject(id);

                //if (project.Name.Equals(name)) 
				if (project.AssemblyName.Equals(name)) 
                {
					var compilation = await CompileProjectAsync(project, cancellationTokenSource.Token);
                    return new ProjectCodeProvider(project, compilation);
                }
            }

            Contract.Assert(false, "Can't find project named = " + name);
            return null;
        }

        #region ICodeProvider Implementation

        public async Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor)
        {
            MethodEntity methodEntity = null;
            var tree = await this.GetSyntaxAsync(methodDescriptor);
            if (tree != null)
            {
                var model = this.Compilation.GetSemanticModel(tree);
                var methodEntityGenerator = new MethodParser(model, this, tree, methodDescriptor);
                methodEntity = methodEntityGenerator.ParseMethod();
            }
            else
            {
                var methodEntityGenerator = new LibraryMethodParser(methodDescriptor);
                methodEntity = methodEntityGenerator.ParseMethod();
            }
            return methodEntity;
        }
		
        public virtual Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        {
            var roslynType1 = RoslynSymbolFactory.GetTypeByName(typeDescriptor1, this.Compilation);
            var roslynType2 = RoslynSymbolFactory.GetTypeByName(typeDescriptor2, this.Compilation);

            return Task.FromResult(TypeHelper.InheritsByName(roslynType1, roslynType2));
        }

        public Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
        {
            return Task.FromResult<MethodDescriptor>(FindMethodImplementation(methodDescriptor, typeDescriptor));
        }

        public MethodDescriptor FindMethodImplementation(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
        {
            var roslynMethod = FindMethod(methodDescriptor);

            if (roslynMethod != null)
            {
                var roslynType = RoslynSymbolFactory.GetTypeByName(typeDescriptor, this.Compilation);
                var implementedMethod = Utils.FindMethodImplementation(roslynMethod, roslynType);
                Contract.Assert(implementedMethod != null);
                return Utils.CreateMethodDescriptor(implementedMethod);
            }

            // If we cannot resolve the method, we return the same method.
            return methodDescriptor;
        }

        public IMethodSymbol FindMethod(MethodDescriptor methodDescriptor)
        {
            return RoslynSymbolFactory.FindMethodInCompilation(methodDescriptor, this.Compilation);
        }

        public Task<IEnumerable<MethodDescriptor>> GetRootsAsync()
        {
            var result = new HashSet<MethodDescriptor>();
            var cancellationTokenSource = new CancellationTokenSource();
            var mainMethod = this.Compilation.GetEntryPoint(cancellationTokenSource.Token);

            if (mainMethod != null)
            {
                // only return if there's a main method
                var methodDescriptor = Utils.CreateMethodDescriptor(mainMethod);
                result.Add(methodDescriptor);
            }

            return Task.FromResult<IEnumerable<MethodDescriptor>>(result);
        }

		public Task<IEnumerable<CodeGraphModel.FileResponse>> GetDocumentsAsync()
		{
			var result = new List<CodeGraphModel.FileResponse>();

			foreach (var document in this.Project.Documents)
			{
				var fileResponse = CreateFileResponse(document);

				if (fileResponse != null)
				{
					result.Add(fileResponse);
				}
            }

			return Task.FromResult(result.AsEnumerable());
		}

		private CodeGraphModel.FileResponse CreateFileResponse(Document document)
		{
			if (document.Name.StartsWith(".NETFramework,")) return null;
			var buildInfo = new CodeGraphModel.BuildInfo();
			var filepath = document.FilePath;

			// TODO: Hack!!!
			var dir = @"C:\Users\t-edzopp\Desktop\ArcusClientPrototype\src\ArcusClient\data\";

			if (filepath.StartsWith(dir))
			{
				filepath = filepath.Replace(dir, string.Empty);
			}

			filepath = filepath.Replace(@"\", "/");

			var result = new CodeGraphModel.FileResponse()
			{
				uid = document.Id.Id.ToString(),
				filepath = filepath,
				repository = buildInfo.RepositoryName,
				version = buildInfo.VersionName
			};

			return result;
        }

		#endregion

		/// <summary>
		/// This is use by the Solution Grain? to get the main methods of the solution
		/// </summary>
		/// <param name="solution"></param>
		/// <returns></returns>
		public static async Task<IEnumerable<MethodDescriptor>> GetMainMethodsAsync(Solution solution)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var tasks = new List<Task<Compilation>>();

            foreach (var project in solution.Projects)
            {
                var compilation = CompileProjectAsync(project, cancellationTokenSource.Token);
                tasks.Add(compilation);
            }

            await Task.WhenAll(tasks);
            var result = new HashSet<MethodDescriptor>();

            foreach (var task in tasks)
            {
                var mainMethod = task.Result.GetEntryPoint(cancellationTokenSource.Token);

                if (mainMethod != null)
                {
                    // only return if there's a main method
                    var methodDescriptor = Utils.CreateMethodDescriptor(mainMethod);
                    result.Add(methodDescriptor);
                }
            }

            return result;
        }
 
        /// <summary>
        /// This method is used by the class MethodParser search for the method declaration syntax
        /// of the Method to Analyze
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        internal Task<Tuple<BaseMethodDeclarationSyntax, IMethodSymbol>> FindMethodSyntaxAndSymbolAsync(SyntaxTree tree, MethodDescriptor method)
        {
            var model = this.Compilation.GetSemanticModel(tree);
            return ProjectCodeProvider.FindMethodSyntaxAsync(model, tree, method);
        }

        public static async Task<Tuple<BaseMethodDeclarationSyntax, IMethodSymbol>> FindMethodSyntaxAsync(SemanticModel model, SyntaxTree tree, MethodDescriptor method)
        {
            var root = await tree.GetRootAsync();
            var visitor = new MethodFinder(method, model);
            visitor.Visit(root);

            if (visitor.Result != null)
            {
                return new Tuple<BaseMethodDeclarationSyntax, IMethodSymbol>(visitor.Result, (IMethodSymbol)model.GetDeclaredSymbol(visitor.Result));
            }
            else
            {
                return null;
            }
        }
        

        //public static async Task<IProjectCodeProviderGrain> GetCodeProviderGrainAsync(MethodDescriptor methodDescriptor, Solution solution, IGrainFactory grainFactory)
        //{
        //    var cancellationTokenSource = new CancellationTokenSource();
        //    var continuations = new List<Task<Compilation>>();

        //    foreach (var project in solution.Projects)
        //    {
        //        var compilation = await CompileProjectAsync(project, cancellationTokenSource.Token);

        //        foreach (var tree in compilation.SyntaxTrees)
        //        {
        //            var model = compilation.GetSemanticModel(tree);
        //            var codeProvider = new ProjectCodeProvider(project, compilation);
        //            var pair = await ProjectCodeProvider.FindMethodSyntaxAsync(model, tree, methodDescriptor);

        //            if (pair != null)
        //            {
        //                // found it
        //                var codeProviderGrain = grainFactory.GetGrain<IProjectCodeProviderGrain>(project.Name);
        //                return codeProviderGrain;
        //            }
        //        }
        //    }

        //    return grainFactory.GetGrain<IProjectCodeProviderGrain>("DUMMY");
        //}
        //public virtual bool IsSubtype(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        //{
        //    var roslynType1 = RoslynSymbolFactory.GetTypeByName(typeDescriptor1, this.Compilation);
        //    var roslynType2 = RoslynSymbolFactory.GetTypeByName(typeDescriptor2, this.Compilation);

        //    return TypeHelper.InheritsByName(roslynType1, roslynType2);
        //}

		internal static async Task<Compilation> CompileProjectAsync(Project project, CancellationToken cancellationToken)
		{
			Console.WriteLine("Project language {0}", project.Language);
			if (project.Language == "Visual Basic") return null;

			var compilation = await project.GetCompilationAsync(cancellationToken);
			var diagnostics = compilation.GetDiagnostics();

			// print compilation errors and warnings
			var errors = from diagnostic in diagnostics
						 where diagnostic.Severity == DiagnosticSeverity.Error
						 select diagnostic.ToString();

			if (errors.Any())
			{
				Console.WriteLine("Failed to compile project {0}", project.Name);
				var fileName = string.Format("roslyn compilation errors for {0}.txt", project.Name);

				File.WriteAllLines(fileName, errors);
			}
			else
			{
				Console.WriteLine("Project {0} compiled successfully", project.Name);
			}

			return compilation;
		}

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

        internal async Task<SyntaxTree> GetSyntaxAsync(MethodDescriptor methodDescriptor)
        {
            var cancellationSource = new CancellationTokenSource();

            foreach (var tree in this.Compilation.SyntaxTrees)
            {
                var model = this.Compilation.GetSemanticModel(tree);
                var pair = await ProjectCodeProvider.FindMethodSyntaxAsync(model, tree, methodDescriptor);

                if (pair != null)
                {
                    // found it
                    return tree;
                }
            }

            return null;
        }

        #endregion
    }

    internal class DummyCodeProvider : IProjectCodeProvider
    {
        public Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        {
            return Task.FromResult(true);
        }

        public Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
        {
            return Task.FromResult(methodDescriptor);
        }

        public bool IsSubtype(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        {
            return true;
        }

        public MethodDescriptor FindMethodImplementation(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
        {
            return methodDescriptor;
        }

        public Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor)
        {
            var libraryMethodVisitor = new ReachingTypeAnalysis.Roslyn.LibraryMethodParser(methodDescriptor);
            var methodEntity = libraryMethodVisitor.ParseMethod();
            return Task.FromResult<IEntity>(methodEntity);
        }

        public Task<IEnumerable<MethodDescriptor>> GetRootsAsync()
        {
            var result = new HashSet<MethodDescriptor>();
            return Task.FromResult(result.AsEnumerable());
        }

		public Task<IEnumerable<CodeGraphModel.FileResponse>> GetDocumentsAsync()
		{
			var result = new HashSet<CodeGraphModel.FileResponse>();
			return Task.FromResult(result.AsEnumerable());
		}
	}
}
