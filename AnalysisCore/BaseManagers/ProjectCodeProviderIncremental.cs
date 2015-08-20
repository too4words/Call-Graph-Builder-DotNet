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
using ReachingTypeAnalysis;

namespace ReachingTypeAnalysis.Analysis
{
	public abstract partial class BaseProjectCodeProvider: IProjectCodeProvider
	{
		//internal async Task UpdateMethodAsync(MethodDescriptor methodDescriptor, SyntaxTree newSyntax)
		//{
		//	var methodInfo = await this.FindMethodDeclarationAsync(methodDescriptor);
		//	var oldTree = methodInfo.SyntaxTree;
		//	var oldCompilation = this.compilation;
		//	var newCompilation = oldCompilation.ReplaceSyntaxTree(oldTree, newSyntax);
		//	var newSemanticModel = newCompilation.GetSemanticModel(newSyntax);

		//	var newMethodDeclaration = await GetMethodDeclarationInTree(methodDescriptor, newSyntax, newSemanticModel);


		//	// Find the method and project of the method to be updated
		//	//var projectMethdod = FindMethodSymbolAndProjectInSolution(methodDescriptor, callgraph);
		//	//var oldRoslynMethod = projectMethdod.Method;
		//	//var project = projectMethdod.Project;

		//	//var aMethod = new AMethod(oldRoslynMethod);
		//	////entityProcessor.MethodEntity.Save(roslynMethod.ContainingType.Name + "_" + roslynMethod.Name + "_orig.dot");
		//	////--------------------------------------------------------------------------------------------------------
		//	//// This is to mimic a change in the method. We need to create a new comp
		//	//var methodDecSyntax = Utils.FindMethodDeclaration(aMethod);
		//	//var newMethodBody = SyntaxFactory.ParseStatement(newCode) as BlockSyntax;
		//	//// here we update the method body
		//	//var newMethodSyntax = methodDecSyntax.WithBody(newMethodBody);
		//	//// This is a trick to recover the part of the syntax tree after replacing the project syntax tree
		//	//var annotation = new SyntaxAnnotation("Hi");
		//	//newMethodSyntax = newMethodSyntax.WithAdditionalAnnotations(annotation);
		//	//// update the syntax tree
		//	//var oldRoot = methodDecSyntax.SyntaxTree.GetRoot();
		//	//var newRoot = oldRoot.ReplaceNode(methodDecSyntax, newMethodSyntax);
		//	//// Compute the new compilation and semantic model
		//	//var oldCompilation = project.GetCompilationAsync().Result;
		//	//var newCompilation = oldCompilation.ReplaceSyntaxTree(oldRoot.SyntaxTree, newRoot.SyntaxTree);
		//	//var newSemanticModel = newCompilation.GetSemanticModel(newRoot.SyntaxTree);
		//	//// Recover the method node
		//	//var recoveredMethodNode = newRoot.GetAnnotatedNodes(annotation).Single();
		//	////////////////////////////////////////////////////////

		//	//// Get the entity corresponding to the new (updated) method
		//	//var updatedRoslynMethod = newSemanticModel.GetDeclaredSymbol(recoveredMethodNode) as IMethodSymbol;

		//	//PerformUpdate(oldRoslynMethod, newSemanticModel, updatedRoslynMethod);
		//}

		//private static async Task<MethodParserInfo> GetMethodDeclarationInTree(MethodDescriptor methodDescriptor, SyntaxTree newSyntax, SemanticModel newSemanticModel)
		//{		
		//	var root = await newSyntax.GetRootAsync();
		//	var visitor = new MethodFinder(methodDescriptor, newSemanticModel);

		//	visitor.Visit(root);
		//	if (visitor.Result != null)
		//	{
		//		var declarationNode = visitor.Result;
		//		var symbol = newSemanticModel.GetDeclaredSymbol(declarationNode) as IMethodSymbol;
		//		return new MethodParserInfo(methodDescriptor)
		//		{
		//			SyntaxTree = newSyntax,
		//			SemanticModel = newSemanticModel,
		//			DeclarationNode = declarationNode,
		//			MethodSymbol = symbol
		//		};
		//	}
		//	return null;
		//}

