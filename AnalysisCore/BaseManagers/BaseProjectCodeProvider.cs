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
using ReachingTypeAnalysis.Roslyn;
using Microsoft.CodeAnalysis.CSharp;

namespace ReachingTypeAnalysis.Analysis
{
	internal class MethodParserInfo
	{
		public MethodDescriptor MethodDescriptor { get; private set; }
		public Document Document { get; set; }
		public SyntaxTree SyntaxTree { get; set; }
		public SemanticModel SemanticModel { get; set; }
		public BaseMethodDeclarationSyntax DeclarationNode { get; set; }
		public IMethodSymbol MethodSymbol { get; set; }

		public MethodParserInfo(MethodDescriptor methodDescriptor)
		{
			this.MethodDescriptor = methodDescriptor;
		}
	}

    public  abstract partial class BaseProjectCodeProvider : IProjectCodeProvider
    {
		protected Project project;
		protected Compilation compilation;
		private IDictionary<Guid, SemanticModel> semanticModels;

		protected BaseProjectCodeProvider(Project project, Compilation compilation)
        {
            this.project = project;
			this.compilation = compilation;
			this.semanticModels = new Dictionary<Guid, SemanticModel>();
		}

		public async Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor)
        {            
            var methodParserInfo = await this.FindMethodDeclarationAsync(methodDescriptor);
			MethodEntity methodEntity = null;

			if (methodParserInfo != null)
            {
                var methodEntityGenerator = new MethodParser(methodParserInfo);
                methodEntity = methodEntityGenerator.ParseMethod();
            }
            else
            {
                var methodEntityGenerator = new LibraryMethodParser(methodDescriptor);
                methodEntity = methodEntityGenerator.ParseMethod();
            }

            return methodEntity;
        }

		private async Task<MethodParserInfo> FindMethodDeclarationAsync(MethodDescriptor methodDescriptor)
		{
			MethodParserInfo result = null;

			foreach (var document in this.project.Documents)
			{
				var methodParserInfo = await this.FindMethodDeclarationAsync(methodDescriptor, document);

				if (methodParserInfo != null)
				{
					result = methodParserInfo;
					break;
				}
			}

			return result;
		}

        internal async Task ReplaceSource(string source, string name)
        {
			//var tree = SyntaxFactory.ParseSyntaxTree(sourceCode);
            var oldDocument = project.Documents.Single(doc => doc.Name == name);
            this.project = this.project.RemoveDocument(oldDocument.Id);       
            var newDocument = this.project.AddDocument(name, source);
            this.project = newDocument.Project;

            var cancelation = new CancellationTokenSource();
            this.compilation = await Utils.CompileProjectAsync(project, cancelation.Token);            
			var semanticModel = await newDocument.GetSyntaxTreeAsync(cancelation.Token);

			this.semanticModels.Remove(oldDocument.Id.Id);
			this.semanticModels .Add(newDocument.Id.Id, compilation.GetSemanticModel(semanticModel));
        }

		private async Task<MethodParserInfo> FindMethodDeclarationAsync(MethodDescriptor method, Document document)
		{
			MethodParserInfo result = null;
			var tree = await document.GetSyntaxTreeAsync();
			var root = await tree.GetRootAsync();
			var model = this.GetSemanticModel(document, tree);			
			var visitor = new MethodFinder(method, model);

			visitor.Visit(root);

			if (visitor.Result != null)
			{
				var declarationNode = visitor.Result;
				var symbol = model.GetDeclaredSymbol(declarationNode) as IMethodSymbol;

				result = new MethodParserInfo(method)
				{
					Document = document,
					SyntaxTree = tree,
					SemanticModel = model,
					DeclarationNode = declarationNode,
					MethodSymbol = symbol
				};
			}

			return result;
		}

		private SemanticModel GetSemanticModel(Document document, SyntaxTree tree)
		{
			SemanticModel model;

			if (!this.semanticModels.TryGetValue(document.Id.Id, out model))
			{
				model = this.compilation.GetSemanticModel(tree);
				semanticModels.Add(document.Id.Id, model);
			}

			return model;
		}

		public abstract Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor);

		public virtual Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        {
            var roslynType1 = RoslynSymbolFactory.GetTypeByName(typeDescriptor1, this.compilation);
            var roslynType2 = RoslynSymbolFactory.GetTypeByName(typeDescriptor2, this.compilation);

            return Task.FromResult(TypeHelper.InheritsByName(roslynType1, roslynType2));
        }

        public Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
        {
			var roslynMethod = RoslynSymbolFactory.FindMethodInCompilation(methodDescriptor, this.compilation);

			if (roslynMethod != null)
			{
				var roslynType = RoslynSymbolFactory.GetTypeByName(typeDescriptor, this.compilation);
				var implementedMethod = Utils.FindMethodImplementation(roslynMethod, roslynType);
				Contract.Assert(implementedMethod != null);
				methodDescriptor = Utils.CreateMethodDescriptor(implementedMethod);
			}

			// If we cannot resolve the method, we return the same method.
			return Task.FromResult(methodDescriptor);
		}

        public Task<IEnumerable<MethodDescriptor>> GetRootsAsync()
        {
            var result = new HashSet<MethodDescriptor>();
            var cancellationTokenSource = new CancellationTokenSource();
            var mainMethod = this.compilation.GetEntryPoint(cancellationTokenSource.Token);

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
			return CodeGraphHelper.GetDocumentsAsync(this.project);
		}

		public Task<IEnumerable<CodeGraphModel.FileResponse>> GetDocumentEntitiesAsync(string filePath)
		{
			var document = this.project.Documents.Single(doc => doc.FilePath.EndsWith(filePath, StringComparison.InvariantCultureIgnoreCase));
			return CodeGraphHelper.GetDocumentEntitiesAsync(document);
        }

		public virtual async Task<PropagationEffects> RemoveMethodAsync(MethodDescriptor methodDescriptor)
		{
			var methodEntity = await this.GetMethodEntityAsync(methodDescriptor);
			// compute delete effects in methodentitywithProp
			/// for each callnode , cgetInvocationInfo. get retinfo new propEffects(callsInvoInfo, retInfo)
			/// 
			var propagationEffects = await methodEntity.RemoveMethodAsync();
			return propagationEffects;
		}
	}
}
