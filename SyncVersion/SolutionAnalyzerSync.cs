// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using ReachingTypeAnalysis.Analysis;
using ReachingTypeAnalysis.Communication;
using ReachingTypeAnalysis.Roslyn;
using SolutionTraversal.CallGraph;
using System.Configuration;

namespace ReachingTypeAnalysis
{
    public enum AnalysisStrategyKind
    {
        ONDEMAND_SYNC,
        ENTIRE_SYNC,
        NONE,
    }

	public class SolutionAnalyzerSync
	{
		private string source;
		private string solutionPath;

		private Solution solution;
		private IDispatcher dispatcher;
		private string testName;

		public static int MessageCounter { get; private set; }

		private SolutionAnalyzerSync()
		{
		}

		public static SolutionAnalyzerSync CreateFromSolution(string solutionPath)
        {
			var analyzer = new SolutionAnalyzerSync();
			analyzer.solutionPath = solutionPath;
			return analyzer;
        }

		public static SolutionAnalyzerSync CreateFromSource(string source)
		{
			var analyzer = new SolutionAnalyzerSync();
			analyzer.source = source;
			return analyzer;
		}

		public static SolutionAnalyzerSync CreateFromTest(string testName)
		{
			var analyzer = new SolutionAnalyzerSync();
			analyzer.testName= testName;
			return analyzer;
		}

		/// <summary>
		/// IMPORTANT: OnDemandSolvers need an OnDemand Dispatcher
		/// We cannot use the SyncronousDistacther because it doesn't look for the 
		/// methods when they are not available. This only works with the entire solution
		/// analysis!!!!
		/// </summary>
		/// <param name="dispatcher"></param>
		public CallGraph<MethodDescriptor, LocationDescriptor> Analyze(AnalysisStrategyKind strategyKind = AnalysisStrategyKind.NONE)
        {
            if (strategyKind == AnalysisStrategyKind.NONE)
            {
                strategyKind = StringToAnalysisStrategy(ConfigurationManager.AppSettings["Strategy"]);
            }

            switch (strategyKind)
            {
                case AnalysisStrategyKind.ONDEMAND_SYNC:
                    {
						this.solution = this.GetSolution();
						this.dispatcher = new OnDemandSyncDispatcher();
                        this.AnalyzeOnDemandSync();
                        return this.GenerateCallGraph();
                    }
				//case AnalysisStrategyKind.ENTIRE_SYNC:
				//	{
				//		this.solution = this.GetSolution();
				//		this.dispatcher = new SynchronousLocalDispatcher();
				//		this.AnalyzeEntireSolution();
				//		return this.GenerateCallGraph();
				//	}
                default:
                    {
                        throw new ArgumentException("Unknown value for Solver " + ConfigurationManager.AppSettings["Solver"]);
                    }
            }
        }

		private Solution GetSolution()
		{
			Solution solution = null;

			if (this.source != null)
			{
				solution = Utils.CreateSolution(this.source);
			}
			else
			{
				solution = Utils.ReadSolution(this.solutionPath);
			}

			return solution;
        }


        public static AnalysisStrategyKind StringToAnalysisStrategy(string strategy)
		{
			switch (strategy)
			{
				case "OnDemandSync": return AnalysisStrategyKind.ONDEMAND_SYNC;
				case "EntireSync":  return AnalysisStrategyKind.ENTIRE_SYNC;
				default:  throw new ArgumentException("Unknown value for the strategy: " + strategy);
			}
		}

		//private void AnalyzeEntireSolution()
		//{
		//	foreach (var project in this.solution.Projects)
		//	{
		//		var compilation = project.GetCompilationAsync().Result;
		//		var diag = compilation.GetDiagnostics();
		//		var theAssembly = compilation.Assembly;

		//		foreach (var tree in compilation.SyntaxTrees)
		//		{
		//			var model = compilation.GetSemanticModel(tree);
		//			var allMethodsVisitor = new AllMethodsVisitor(model, tree);
		//			allMethodsVisitor.Visit(tree.GetRoot());
		//		}
		//	}
		//}

        /// <summary>
        /// Try to get the roslyn methods on the fly
        /// Currently works with one project.
        /// </summary>
        private void AnalyzeOnDemandSync()
        {
			// TOOD: hack -- set the global solution
			ProjectCodeProvider.Solution = this.solution;

			var cancellationToken = new CancellationTokenSource();
			var projectIDs = this.solution.GetProjectDependencyGraph().GetTopologicallySortedProjects(cancellationToken.Token);

			foreach (var projectId in projectIDs)
			{
				var project = this.solution.GetProject(projectId);
				var compilation = project.GetCompilationAsync().Result;
                var triple = ProjectCodeProvider.GetProviderContainingEntryPointAsync(project, cancellationToken.Token).Result;
                var provider = triple.Item1;
                var mainSymbol = triple.Item2;
                var tree = triple.Item3;

				if (provider != null)
				{
                    var model = provider.Compilation.GetSemanticModel(tree);
					cancellationToken.Cancel(); // cancel out outstanding processing tasks
                    var methodVisitor = new MethodParser(model, tree, mainSymbol);

					var mainMethodEntity = methodVisitor.ParseMethod();
                    this.dispatcher.RegisterEntity(mainMethodEntity.EntityDescriptor, mainMethodEntity);
                    var mainEntityProcessor = new MethodEntityProcessor(mainMethodEntity, this.dispatcher);
					mainEntityProcessor.DoAnalysis();
					Logger.LogS("SolutionAnalyzer", "AnalyzeOnDemand", "--- Done with propagation ---");
				}
			}

			if (this.dispatcher is QueueingDispatcher)
            {
                var qd = (QueueingDispatcher)this.dispatcher;

                while (!qd.IsDoneProcessing)
                {
                    Logger.LogS("SolutionAnalyzer", "AnalyzeOnDemand", "Waiting for the queue to empty up...");
                    Thread.Sleep(1000);
                }
            }
        }

