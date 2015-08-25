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
using Microsoft.CodeAnalysis.CSharp;

using DocumentPath = System.String;
using Orleans;

namespace ReachingTypeAnalysis.Analysis
{
	public class DocumentInfo
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
		protected string projectPath;
		private Project project;
		private Project newProject;
		private Compilation compilation;
		private Compilation newCompilation;
		private IDictionary<DocumentPath, DocumentInfo> documentsInfo;
		private IDictionary<DocumentPath, DocumentInfo> newDocumentsInfo;
		protected bool useNewFieldsVersion;

		protected BaseProjectCodeProvider()
        {
			this.documentsInfo = new Dictionary<DocumentPath, DocumentInfo>();
        }

		protected Project Project
		{
			get { return useNewFieldsVersion ? newProject : project; }
		}

		protected Compilation Compilation
		{
			get { return useNewFieldsVersion ? newCompilation : compilation; }
		}

		protected IDictionary<DocumentPath, DocumentInfo> DocumentsInfo
		{
			get { return useNewFieldsVersion ? newDocumentsInfo : documentsInfo; }
		}

		protected async Task LoadProjectAsync(string projectPath)
		{
			var cancellationTokenSource = new CancellationTokenSource();
			this.projectPath = projectPath;
			this.project = await Utils.ReadProjectAsync(projectPath);			
			this.compilation = await Utils.CompileProjectAsync(this.Project, cancellationTokenSource.Token);
		}

		protected async Task LoadSourceAsync(string source, string assemblyName)
		{
			var cancellationTokenSource = new CancellationTokenSource();
			var solution = Utils.CreateSolution(source);
			this.project = solution.Projects.Single(p => p.AssemblyName == assemblyName);
			this.compilation = await Utils.CompileProjectAsync(this.Project, cancellationTokenSource.Token);
		}

		protected Task LoadTestAsync(string testName, string assemblyName)
		{
			var source = TestSources.BasicTestsSources.Test[testName];
			return this.LoadSourceAsync(source, assemblyName);
        }

