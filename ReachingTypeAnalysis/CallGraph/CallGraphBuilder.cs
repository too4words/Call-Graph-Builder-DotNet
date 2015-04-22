// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReachingTypeAnalysis.Analysis;
using ReachingTypeAnalysis.Roslyn;
using SolutionTraversal.Callgraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis
{
	public static class CallGraphBuilder
    {
        /// <summary>
        /// Computes a CG for a solution
        /// It can be used and the inititalization for analysis that needs a CG
        /// </summary>
        /// <param name="solution"></param>
        /// <returns></returns>
        //public static CallGraph<IMethodSymbol,Location> BuildCallGraph(Solution solution)
        public static CallGraph<MethodDescriptor, AnalysisLocation> BuildCallGraph(Solution solution)
        {
            var analyzer = new SolutionAnalyzer(solution);
            analyzer.Analyze();
            //analyzer.GenerateCallGraph();
            var callgraph = analyzer.GenerateCallGraph();
            Console.WriteLine("Reachable methods={0}", callgraph.GetReachableMethods().Count);

            return callgraph;
        }

        public static CallGraph<MethodDescriptor, AnalysisLocation> BuildCallGraphWithoutAppSettings(Solution solution)
        {
            var analyzer = new SolutionAnalyzer(solution);
            analyzer.AnalyzeWithoutAppSettings();
            //analyzer.GenerateCallGraph();
            var callgraph = analyzer.GenerateCallGraph();
            Console.WriteLine("Reachable methods={0}", callgraph.GetReachableMethods().Count);

            return callgraph;
        }
    }

    internal class MethodEntitiesCallsInfo
    {
        private SolutionAnalyzer solutionAnalyzer;
		internal MethodEntitiesCallsInfo(Solution solution)
        {
            this.solutionAnalyzer= new SolutionAnalyzer(solution);
            this.solutionAnalyzer.AnalyzeWithoutAppSettings();
        }

		internal async Task<ISet<MethodDescriptor>> GetCallees(IMethodSymbol m, int order)
        {
            var result = new HashSet<MethodDescriptor>();
            var aMethod = new AnalysisMethod(m);
            var methodDescriptor = aMethod.MethodDescriptor;
            //var methodEntity = this.solutionAnalyzer.Dispatcher.GetEntity(new MethodEntityDescriptor<AMethod>(aMethod)) as MethodEntity<ANode,AType,AMethod>;
            var methodEntityProcessor = (MethodEntityProcessor)
                await this.solutionAnalyzer.Dispatcher.GetEntityWithProcessorAsync
                (EntityFactory.Create(aMethod));
            var methodEntity = methodEntityProcessor.MethodEntity;

            if (!methodEntity.HasBeenPropagated)
                methodEntityProcessor.DoAnalysis();

            var callSitesForMethod = methodEntity.GetCalleesInfo();

			var roslynMethodwithNewId = methodEntity.Method.RoslynMethod;
			// var locToFilter = new ALocation(l);
			foreach (var callSiteNode in callSitesForMethod.Keys)
			{
				var loc = ((AnalysisNode)callSiteNode).LocationReference;
				var invocation = ((AnalysisNode)callSiteNode).Expression;
				int invOrder = GetInvocationNumber(roslynMethodwithNewId, invocation);
				if (invOrder == order)
				{
					foreach (var calleeAMethod in callSitesForMethod[callSiteNode])
					{
						var callee = calleeAMethod.MethodDescriptor;
						result.Add(callee);
					}
				}
			}
            return result;
        }

        private int GetInvocationNumber(IMethodSymbol roslynMethod, SyntaxNodeOrToken invocation)
        {
            // var roslynMethod = RoslynSymbolFactory.FindMethodSymbolInSolution(this.solution, locMethod.Value);
            var methodDeclarationSyntax = roslynMethod.DeclaringSyntaxReferences.First();
            //var syntaxTree = methodDeclarationSyntax.SyntaxTree;
            var invocations = methodDeclarationSyntax.GetSyntax().DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().ToArray();
            int count = 0;
			for (int i = 0; i < invocations.Length && !invocations[i].GetLocation().Equals(invocation.GetLocation()); i++)
			{
				count++;
			}

			return count;
        }

		// Original version using locations
		internal ISet<MethodDescriptor> GetCallees(IMethodSymbol m, AnalysisLocation locToFilter)
        {
            var result = new HashSet<MethodDescriptor>();
            var aMethod = new AnalysisMethod(m);
            var methodDescriptor = aMethod.MethodDescriptor;
			//var methodEntity = this.solutionAnalyzer.Dispatcher.GetEntity(new MethodEntityDescriptor<AMethod>(aMethod)) as MethodEntity<ANode,AType,AMethod>;
			var methodEntityProcessor = (MethodEntityProcessor)
				this.solutionAnalyzer.Dispatcher.GetEntityWithProcessorAsync(EntityFactory.Create(aMethod)).Result;
            var methodEntity = methodEntityProcessor.MethodEntity;

            if (!methodEntity.HasBeenPropagated)
                methodEntityProcessor.DoAnalysis();
            
            var callSitesForMethod = methodEntity.GetCalleesInfo();
            // var locToFilter = new ALocation(l);
            foreach(var callSiteNode in callSitesForMethod.Keys)
            {
                var loc = ((AnalysisNode)callSiteNode).LocationReference;
                if (loc.Equals(locToFilter))
                {
                    foreach (var calleeAMethod in callSitesForMethod[callSiteNode])
                    {
                        var callee = calleeAMethod.MethodDescriptor;
                        result.Add(callee);
                    }
                }
            }
            return result;
        }		

		public async Task<ISet<KeyValuePair<int, MethodDescriptor>>> GetCallersWithOrder(IMethodSymbol m)
		{
			var result = new HashSet<KeyValuePair<int, MethodDescriptor>>();
			var aMethod = new AnalysisMethod(m);
			var methodDescriptor = aMethod.MethodDescriptor;
			var methodEntityProcessor = (MethodEntityProcessor)
				await this.solutionAnalyzer.
				Dispatcher.GetEntityWithProcessorAsync(
					EntityFactory.Create(aMethod));
			var methodEntity = methodEntityProcessor.MethodEntity;
			if (!methodEntity.HasBeenPropagated)
			{
				await methodEntityProcessor.DoAnalysisAsync();
			}

			foreach (var callContex in methodEntity.Callers)
			{
				int order = GetInvocationNumber(callContex.Caller.RoslynMethod, callContex.Invocation.Expression);
				result.Add(new KeyValuePair<int, MethodDescriptor>(order, callContex.Caller.MethodDescriptor));
			}
			return result;
		}

        //public void UpdateMethod(MethodDescriptor oldMethodDescriptor, IMethodSymbol newMethod, SemanticModel newSemanticModel)
        //{
        //    var oldMethod = RoslynSymbolFactory.FindMethodSymbolInSolution(this.solutionAnalyzer.Compilation, oldMethodDescriptor);
        //    this.solutionAnalyzer.PerformUpdate(oldMethod, newSemanticModel, newMethod);
        //}
    }
}
