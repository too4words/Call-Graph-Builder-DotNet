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
	internal class DocumentInfo
	{
		public Document Document { get; private set; }
		public SemanticModel SemanticModel { get; private set; }		
		public SyntaxTree SyntaxTree { get; private set; }
		public SyntaxNode SyntaxTreeRoot { get; private set; }
		public ISet<MethodDescriptor> Methods { get; private set; }

		private DocumentInfo()
		{
			this.Methods = new HashSet<MethodDescriptor>();
        }

		public static async Task<DocumentInfo> CreateAsync(Document document, Compilation compilation)
		{
			var cancellationTokenSource = new CancellationTokenSource();
			var syntaxTree = await document.GetSyntaxTreeAsync(cancellationTokenSource.Token);
			var syntaxTreeRoot = await syntaxTree.GetRootAsync(cancellationTokenSource.Token);
			var semanticModel = compilation.GetSemanticModel(syntaxTree);

			var result = new DocumentInfo()
			{
				Document = document,
				SyntaxTree = syntaxTree,
				SyntaxTreeRoot = syntaxTreeRoot,
                SemanticModel = semanticModel
			};

			return result;
		}
	}

    public abstract partial class BaseProjectCodeProvider : IProjectCodeProvider
    {
		protected Project project;
		protected Compilation compilation;
		private IDictionary<string, DocumentInfo> documentsInfo;

		protected BaseProjectCodeProvider(Project project, Compilation compilation)
        {
            this.project = project;
			this.compilation = compilation;
			this.documentsInfo = new Dictionary<string, DocumentInfo>();
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

		private async Task<DocumentInfo> GetDocumentInfoAsync(string documentPath)
		{
			DocumentInfo documentInfo;

			if (!this.documentsInfo.TryGetValue(documentPath, out documentInfo))
            {
				var document = this.project.Documents.Single(doc => doc.FilePath.Equals(documentPath, StringComparison.InvariantCultureIgnoreCase));
				documentInfo = await DocumentInfo.CreateAsync(document, this.compilation);
				this.documentsInfo.Add(documentPath, documentInfo);
            }

			return documentInfo;
        }

		private async Task<MethodParserInfo> FindMethodDeclarationAsync(MethodDescriptor methodDescriptor)
		{
			MethodParserInfo result = null;

			foreach (var document in this.project.Documents)
			{
				var methodParserInfo = await this.FindMethodDeclarationAsync(methodDescriptor, document.FilePath);

				if (methodParserInfo != null)
				{
					result = methodParserInfo;
					break;
				}
			}

			return result;
		}

		private async Task<MethodParserInfo> FindMethodDeclarationAsync(MethodDescriptor methodDescriptor, string documentPath)
		{
			MethodParserInfo result = null;
			var documentInfo = await this.GetDocumentInfoAsync(documentPath);
			var document = documentInfo.Document;
            var tree = documentInfo.SyntaxTree;			
			var root = documentInfo.SyntaxTreeRoot;
			var model = documentInfo.SemanticModel;
			var visitor = new MethodFinder(methodDescriptor, model);

			visitor.Visit(root);

			if (visitor.Result != null)
			{
				var declarationNode = visitor.Result;
				var symbol = model.GetDeclaredSymbol(declarationNode) as IMethodSymbol;

				result = new MethodParserInfo(methodDescriptor)
				{
					SemanticModel = model,
					DeclarationNode = declarationNode,
					MethodSymbol = symbol
				};

				documentInfo.Methods.Add(methodDescriptor);
			}

			return result;
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

		//public Task<IEnumerable<CodeGraphModel.FileResponse>> GetDocumentEntitiesAsync(string documentPath)
		//{
		//	var document = this.project.Documents.Single(doc => doc.FilePath.EndsWith(documentPath, StringComparison.InvariantCultureIgnoreCase));
		//	return CodeGraphHelper.GetDocumentEntitiesAsync(document);
		//}

		public async Task<IEnumerable<CodeGraphModel.FileResponse>> GetDocumentEntitiesAsync(string documentPath)
		{
			var documentInfo = await this.GetDocumentInfoAsync(documentPath);
			var result = await CodeGraphHelper.GetDocumentEntitiesAsync(this, documentInfo);
			return result;
		}

		public virtual async Task<PropagationEffects> RemoveMethodAsync(MethodDescriptor methodDescriptor)
		{
			var methodEntity = await this.GetMethodEntityAsync(methodDescriptor);
			var propagationEffects = await methodEntity.RemoveMethodAsync();

			this.RemoveMethodFromDocumentInfo(methodDescriptor);
            return propagationEffects;
		}

		internal async Task ReplaceSourceAsync(string source, string documentName)
		{
			//var tree = SyntaxFactory.ParseSyntaxTree(sourceCode);
			var oldDocument = project.Documents.Single(doc => doc.Name == documentName);

			this.RemoveDocumentInfo(oldDocument.FilePath);

			this.project = this.project.RemoveDocument(oldDocument.Id);
			var newDocument = this.project.AddDocument(documentName, source, null, oldDocument.FilePath);
			this.project = newDocument.Project;

			var cancellationTokenSource = new CancellationTokenSource();
			this.compilation = await Utils.CompileProjectAsync(project, cancellationTokenSource.Token);
			//var semanticModel = await newDocument.GetSyntaxTreeAsync(cancellationTokenSource.Token);

			//this.semanticModels.Remove(oldDocument.Id.Id);
			//this.semanticModels.Add(newDocument.Id.Id, compilation.GetSemanticModel(semanticModel));
		}

		private void RemoveDocumentInfo(string documentPath)
		{
			DocumentInfo documentInfo;

			if (this.documentsInfo.TryGetValue(documentPath, out documentInfo))
			{
				this.documentsInfo.Remove(documentPath);
			}
		}

		private void RemoveMethodFromDocumentInfo(MethodDescriptor methodDescriptor)
		{
			foreach (var documentInfo in this.documentsInfo.Values)
			{
				var methodFound = documentInfo.Methods.Remove(methodDescriptor);
				if (methodFound) break;
			}
		}
	}
}
