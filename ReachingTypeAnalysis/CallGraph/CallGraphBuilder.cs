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
        public static CallGraph<MethodDescriptor, LocationDescriptor> BuildCallGraph(Solution solution)
        {
            var analyzer = new SolutionAnalyzer(solution);
            analyzer.Analyze();
            //analyzer.GenerateCallGraph();
            var callgraph = analyzer.GenerateCallGraph();
            Console.WriteLine("Reachable methods={0}", callgraph.GetReachableMethods().Count);

            return callgraph;
        }

        public static CallGraph<MethodDescriptor, LocationDescriptor> BuildCallGraphWithoutAppSettings(Solution solution)
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

        /// <summary>
        /// This is used by the VS Studio Extension. 
        /// NEEDS TO BE REEMPLEMENTED because nodes no longer contains AST Expression 
        /// I used the AST expression of the invocation to recover the (relative) position 
        /// of an invocation (the use of locations is not good because a change in the file
        /// affect the position of all the statements) 
        /// </summary>
        /// <param name="method"></param>
        /// <param name="order"></param>
        /// <returns></returns>
		//internal async Task<ISet<MethodDescriptor>> GetCallees(IMethodSymbol method, int order)
        internal ISet<MethodDescriptor> GetCallees(IMethodSymbol method, int order)
        {
            var result = new HashSet<MethodDescriptor>();
            var methodDescriptor = Utils.CreateMethodDescriptor(method);
            var methodEntityProcessor = (MethodEntityProcessor)
                 this.solutionAnalyzer.Dispatcher.GetEntityWithProcessor
                (EntityFactory.Create(methodDescriptor, this.solutionAnalyzer.Dispatcher));
            var methodEntity = methodEntityProcessor.MethodEntity;

            if (!methodEntity.HasBeenPropagated)
                methodEntityProcessor.DoAnalysis();

            var callSitesForMethod = methodEntityProcessor.GetCalleesInfo();

			// var roslynMethodwithNewId = methodEntity.Method.RoslynMethod;
            var roslynMethodwithNewId = RoslynSymbolFactory.FindMethodSymbolInSolution(this.solutionAnalyzer.Solution, methodDescriptor);

			foreach (var callSiteNode in callSitesForMethod.Keys)
			{
                //var loc = ((AnalysisCallNode)callSiteNode).LocationDescriptor;
                //var invocation = ((AnalysisCallNode)callSiteNode)..Expression;
                //int invOrder = GetInvocationNumber(roslynMethodwithNewId, invocation);
                int invOrder = ((AnalysisCallNode)callSiteNode).InMethodOrder;
				if (invOrder == order)
				{
					foreach (var calleemethodDescriptor in callSitesForMethod[callSiteNode])
					{
						var callee = calleemethodDescriptor;
						result.Add(callee);
					}
				}
			}
            return result;
        }

		// Original version using locations
		internal ISet<MethodDescriptor> GetCallees(IMethodSymbol method, LocationDescriptor locToFilter)
        {
            var result = new HashSet<MethodDescriptor>();
            var methodDescriptor = Utils.CreateMethodDescriptor(method);
			//var methodEntity = this.solutionAnalyzer.Dispatcher.GetEntity(new MethodEntityDescriptor<methodDescriptor>(methodDescriptor)) as MethodEntity<ANode,AType,methodDescriptor>;
			var methodEntityProcessor = (MethodEntityProcessor)
                this.solutionAnalyzer.Dispatcher.GetEntityWithProcessorAsync(EntityFactory.Create(methodDescriptor, 
                                                                                this.solutionAnalyzer.Dispatcher)).Result;
            var methodEntity = methodEntityProcessor.MethodEntity;

            if (!methodEntity.HasBeenPropagated)
                methodEntityProcessor.DoAnalysis();
            
            var callSitesForMethod = methodEntityProcessor.GetCalleesInfo();
            // var locToFilter = new ALocation(l);
            foreach(var callSiteNode in callSitesForMethod.Keys)
            {
                var loc = ((AnalysisCallNode)callSiteNode).LocationDescriptor;
                if (loc.Equals(locToFilter))
                {
                    foreach (var calleemethodDescriptor in callSitesForMethod[callSiteNode])
                    {
                        var callee = calleemethodDescriptor;
                        result.Add(callee);
                    }
                }
            }
            return result;
        }		

//		public async Task<ISet<KeyValuePair<int, MethodDescriptor>>> GetCallersWithOrder(IMethodSymbol m)
        public ISet<KeyValuePair<int, MethodDescriptor>> GetCallersWithOrder(IMethodSymbol m)
        {
			var result = new HashSet<KeyValuePair<int, MethodDescriptor>>();
			var methodDescriptor = Utils.CreateMethodDescriptor(m);

            var methodEntityProcessor = (MethodEntityProcessor)
				this.solutionAnalyzer.
				Dispatcher.GetEntityWithProcessor(
                    EntityFactory.Create(methodDescriptor, this.solutionAnalyzer.Dispatcher));
			var methodEntity = methodEntityProcessor.MethodEntity;
			if (!methodEntity.HasBeenPropagated)
			{
				//await methodEntityProcessor.DoAnalysisAsync();
                methodEntityProcessor.DoAnalysis();
			}

			foreach (var callContex in methodEntity.Callers)
			{
				//int order = GetInvocationNumber(callContex.Caller.RoslynMethod, callContex.Invocation.Expression);
                int order = callContex.Invocation.InMethodOrder;
                result.Add(new KeyValuePair<int, MethodDescriptor>(order, callContex.Caller));
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