		//internal async Task PerformUpdateAsync(MethodInfo oldMethodInfo, MethodInfo newMethodInfo)
		//{
		//	var newMethodParser = new MethodParser(newMethodInfo);
		//	var newMethodEntity = newMethodParser.ParseMethod();
		//	var oldMethodEntityWP = await this.GetMethodEntityAsync(newMethodInfo.MethodDescriptor);
		//}
/*
		private void RemoveCall(AMethod aCallee, AInvocationExp<AMethod, AType, ANode> invocation)
		{
			var entityProcessorforCallee = Dispatcher.GetEntityWithProcessor(EntityFactory<AMethod>.Create((AMethod)aCallee)) as MethodEntityProcessor<ANode, AType, AMethod>;
			var calleeEntity = entityProcessorforCallee.MethodEntity;
			calleeEntity.InvalidateCaches();
			// Delete progragation of arguments and receiver
			var statementProcessor = new StatementProcessor<ANode, AType, AMethod>((AMethod)aCallee,
					calleeEntity.ReturnVariable, calleeEntity.ThisRef, calleeEntity.ParameterNodes,
					calleeEntity.PropGraph);
			foreach (var p in calleeEntity.ParameterNodes)
			{
				statementProcessor.RegisterRemoveNewExpressionAssignment(p);
			}
			if (calleeEntity.ThisRef != null)
				statementProcessor.RegisterRemoveNewExpressionAssignment(calleeEntity.ThisRef);
			// entity.RemoveCallees();
			entityProcessorforCallee.DoDelete();
			var context = new CallConext<AMethod, ANode>(invocation.Caller, invocation.LHS, invocation.CallNode);
			calleeEntity.RemoveFromCallers(context);
		}
*/