		public async Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor)
        {
			// TODO: We need to visit each document AST only once, maybe on demand the first time this method is called
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
			DocumentInfo documentInfo = null;

			if (!this.DocumentsInfo.TryGetValue(documentPath, out documentInfo))
            {
				var document = this.Project.Documents.SingleOrDefault(doc => doc.FilePath.Equals(documentPath, StringComparison.InvariantCultureIgnoreCase));

				if (document != null)
				{
					documentInfo = await DocumentInfo.CreateAsync(document, this.Compilation);
					this.DocumentsInfo.Add(documentPath, documentInfo);
				}
            }

			return documentInfo;
        }

		private async Task<MethodParserInfo> FindMethodDeclarationAsync(MethodDescriptor methodDescriptor)
		{
			MethodParserInfo result = null;

			foreach (var document in this.Project.Documents)
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
				result = visitor.Result;
				documentInfo.Methods.Add(methodDescriptor);
			}

			return result;
		}

		public abstract Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor);

		public virtual Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        {
            var roslynType1 = RoslynSymbolFactory.GetTypeByName(typeDescriptor1, this.Compilation);
            var roslynType2 = RoslynSymbolFactory.GetTypeByName(typeDescriptor2, this.Compilation);

            return Task.FromResult(TypeHelper.InheritsByName(roslynType1, roslynType2));
        }

        public Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
        {
			var roslynMethod = RoslynSymbolFactory.FindMethodInCompilation(methodDescriptor, this.Compilation);

			if (roslynMethod != null)
			{
				var roslynType = RoslynSymbolFactory.GetTypeByName(typeDescriptor, this.Compilation);
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

		// Old version using DocumentVisitor
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

        internal Task<IEnumerable<MethodDescriptor>> GetAllMethodDescriptors()
        {
            var result = new HashSet<MethodDescriptor>();
            foreach(var documentPath in this.DocumentsInfo.Keys)
            {
                var documentInfo = this.DocumentsInfo[documentPath];
                result.UnionWith(documentInfo.Methods);
            }
            return Task.FromResult(result.AsEnumerable());
        }

		public virtual async Task<PropagationEffects> RemoveMethodAsync(MethodDescriptor methodDescriptor)
		{
			var methodEntity = await this.GetMethodEntityAsync(methodDescriptor);
			var propagationEffects = await methodEntity.RemoveMethodAsync();

			this.RemoveMethodFromDocumentInfo(methodDescriptor);
            return propagationEffects;
		}

		public async Task ReplaceDocumentSourceAsync(string source, string documentPath)
		{
			//var tree = SyntaxFactory.ParseSyntaxTree(sourceCode);
			var oldDocument = this.Project.Documents.Single(doc => doc.FilePath == documentPath);

			this.RemoveDocumentInfo(oldDocument.FilePath);

			this.project = this.Project.RemoveDocument(oldDocument.Id);
			var newDocument = project.AddDocument(oldDocument.Name, source, null, oldDocument.FilePath);
			this.project = newDocument.Project;

			var cancellationTokenSource = new CancellationTokenSource();
			this.compilation = await Utils.CompileProjectAsync(project, cancellationTokenSource.Token);
			//var semanticModel = await newDocument.GetSyntaxTreeAsync(cancellationTokenSource.Token);

			//this.semanticModels.Remove(oldDocument.Id.Id);
			//this.semanticModels.Add(newDocument.Id.Id, compilation.GetSemanticModel(semanticModel));
		}

		public Task ReplaceDocumentAsync(string documentPath, string newDocumentPath = null)
		{
			if (newDocumentPath == null)
			{
				newDocumentPath = documentPath;
			}

			var sourceText = File.ReadAllText(newDocumentPath);
			return this.ReplaceDocumentSourceAsync(sourceText, documentPath);
		}

		//public Task ReplaceDocumentAsync(string documentPath)
		//{
		//	var source = File.ReadAllText(documentPath);
		//	return this.ReplaceDocumentSourceAsync(source, documentPath);
		//}

		private void RemoveDocumentInfo(string documentPath)
		{
			DocumentInfo documentInfo;

			if (this.DocumentsInfo.TryGetValue(documentPath, out documentInfo))
			{
				this.DocumentsInfo.Remove(documentPath);
			}
		}

		private void RemoveMethodFromDocumentInfo(MethodDescriptor methodDescriptor)
		{
			foreach (var documentInfo in this.DocumentsInfo.Values)
			{
				var methodFound = documentInfo.Methods.Remove(methodDescriptor);
				if (methodFound) break;
			}
		}

		public virtual async Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			var cancellationTokenSource = new CancellationTokenSource();
			var result = new List<MethodModification>();
			var documentDiff = new DocumentDiff();

			this.newProject = await Utils.ReadProjectAsync(projectPath);
			this.newCompilation = await Utils.CompileProjectAsync(newProject, cancellationTokenSource.Token);
			this.newDocumentsInfo = new Dictionary<DocumentPath, DocumentInfo>(this.DocumentsInfo);

			foreach (var documentPath in modifiedDocuments)
			{
				var oldDocumentInfo = await this.GetDocumentInfoAsync(documentPath);
				this.useNewFieldsVersion = true;

				var newDocumentInfo = await this.GetDocumentInfoAsync(documentPath);
				this.useNewFieldsVersion = false;

				var modifications = documentDiff.GetDifferences(oldDocumentInfo, newDocumentInfo);
				result.AddRange(modifications);
			}

			return result;
		}

		public virtual Task ReloadAsync()
		{
			if (newProject != null)
			{
				this.project = newProject;
				this.newProject = null;
			}

			if (newCompilation != null)
			{
				this.compilation = newCompilation;
				this.newCompilation = null;
			}

			if (newDocumentsInfo != null)
			{
				this.documentsInfo = newDocumentsInfo;
				this.newDocumentsInfo = null;
			}

			return TaskDone.Done;
		}
	}
}
