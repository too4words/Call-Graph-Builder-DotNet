using Microsoft.CodeAnalysis;
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

using DocumentPath = System.String;
using Orleans;
using System.Text;

namespace ReachingTypeAnalysis.Analysis
{
	public class DocumentInfo
	{
		public Document Document { get; private set; }
		public SemanticModel SemanticModel { get; private set; }		
		public SyntaxTree SyntaxTree { get; private set; }
		public SyntaxNode SyntaxTreeRoot { get; private set; }
		public IDictionary<MethodDescriptor, MethodParserInfo> DeclaredMethods { get; private set; }

		public static async Task<DocumentInfo> CreateAsync(Document document, Compilation compilation)
		{
			var cancellationTokenSource = new CancellationTokenSource();
			var syntaxTree = await document.GetSyntaxTreeAsync(cancellationTokenSource.Token);
			var syntaxTreeRoot = await syntaxTree.GetRootAsync(cancellationTokenSource.Token);
			var semanticModel = compilation.GetSemanticModel(syntaxTree);

			var visitor = new MethodFinder(semanticModel);
			visitor.Visit(syntaxTreeRoot);

			var result = new DocumentInfo()
			{
				Document = document,
				SyntaxTree = syntaxTree,
				SyntaxTreeRoot = syntaxTreeRoot,
                SemanticModel = semanticModel,
				DeclaredMethods = visitor.DeclaredMethods
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
		protected ISolutionManager solutionManager;
        protected IRtaManager rtaManager;

		protected BaseProjectCodeProvider(ISolutionManager solutionManager, IRtaManager rtaManager)
        {
			this.solutionManager = solutionManager;
            this.rtaManager = rtaManager;
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
			var solution = SolutionFileGenerator.CreateSolution(source);
			this.project = solution.Projects.Single(p => p.AssemblyName == assemblyName);
			this.compilation = await Utils.CompileProjectAsync(this.Project, cancellationTokenSource.Token);
		}

		protected Task LoadTestAsync(string testName, string assemblyName)
		{
			var source = TestSources.BasicTestsSources.Test[testName];
			return this.LoadSourceAsync(source, assemblyName);
        }

		public virtual async Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor)
        {
            var methodParserInfo = await this.FindMethodInProjectAsync(methodDescriptor);
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

            // this is for RTA analysis
            if (methodEntity.InstantiatedTypes.Count > 0)
            {
                await this.rtaManager.AddInstantiatedTypesAsync(methodEntity.InstantiatedTypes);
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
		
		private async Task<MethodParserInfo> FindMethodInProjectAsync(MethodDescriptor methodDescriptor)
		{
			MethodParserInfo result = null;

			foreach (var document in this.Project.Documents)
			{
				var methodParserInfo = await this.FindMethodInDocumentAsync(methodDescriptor, document.FilePath);

				if (methodParserInfo != null)
				{
					result = methodParserInfo;
					break;
				}
			}

			return result;
		}

		private async Task<MethodParserInfo> FindMethodInDocumentAsync(MethodDescriptor methodDescriptor, string documentPath)
		{
			MethodParserInfo result = null;
			var documentInfo = await this.GetDocumentInfoAsync(documentPath);

			documentInfo.DeclaredMethods.TryGetValue(methodDescriptor, out result);
			return result;
		}

		public abstract Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor);

		public abstract Task<IEnumerable<MethodDescriptor>> GetReachableMethodsAsync();

        public abstract Task<int> GetReachableMethodsCountAsync();

		public abstract Task<MethodDescriptor> GetRandomMethodAsync();

		public abstract Task<bool> IsReachableAsync(MethodDescriptor methodDescriptor);

		public virtual async Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        {
			var result = false;

			if (typeDescriptor1.Equals(typeDescriptor2))
			{
				result = true;
			}
			else
			{
				var roslynType1 = RoslynSymbolFactory.GetTypeByName(typeDescriptor1, this.Compilation);

				if (roslynType1 == null && typeDescriptor1.AssemblyName != this.Project.AssemblyName)
				{
					// We assume if T1 <= T2, then the project (compilation) where T1 is declared must know T2 also
					var projectProvider = await this.solutionManager.GetProjectCodeProviderAsync(typeDescriptor1.AssemblyName);

					result = await projectProvider.IsSubtypeAsync(typeDescriptor1, typeDescriptor2);
				}
				else
				{
					var roslynType2 = RoslynSymbolFactory.GetTypeByName(typeDescriptor2, this.Compilation);

					if (roslynType2 == null)
					{
						// We assume if this project (compilation) knows T1 but don't know T2,
						// then it cannot be T1 <= T2
						result = false;
					}
					else
					{
						result = TypeHelper.InheritsByName(roslynType1, roslynType2);
					}
				}
			}

			return result;
        }

		public async Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
		{
			//var roslynMethod = RoslynSymbolFactory.FindMethodInCompilation(methodDescriptor, this.Compilation);
			var methodParserInfo = await this.FindMethodInProjectAsync(methodDescriptor);

			//if (roslynMethod != null)
			if (methodParserInfo != null)
			{
				var roslynMethod = methodParserInfo.MethodSymbol;
				var roslynType = RoslynSymbolFactory.GetTypeByName(typeDescriptor, this.Compilation);
				var implementedMethod = Utils.FindMethodImplementation(roslynMethod, roslynType);
				//Contract.Assert(implementedMethod != null);

				if (implementedMethod != null)
				{
					methodDescriptor = Utils.CreateMethodDescriptor(implementedMethod);
				}
			}
			else
			{
				// If it is interface/abstract or code that we did not parse  (library)
				var roslynMethod = RoslynSymbolFactory.FindMethodInCompilation(methodDescriptor, this.Compilation);

				if (roslynMethod != null)
				{
					var roslynType = RoslynSymbolFactory.GetTypeByName(typeDescriptor, this.Compilation);
					var implementedMethod = Utils.FindMethodImplementation(roslynMethod, roslynType);

					if (implementedMethod != null)
					{
						methodDescriptor = Utils.CreateMethodDescriptor(implementedMethod);
					}
				}
			}

			// TODO: If we cannot resolve the method, we return the same method.
			// Maybe we should consider to return null instead?
			return methodDescriptor;
		}

        public Task<MethodDescriptor> FindMethodImplementationUsingRoslynAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
        {
			var roslynMethod = RoslynSymbolFactory.FindMethodInCompilation(methodDescriptor, this.Compilation);

			if (roslynMethod != null)
			{
				var roslynType = RoslynSymbolFactory.GetTypeByName(typeDescriptor, this.Compilation);
				var implementedMethod = Utils.FindMethodImplementation(roslynMethod, roslynType);
				//Contract.Assert(implementedMethod != null);

				if (implementedMethod != null)
				{
					methodDescriptor = Utils.CreateMethodDescriptor(implementedMethod);
				}
				else
				{
					Logger.LogS("BaseProjectCodeProvider", "FindMethodImplementationAsync", "Cannot find implementation for method {0}", methodDescriptor);
				}
			}

			// If we cannot resolve the method, we return the same method.
			return Task.FromResult(methodDescriptor);
		}

		public async Task<IEnumerable<TypeDescriptor>> GetCompatibleInstantiatedTypesAsync(TypeDescriptor type)
		{
			var result = new HashSet<TypeDescriptor>();
			var instantiatedTypes = await this.rtaManager.GetInstantiatedTypesAsync();

			//if (type.QualifiedTypeName == "System.Threading.Tasks.Task<AnalysisEngine>")
			//{
			//	var line = new StringBuilder();

			//	foreach (var potentialType in instantiatedTypes)
			//	{
			//		line.AppendLine(potentialType.QualifiedTypeName);
			//	}

			//	line.AppendLine();

			//	File.AppendAllText(@"C:\Users\Edgar\Downloads\log.txt", line.ToString());
			//}
			
			foreach (var potentialType in instantiatedTypes)
			{
				var isSubtype = await this.IsSubtypeAsync(potentialType, type);

				if (isSubtype)
				{
					result.Add(potentialType);
				}
			}

			return result;
		}

		public Task<IEnumerable<MethodDescriptor>> GetRootsAsync(AnalysisRootKind rootKind = AnalysisRootKind.Default)
		{
			return Utils.GetRootsAsync(this.Compilation, rootKind);
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

		// This version get the info from the PropagationGraphs in the method entities
		// The problem is when you update a file the relative positions of the methods in the file changes
		// and we need to update all the ranges in all the entities
		public async Task<IEnumerable<CodeGraphModel.FileResponse>> GetDocumentEntitiesAsync(string documentPath)
		{
			var documentInfo = await this.GetDocumentInfoAsync(documentPath);
			var result = await CodeGraphHelper.GetDocumentEntitiesAsync(this, documentInfo);
			return result;
		}

		public async Task<CodeGraphModel.SymbolReference> GetDeclarationInfoAsync(MethodDescriptor methodDescriptor)
		{
			CodeGraphModel.SymbolReference result = null;
			var methodInfo = await this.FindMethodInProjectAsync(methodDescriptor);

			if (methodInfo != null)
			{
				result = CodeGraphHelper.GetMethodReferenceInfo(methodInfo.DeclarationSyntaxNode);
			}

			return result;
		}

		public async Task<CodeGraphModel.SymbolReference> GetInvocationInfoAsync(CallContext callContext)
		{
			CodeGraphModel.SymbolReference result = null;
			var methodInfo = await this.FindMethodInProjectAsync(callContext.Caller);

			if (methodInfo != null)
			{
				result = CodeGraphHelper.GetMethodReferenceInfo(callContext.CallNode, methodInfo.DeclarationSyntaxNode);
			}

			return result;
		}

		public virtual async Task<PropagationEffects> RemoveMethodAsync(MethodDescriptor methodDescriptor)
		{
			var methodEntity = await this.GetMethodEntityAsync(methodDescriptor);
			var propagationEffects = await methodEntity.RemoveMethodAsync();

			this.RemoveMethodFromDocumentInfo(methodDescriptor);
            return propagationEffects;
		}

		public async Task<PropagationEffects> AddMethodAsync(MethodDescriptor methodToAdd)
		{
			var methodParserInfo = await this.FindMethodInProjectAsync(methodToAdd);
			var roslynMethod = methodParserInfo.MethodSymbol;

			var propagationForCallers = new HashSet<ReturnInfo>();
			if (roslynMethod.IsOverride)
			{
				var overridenMethodDescriptor = Utils.CreateMethodDescriptor(roslynMethod.OverriddenMethod);
				var methodEntityWP = await this.GetMethodEntityAsync(overridenMethodDescriptor);
				var callersWithContext = await methodEntityWP.GetCallersAsync();
				foreach(var callerContext in callersWithContext)
				{
					var returnInfo = new ReturnInfo(overridenMethodDescriptor,callerContext);
					propagationForCallers.Add(returnInfo);
				}
				
			}
			return new PropagationEffects(propagationForCallers);
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
				var methodFound = documentInfo.DeclaredMethods.Remove(methodDescriptor);
				if (methodFound) break;
			}
		}

		public virtual async Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			var cancellationTokenSource = new CancellationTokenSource();
			var result = new List<MethodModification>();
			var documentDiff = new DocumentDiff();

			this.newProject = await Utils.ReadProjectAsync(projectPath);

			var oldProjectDocuments = from doc in project.Documents
									  select doc.FilePath;

			var newProjectDocuments = from doc in newProject.Documents
									  select doc.FilePath;

			var allProjectDocuments = oldProjectDocuments.Union(newProjectDocuments, StringComparer.InvariantCultureIgnoreCase);
			var modifiedProjectDocuments = modifiedDocuments.Intersect(allProjectDocuments, StringComparer.InvariantCultureIgnoreCase);

			if (modifiedProjectDocuments.Any())
			{
				this.newCompilation = await Utils.CompileProjectAsync(newProject, cancellationTokenSource.Token);
				this.newDocumentsInfo = new Dictionary<DocumentPath, DocumentInfo>(this.DocumentsInfo);

				foreach (var documentPath in modifiedProjectDocuments)
				{
					var oldDocumentInfo = await this.GetDocumentInfoAsync(documentPath);
					this.useNewFieldsVersion = true;

					this.RemoveDocumentInfo(documentPath);
					var newDocumentInfo = await this.GetDocumentInfoAsync(documentPath);
					this.useNewFieldsVersion = false;

					var modifications = documentDiff.GetDifferences(oldDocumentInfo, newDocumentInfo);
					result.AddRange(modifications);
				}
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
