using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using ReachingTypeAnalysis.Analysis;
using System.IO;

namespace ReachingTypeAnalysis.Roslyn
{
    public partial class ProjectCodeProvider : IProjectCodeProvider
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

        #region IProjectCodeProvider Implementation

        public async Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor)
        {
            MethodEntity methodEntity = null;
            var tree = await this.GetSyntaxAsync(methodDescriptor);
            if (tree != null)
            {
                var model = this.Compilation.GetSemanticModel(tree);
                var methodEntityGenerator = new MethodParser(model, tree, methodDescriptor);
                methodEntity = methodEntityGenerator.ParseMethod();
            }
            else
            {
                var methodEntityGenerator = new LibraryMethodParser(methodDescriptor);
                methodEntity = methodEntityGenerator.ParseMethod();
            }
            return methodEntity;
        }

		internal async Task<SyntaxTree> GetSyntaxAsync(MethodDescriptor methodDescriptor)
		{
			var cancellationSource = new CancellationTokenSource();

			foreach (var doc in this.Project.Documents)
			{
				var tree = await doc.GetSyntaxTreeAsync();
				var model = this.GetSemanticModel(doc, tree);
				var pair = await ProjectCodeProvider.FindMethodSyntaxAsync(model, tree, methodDescriptor);

				if (pair != null)
				{
					// found it
					return tree;
				}
			}

			//foreach (var tree in this.Compilation.SyntaxTrees)
			//{
			//	var model = this.Compilation.GetSemanticModel(tree);
			//	var pair = await ProjectCodeProvider.FindMethodSyntaxAsync(model, tree, methodDescriptor);

			//	if (pair != null)
			//	{
			//		// found it
			//		return tree;
			//	}
			//}

			return null;
		}

		private IDictionary<Guid, SemanticModel> semanticModelMap = new Dictionary<Guid, SemanticModel>();

		private SemanticModel GetSemanticModel(Document document, SyntaxTree tree)
		{
			SemanticModel model;

			if (!this.semanticModelMap.TryGetValue(document.Id.Id, out model))
			{
				model = this.Compilation.GetSemanticModel(tree);
				semanticModelMap.Add(document.Id.Id, model);
			}
			return model;
		}

		public async Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			var methodEntity = await this.CreateMethodEntityAsync(methodDescriptor) as MethodEntity;
			var result = new MethodEntityWithPropagator(methodEntity, this);
			return result;
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

        public Task<IEnumerable<MethodDescriptor>> GetRootsAsync(AnalysisRootKind rootKind = AnalysisRootKind.Default)
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

            return Task.FromResult(result.AsEnumerable());
        }

		public Task<IEnumerable<CodeGraphModel.FileResponse>> GetDocumentsAsync()
		{
			return CodeGraphHelper.GetDocumentsAsync(this.Project);
		}

		public Task<IEnumerable<CodeGraphModel.FileResponse>> GetDocumentEntitiesAsync(string filePath)
		{
			var document = this.Project.Documents.Single(doc => doc.FilePath.EndsWith(filePath, StringComparison.InvariantCultureIgnoreCase));
			return CodeGraphHelper.GetDocumentEntitiesAsync(document);
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="model"></param>
		/// <param name="tree"></param>
		/// <param name="method"></param>
		/// <returns></returns>
		public static async Task<Tuple<BaseMethodDeclarationSyntax, IMethodSymbol>> FindMethodSyntaxAsync(SemanticModel model, SyntaxTree tree, MethodDescriptor method)
        {
            var root = await tree.GetRootAsync();
            var visitor = new MethodFinder(method, model);
            visitor.Visit(root);

            if (visitor.Result != null)
            {
				return new Tuple<BaseMethodDeclarationSyntax, IMethodSymbol>(visitor.Result.DeclarationNode, visitor.Result.MethodSymbol);
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
			//Console.WriteLine("Project language {0}", project.Language);
			if (project.Language == "Visual Basic") return null;

			var compilation = await project.GetCompilationAsync(cancellationToken);
			var diagnostics = compilation.GetDiagnostics();

			// print compilation errors and warnings
			var errors = from diagnostic in diagnostics
						 where diagnostic.Severity == DiagnosticSeverity.Error
						 select diagnostic.ToString();

			if (errors.Any())
			{
				var fileName = string.Format("roslyn compilation errors for {0}.txt", project.Name);

				Console.WriteLine("Failed to compile project {0}", project.Name);
				Console.WriteLine("To see the error log open file: {0}", fileName);				

				File.WriteAllLines(fileName, errors);
			}
			else
			{
				Console.WriteLine("Project {0} compiled successfully", project.Name);
			}

			return compilation;
		}

		public Task<PropagationEffects> RemoveMethodAsync(MethodDescriptor methodToUpdate)
		{
			throw new NotImplementedException();
		}

		public Task ReplaceDocumentSourceAsync(string source, string documentPath)
		{
			throw new NotImplementedException();
		}

		public Task ReplaceDocumentAsync(string documentPath, string newDocumentPath=null)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			throw new NotImplementedException();
		}

		public Task ReloadAsync()
		{
			throw new NotImplementedException();
		}

		public Task<PropagationEffects> AddMethodAsync(MethodDescriptor methodToAdd)
		{
			throw new NotImplementedException();
		}

		public Task<CodeGraphModel.SymbolReference> GetDeclarationInfoAsync(MethodDescriptor methodDescriptor)
		{
			throw new NotImplementedException();
		}

		public Task<CodeGraphModel.SymbolReference> GetInvocationInfoAsync(CallContext callContext)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<TypeDescriptor>> GetCompatibleInstantiatedTypesAsync(TypeDescriptor type)
		{
			var result = new List<TypeDescriptor>() { type };
			return Task.FromResult(result.AsEnumerable());
		}

		public Task<IEnumerable<MethodDescriptor>> GetReachableMethodsAsync()
		{
			throw new NotImplementedException();
		}

        public Task<int> GetReachableMethodsCountAsync()
        {
            throw new NotImplementedException();
        }
    }
}
