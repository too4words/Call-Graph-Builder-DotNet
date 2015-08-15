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


		public virtual async Task RemoveMethodAsync(MethodDescriptor methodToRemove)
		{
			var metdhodEntityWP = await this.GetMethodEntityAsync(methodToRemove);
			
		}

		public async Task PropagateMethodRemoval()
		{
			//var propGraphOld = this.methodEntity.PropGraph;

			//foreach (var potentialCallee in this.GetCalleesAsync())
			//{
			//	RemoveCall(potentialCallee);
			//}

			// This is to force the callers to call me
			//foreach(var callerConext in  entity.Callers)
			//{
			//    var caller = callerConext.Caller;
			//    var callerEntityProcessor = Dispatcher.GetEntityWithProcessor(new MethodEntityDescriptor<AMethod>(caller));
			//    callerEntityProcessor.DoAnalysis(); 
			//}
			// Here we propagate the removal of the retvalue of the method we eliminate
			//if (oldMethodEntity.ReturnVariable != null)
			//{
			//	var returnTypes = oldMethodEntity.GetTypes(oldMethodEntity.ReturnVariable);
			//	foreach (var callersContext in oldMethodEntity.Callers)
			//	{
			//		RemoveReturnValuesFromCallerLHS(returnTypes, callersContext.Caller, callersContext.CallLHS);
			//	}
			//}

			//this.Dispatcher.RegisterEntity(entityDescriptor, newEntity);
			//// I get an entity processor to analyze the new entity
			//var newEntityProcessor = Dispatcher.GetEntityWithProcessor(entityDescriptor) as MethodEntityProcessor<ANode, AType, AMethod>;
			///// I need to copy all the input data from the old method 
			//newEntity.CopyInterfaceDataAndCallers(oldMethodEntity);
			//newEntityProcessor.DoAnalysis();

			//var propGraphNew = newEntity.PropGraph;
			//var invoNew = GetInvocations(propGraphNew);
			////newEntityProcessor.MethodEntity.Save(oldRoslynMethod.ContainingType.Name + "_" + oldRoslynMethod.Name + "_d.dot");
			//oldMethodEntity.InvalidateCaches();

		}
		private void RemoveCall(MethodDescriptor callee)
		{
			//	this.codeProvider.GetMethodEntityAsync()
			//	var entityProcessorforCallee = Dispatcher.GetEntityWithProcessor(EntityFactory<AMethod>.Create((AMethod)aCallee)) as MethodEntityProcessor<ANode, AType, AMethod>;
			//	var calleeEntity = entityProcessorforCallee.MethodEntity;
			//	calleeEntity.InvalidateCaches();
			//	// Delete progragation of arguments and receiver
			//	var statementProcessor = new StatementProcessor<ANode, AType, AMethod>((AMethod)aCallee,
			//			calleeEntity.ReturnVariable, calleeEntity.ThisRef, calleeEntity.ParameterNodes,
			//			calleeEntity.PropGraph);
			//	foreach (var p in calleeEntity.ParameterNodes)
			//	{
			//		statementProcessor.RegisterRemoveNewExpressionAssignment(p);
			//	}
			//	if (calleeEntity.ThisRef != null)
			//		statementProcessor.RegisterRemoveNewExpressionAssignment(calleeEntity.ThisRef);
			//	// entity.RemoveCallees();
			//	entityProcessorforCallee.DoDelete();
			//	var context = new CallConext<AMethod, ANode>(invocation.Caller, invocation.LHS, invocation.CallNode);
			//	calleeEntity.RemoveFromCallers(context);
		}

	}
}