		/*
				#region Just a test to discard about programation of a deletion o a croncrete type
				internal void RemoveTypesFromNode(MethodDescriptor methodDescriptor, string text)
				{
					var m = RoslynSymbolFactory.FindMethodSymbolInSolution(this.Solution, methodDescriptor);
					var am = new AMethod(m);
					var entityProcessor = (MethodEntityProcessor<ANode, AType, AMethod>)
						this.Dispatcher.GetEntityWithProcessor(EntityFactory<AMethod>.Create(am));
					var entity = entityProcessor.MethodEntity;
					var pg = entity.PropGraph;
					ANode n = pg.FindNodeInPropationGraph(text);
					var statementProcessor = new StatementProcessor<ANode, AType, AMethod>(am, entity.ReturnVariable, entity.ThisRef, entity.ParameterNodes, pg);
            
					statementProcessor.RegisterRemoveNewExpressionAssignment(n);
					// entity.RemoveCallees();
					entityProcessor.DoDelete();

					//entity.InvalidateCaches();
				}

				internal void RemoveAsignment(MethodDescriptor methodDescriptor, string p1, string p2)
				{
					var roslynMethod = RoslynSymbolFactory.FindMethodSymbolInSolution(this.Solution, methodDescriptor);
					var aMethod = new AMethod(roslynMethod);
					var entityProcessor = this.Dispatcher.GetEntityWithProcessor(
						EntityFactory<AMethod>.Create(aMethod)) as MethodEntityProcessor<ANode, AType, AMethod>;
					var entity = entityProcessor.MethodEntity;
            
					var syntaxNode = Utils.FindMethodDeclaration(aMethod);

					var nodes = syntaxNode.DescendantNodes().OfType<AssignmentExpressionSyntax>();
					var node = nodes.First(a => a.Left.ToString() == p1 && a.Right.ToString() == p2);

					ANode lhs = entity.PropGraph.FindNodeInPropationGraph(p1);
					ANode rhs = entity.PropGraph.FindNodeInPropationGraph(p2);

					var statementProcessor = new StatementProcessor<ANode, AType, AMethod>(aMethod, 
							entity.ReturnVariable, entity.ThisRef, entity.ParameterNodes, entity.PropGraph);

					statementProcessor.RegisterRemoveAssignment(lhs, rhs);
					entityProcessor.DoDelete();

					//entity.InvalidateCaches();
				}

				/// <summary>
				/// A test of how we should proceed when a method is modified
				/// </summary>
				/// <param name="methodDescriptor"></param>
				/// <param name="newCode"></param>
				internal void UpdateMethod(MethodDescriptor methodDescriptor, string newCode, CallGraph<MethodDescriptor, ALocation> callgraph)
				{
					// Find the method and project of the method to be updated
					var projectMethdod = FindMethodSymbolAndProjectInSolution(methodDescriptor, callgraph);
					var oldRoslynMethod = projectMethdod.Method;
					var project = projectMethdod.Project;
            
					var aMethod = new AMethod(oldRoslynMethod);
					//entityProcessor.MethodEntity.Save(roslynMethod.ContainingType.Name + "_" + roslynMethod.Name + "_orig.dot");
					//--------------------------------------------------------------------------------------------------------
					// This is to mimic a change in the method. We need to create a new comp
					var methodDecSyntax = Utils.FindMethodDeclaration(aMethod);   
					var newMethodBody = SyntaxFactory.ParseStatement(newCode) as BlockSyntax;            
					// here we update the method body
					var newMethodSyntax = methodDecSyntax.WithBody(newMethodBody);
					// This is a trick to recover the part of the syntax tree after replacing the project syntax tree
					var annotation = new SyntaxAnnotation("Hi");
					newMethodSyntax = newMethodSyntax.WithAdditionalAnnotations(annotation);
					// update the syntax tree
					var oldRoot = methodDecSyntax.SyntaxTree.GetRoot();
					var newRoot = oldRoot.ReplaceNode(methodDecSyntax, newMethodSyntax);
					// Compute the new compilation and semantic model
					var oldCompilation = project.GetCompilationAsync().Result;
					var newCompilation = oldCompilation.ReplaceSyntaxTree(oldRoot.SyntaxTree, newRoot.SyntaxTree);
					var newSemanticModel = newCompilation.GetSemanticModel(newRoot.SyntaxTree);
					// Recover the method node
					var recoveredMethodNode = newRoot.GetAnnotatedNodes(annotation).Single();
					//////////////////////////////////////////////////////

					// Get the entity corresponding to the new (updated) method
					var updatedRoslynMethod = newSemanticModel.GetDeclaredSymbol(recoveredMethodNode) as IMethodSymbol;

					PerformUpdate(oldRoslynMethod, newSemanticModel, updatedRoslynMethod);
				}

				

				private void RemoveReturnValuesFromCallerLHS(ISet<AType> returnTypes, AMethod aCaller, ANode lhs)
				{
					var entityProcessorforCaller = (MethodEntityProcessor<ANode, AType, AMethod>)
						Dispatcher.GetEntityWithProcessor(EntityFactory<AMethod>.Create((AMethod)aCaller));
					var callerEntity = entityProcessorforCaller.MethodEntity;

					var statementProcessor = new StatementProcessor<ANode, AType, AMethod>((AMethod)aCaller,
							callerEntity.ReturnVariable, callerEntity.ThisRef, callerEntity.ParameterNodes,
							callerEntity.PropGraph);
					//callerEntity.PropGraph.
					statementProcessor.RegisterRemoveTypes(lhs, returnTypes);
					//callerEntity.InvalidateCaches();
					entityProcessorforCaller.DoDelete();
				}

				private void RemoveCall(AMethod aCallee, AInvocationExp<AMethod,AType,ANode> invocation)
				{
					var entityProcessorforCallee = Dispatcher.GetEntityWithProcessor(EntityFactory<AMethod>.Create((AMethod)aCallee)) as MethodEntityProcessor<ANode, AType, AMethod>;
					var calleeEntity = entityProcessorforCallee.MethodEntity;
					calleeEntity.InvalidateCaches();
					// Delete progragation of arguments and receiver
					var statementProcessor = new StatementProcessor<ANode, AType, AMethod>((AMethod)aCallee,
							calleeEntity.ReturnVariable, calleeEntity.ThisRef, calleeEntity.ParameterNodes,
							calleeEntity.PropGraph);
					foreach (var p in calleeEntity.ParameterNodes)
					{
						statementProcessor.RegisterRemoveNewExpressionAssignment(p);
					}
					if (calleeEntity.ThisRef != null)
						statementProcessor.RegisterRemoveNewExpressionAssignment(calleeEntity.ThisRef);
					// entity.RemoveCallees();
					entityProcessorforCallee.DoDelete();
					var context = new CallConext<AMethod, ANode>(invocation.Caller,invocation.LHS,invocation.CallNode);
					calleeEntity.RemoveFromCallers(context);
				}

				private static List<AInvocationExp<AMethod, AType, ANode>> GetInvocations(PropagationGraph<ANode, AType, AMethod> propGraphOld)
				{
					var invoList = new List<AInvocationExp<AMethod, AType, ANode>>();
					foreach (var oldCall in propGraphOld.CallNodes)
					{
						var oldCallInfo = propGraphOld.GetInvocationInfo(oldCall);
						invoList.Add(oldCallInfo);
					}
					return invoList;
				}
				#endregion
		*/

	}
}