		/// <summary>
		/// Checks if a method is reachbable
		/// </summary>
		/// <param name="methodDescriptor"></param>
		/// <returns></returns>
		internal bool IsReachable(MethodDescriptor methodDescriptor, CallGraph<MethodDescriptor, LocationDescriptor> callgraph)
        {  
            //var method = FindMethodSymbolInSolution(methodDescriptor).Method;
            //return method != null && Callgraph.GetReachableMethods().Contains(method); // ,new Compare());
            return callgraph.GetReachableMethods().Contains(methodDescriptor); 
        }

        internal ISet<MethodDescriptor> GetReachableMethods(CallGraph<MethodDescriptor, LocationDescriptor> callgraph)
        {
            return callgraph.GetReachableMethods();
        }

		internal bool IsCalled(MethodDescriptor methodDescriptor, MethodDescriptor calleeDescriptor, 
                        CallGraph<MethodDescriptor, LocationDescriptor> callgraph)
        {
            return callgraph.GetCallees(methodDescriptor).Contains(calleeDescriptor);
        }

		internal bool IsCaller(MethodDescriptor callerDescriptor, MethodDescriptor calleeDescriptor, 
                        CallGraph<MethodDescriptor, LocationDescriptor> callgraph)
        {
            return callgraph.GetCallers(calleeDescriptor).Select(kp => kp.Value).Contains(callerDescriptor);
        }


        #region Callgraph

        private static void UpdateCallGraph(IEntityProcessor entityProcessor, CallGraph<MethodDescriptor, LocationDescriptor> callgraph, Solution solution)
        {
            Contract.Assert(entityProcessor != null);
            var methodEntity = (MethodEntity)entityProcessor.Entity;
            Contract.Assert(methodEntity.MethodDescriptor != null);
            var callerMethod = methodEntity.MethodDescriptor;
            var pair = ProjectCodeProvider.GetProjectProviderAndSyntaxAsync(callerMethod, solution).Result;

			if (pair != null)
			{
				var codeProvider = pair.Item1;
				// Hack
				var methodEntityProcessor = new MethodEntityProcessor(methodEntity, ((MethodEntityProcessor)entityProcessor).dispatcher, codeProvider);
				//(MethodEntityProcessor)entityProcessor;
                var callSitesForMethod = GetCalleesInfo(methodEntity, codeProvider);

				foreach (var callSiteNode in callSitesForMethod.Keys)
				{
					foreach (var calleeAMethod in callSitesForMethod[callSiteNode])
					{
						//var callee = Utils.FindMethodSymbolDeclaration(this.Solution, ((AMethod)calleeAMethod).RoslynMethod);
						var callee = calleeAMethod;
						Logger.LogS("SolutionAnalyzer", "UpdateCallGraph", "\t-> {0}", callee);
						callgraph.AddCallAtLocation(callSiteNode.LocationDescriptor, callerMethod, callee);
					}
				}
			}
        }

		internal static IDictionary<AnalysisCallNode, ISet<MethodDescriptor>> GetCalleesInfo(MethodEntity methodEntity, IProjectCodeProvider codeProvider)
		{
			var calleesPerEntity = new Dictionary<AnalysisCallNode, ISet<MethodDescriptor>>();

			foreach (var calleeNode in methodEntity.PropGraph.CallNodes)
			{
				calleesPerEntity[calleeNode] = GetCallees(methodEntity, calleeNode, codeProvider);
			}

			return calleesPerEntity;
		}
		internal static ISet<MethodDescriptor> GetCallees(MethodEntity methodEntity, AnalysisCallNode node, IProjectCodeProvider codeProvider)
		{
			var calleesForNode = new HashSet<MethodDescriptor>();
			var invExp = methodEntity.PropGraph.GetInvocationInfo((AnalysisCallNode)node);

			Contract.Assert(invExp != null);
			Contract.Assert(codeProvider != null);

			var calleeResult = methodEntity.PropGraph.ComputeCalleesForNode(invExp, codeProvider);

			calleesForNode.UnionWith(calleeResult);

			return calleesForNode;
		}

        private CallGraph<MethodDescriptor, LocationDescriptor> GenerateCallGraph()
        {
            Contract.Assert(this.dispatcher != null);
			//Contract.Assert(dispatcher.GetAllEntites() != null);

			var roots = ProjectCodeProvider.GetMainMethods(this.solution);
            var callgraph = new CallGraph<MethodDescriptor, LocationDescriptor>();
            callgraph.AddRootMethods(roots);
            // var allEntities = new HashSet<IEntity>(this.Dispatcher.GetAllEntites());
            var allEntityDescriptors = this.dispatcher.GetAllEntitiesDescriptors();

			foreach (var entityDesc in allEntityDescriptors)
            {
                //  entity.GetEntityProcessor(this.Dispatcher);
                //var entityProcessor = new MethodEntityProcessor((MethodEntity)entity, this.Dispatcher); 
                var entityProcessor = this.dispatcher.GetEntityWithProcessor(entityDesc);
                
                // Updates the callGraph
                UpdateCallGraph(entityProcessor, callgraph, this.solution);
                var methodEntity = (MethodEntity) entityProcessor.Entity;
                
                methodEntity.Save(Path.Combine(Path.GetTempPath(), 
                    methodEntity.MethodDescriptor.ClassName + "_" + methodEntity.MethodDescriptor.Name + ".dot"));
            }

            //callgraph.Save("cg.dot");
            return callgraph;
        }

        #endregion

	}
}
