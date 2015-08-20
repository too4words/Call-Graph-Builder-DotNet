// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Orleans;
using OrleansInterfaces;
using ReachingTypeAnalysis.Analysis;
using ReachingTypeAnalysis.Communication;
using ReachingTypeAnalysis.Roslyn;
using SolutionTraversal.CallGraph;
using Microsoft.CodeAnalysis.MSBuild;

namespace ReachingTypeAnalysis
{
    public enum AnalysisStrategyKind
    {
        ONDEMAND_SYNC,
        ENTIRE_SYNC,
        ONDEMAND_ASYNC,
        ONDEMAND_ORLEANS,
        ENTIRE_ASYNC,
        NONE,
    }

	public class SolutionAnalyzer
	{
		private string source;
		private string solutionPath;

		private Solution solution;
		private IDispatcher dispatcher;

		public ISolutionManager SolutionManager { get; private set; }

		public static int MessageCounter { get; private set; }

		private SolutionAnalyzer()
		{
		}

		public static SolutionAnalyzer CreateFromSolution(string solutionPath)
        {
            var analyzer = new SolutionAnalyzer();
			analyzer.solutionPath = solutionPath;
			return analyzer;
        }

		public static SolutionAnalyzer CreateFromSource(string source)
		{
			var analyzer = new SolutionAnalyzer();
			analyzer.source = source;
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
                case AnalysisStrategyKind.ENTIRE_SYNC:
                    {
						this.solution = this.GetSolution();
						this.dispatcher = new SynchronousLocalDispatcher();
                        this.AnalyzeEntireSolution();
                        return this.GenerateCallGraph();
                    }
                case AnalysisStrategyKind.ONDEMAND_ASYNC:
                    {
						this.AnalyzeOnDemandAsync().Wait();
						var callgraph = this.GenerateCallGraphAsync().Result;
                        return callgraph;
                    }
                case AnalysisStrategyKind.ONDEMAND_ORLEANS:
					{
						this.AnalyzeOnDemandOrleans().Wait();
						var callgraph = this.GenerateCallGraphAsync().Result;
						return callgraph;
					}
                case AnalysisStrategyKind.ENTIRE_ASYNC:
                    {
						this.solution = this.GetSolution();
						this.dispatcher = new AsyncDispatcher();
                        this.AnalyzeEntireSolutionAsync();
                        return this.GenerateCallGraph();
					}
                default:
                    {
                        throw new ArgumentException("Unknown value for Solver " + ConfigurationManager.AppSettings["Solver"]);
                    }
            }
        }

		internal Task RemoveMethod(MethodDescriptor methodDescriptor, string newSource)
		{
			var orchestator = new AnalysisOrchestator(this.SolutionManager);
			return orchestator.RemoveMethodAsync(methodDescriptor, newSource);
		}

		internal Task AddMethod(MethodDescriptor methodDescriptor, string newSource)
		{
			var orchestator = new AnalysisOrchestator(this.SolutionManager);
			return orchestator.AddMethodAsync(methodDescriptor, newSource);
		}

		internal Task UpdateMethod(MethodDescriptor methodDescriptor, string newSource)
		{
			var orchestator = new AnalysisOrchestator(this.SolutionManager);
			return orchestator.UpdateMethodAsync(methodDescriptor, newSource);
		}


		public async Task<CallGraph<MethodDescriptor, LocationDescriptor>> AnalyzeAsync(AnalysisStrategyKind strategyKind = AnalysisStrategyKind.NONE)
        {
            if (strategyKind == AnalysisStrategyKind.NONE)
            {
                strategyKind = StringToAnalysisStrategy(ConfigurationManager.AppSettings["Strategy"]);
            }

            switch (strategyKind)
            {
                case AnalysisStrategyKind.ONDEMAND_ASYNC:
                    {
						await this.AnalyzeOnDemandAsync();
						var callgraph = await this.GenerateCallGraphAsync();
                        return callgraph;
                    }
                case AnalysisStrategyKind.ONDEMAND_ORLEANS:
                    {
						await this.AnalyzeOnDemandOrleans();
						var callgraph = await this.GenerateCallGraphAsync();
						return callgraph;
                    }
                default:
                    {
                        throw new ArgumentException("Unknown value for Solver " + ConfigurationManager.AppSettings["Solver"]);
                    }
            }
        }

        internal async Task AnalyzeOnDemandAsync()
        {
            if (this.source != null)
            {
                this.SolutionManager = await AsyncSolutionManager.CreateFromSourceAsync(this.source);
            }
            else
            {
                this.SolutionManager = await AsyncSolutionManager.CreateFromSolutionAsync(this.solutionPath);
            }

            var mainMethods = await this.SolutionManager.GetRootsAsync();
            var orchestator = new AnalysisOrchestator(this.SolutionManager);
            await orchestator.AnalyzeAsync(mainMethods);
        }

		public async Task AnalyzeOnDemandOrleans()
        {
			SolutionAnalyzer.MessageCounter = 0;
			GrainClient.ClientInvokeCallback = OnClientInvokeCallBack;

			// For orleans we cannot use the strategy to create a solution
			// The solution grain creates an internal strategy and contain an internal 
			// solution manager. We obtain the solution grain that handles everything
            var solutionManager = GrainClient.GrainFactory.GetGrain<ISolutionGrain>("Solution");
			this.SolutionManager = solutionManager;

			if (this.source != null)
            {
                await solutionManager.SetSolutionSourceAsync(this.source);
            }
            else
            {
                await solutionManager.SetSolutionPathAsync(this.solutionPath);
            }

            var mainMethods = await this.SolutionManager.GetRootsAsync();
            var orchestator = new AnalysisOrchestator(this.SolutionManager);
            await orchestator.AnalyzeAsync(mainMethods);
			//var callGraph = await orchestator.GenerateCallGraphAsync();
			Logger.Log(GrainClient.Logger, "SolutionAnalyzer", "Analyze", "Message count {0}", MessageCounter);
			//return callGraph;
        }

		internal async Task<CallGraph<MethodDescriptor, LocationDescriptor>> GenerateCallGraphAsync()
		{
			Logger.Instance.Log("AnalysisOrchestator", "GenerateCallGraph", "Start building CG");
			var callgraph = new CallGraph<MethodDescriptor, LocationDescriptor>();		
			var roots = await SolutionManager.GetRootsAsync();
			var worklist = new Queue<MethodDescriptor>(roots);
			var visited = new HashSet<MethodDescriptor>();

			callgraph.AddRootMethods(roots);

			while (worklist.Count > 0)
			{
				var currentMethodDescriptor = worklist.Dequeue();
				visited.Add(currentMethodDescriptor);
				Logger.Instance.Log("AnalysisOrchestator", "GenerateCallGraph", "Proccesing  {0}", currentMethodDescriptor);

				var methodEntity = await this.SolutionManager.GetMethodEntityAsync(currentMethodDescriptor);
				var calleesInfoForMethod = await methodEntity.GetCalleesInfoAsync();

				foreach (var entry in calleesInfoForMethod)
				{
					var analysisNode = entry.Key;
					var callees = entry.Value;

					foreach (var calleeDescriptor in callees)
					{
						Logger.Instance.Log("AnalysisOrchestator", "GenerateCallGraph", "Adding {0}-{1} to CG", currentMethodDescriptor, calleeDescriptor);
						callgraph.AddCallAtLocation(analysisNode.LocationDescriptor, currentMethodDescriptor, calleeDescriptor);

						if (!visited.Contains(calleeDescriptor) && !worklist.Contains(calleeDescriptor))
						{
							worklist.Enqueue(calleeDescriptor);
						}
					}
				}
			}

			return callgraph;
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

        private void OnClientInvokeCallBack(Orleans.CodeGeneration.InvokeMethodRequest arg1, IGrain arg2)
        {
            // TODO: Check. Because this is static.
            MessageCounter++;
        }

        public static AnalysisStrategyKind StringToAnalysisStrategy(string strategy)
		{
			switch (strategy)
			{
				case "OnDemandSync": return AnalysisStrategyKind.ONDEMAND_SYNC;
				case "EntireSync":  return AnalysisStrategyKind.ENTIRE_SYNC;
				case "OnDemandAsync": return AnalysisStrategyKind.ONDEMAND_ASYNC;
				case "OnDemandOrleans": return AnalysisStrategyKind.ONDEMAND_ORLEANS;
                case "EntireAsync":return AnalysisStrategyKind.ENTIRE_ASYNC;
				default:  throw new ArgumentException("Unknown value for the strategy: " + strategy);
			}
		}

        private void AnalyzeEntireSolution()
        {
			foreach (var project in this.solution.Projects)
			{
				var compilation = project.GetCompilationAsync().Result;
				var diag = compilation.GetDiagnostics();
				var theAssembly = compilation.Assembly;

				foreach (var tree in compilation.SyntaxTrees)
				{
                    var model = compilation.GetSemanticModel(tree);
					var allMethodsVisitor = new AllMethodsVisitor(model, tree);
					allMethodsVisitor.Visit(tree.GetRoot());
				}
			}
        }

        private void AnalyzeEntireSolutionAsync()
        {
			// TOOD: hack -- set the global solution
			ProjectCodeProvider.Solution = this.solution;

			foreach (var project in this.solution.Projects)
			{
				var compilation = project.GetCompilationAsync().Result;
				var diag = compilation.GetDiagnostics();
				var theAssembly = compilation.Assembly;
				var continuations = new List<Task>();

				foreach (var tree in compilation.SyntaxTrees)
				{
					var provider = new ProjectCodeProvider(project, compilation);
                    var model = compilation.GetSemanticModel(tree);
                    var allMethodsVisitor = new AllMethodsVisitor(model, tree);

					continuations.Add(allMethodsVisitor.Run(tree));
				}

				Task.WhenAll(continuations);
			}
        }

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
					Logger.Instance.Log("SolutionAnalyzer", "AnalyzeOnDemand", "--- Done with propagation ---");
				}
			}

			if (this.dispatcher is QueueingDispatcher)
            {
                var qd = (QueueingDispatcher)this.dispatcher;

                while (!qd.IsDoneProcessing)
                {
                    Logger.Instance.Log("SolutionAnalyzer", "AnalyzeOnDemand", "Waiting for the queue to empty up...");
                    Thread.Sleep(1000);
                }
            }
        }

        internal void CompareWithRoslynFindReferences(string filename)
        {
            var writer = File.CreateText(filename);
            writer.WriteLine("Caller; Callee; CG; Roslyn; CG vs R; R vs CG");
            var allEntities = new HashSet<IEntity>(this.dispatcher.GetAllEntites());
            var max = 0;
			var count = 0;
			var sum = 0;
            var countDiff = 0;

            foreach (var e in allEntities)
            {
				var methodEntity = e as MethodEntity;
                // Updates the callGraph
                var method = methodEntity.MethodDescriptor;
                //var methodEntityProcessor = (MethodEntityProcessor)methodEntity.GetEntityProcessor(this.Dispatcher);
                var methodEntityProcessor = (MethodEntityProcessor)this.dispatcher.GetEntityWithProcessor(methodEntity.EntityDescriptor);

                foreach (var callNode in methodEntity.PropGraph.CallNodes)
                {
                    var countCG = CallGraphQueryInterface.GetCalleesAsync(methodEntity, callNode, methodEntityProcessor.codeProvider).Result.Count();
                    var invExp = methodEntity.PropGraph.GetInvocationInfo(callNode);

                    if (invExp is MethodCallInfo)
                    {
						var callInfo = invExp as MethodCallInfo;
                        var calleeRoslynMethod = RoslynSymbolFactory.FindMethodSymbolInSolution(this.solution, callInfo.Method);
                        var calleeReferences = SymbolFinder.FindImplementationsAsync(calleeRoslynMethod, this.solution).Result;
                        var roslynCount = calleeReferences.Count();

						writer.WriteLine("{0}; {1}; {2}; {3}; {4}; {5}", method.ToString(), callInfo.Method.ToString(), countCG,roslynCount ,roslynCount-countCG, countCG-roslynCount );
                        max = Math.Max(max, roslynCount-countCG);

						if (roslynCount - countCG > 5) countDiff++;

                        sum += roslynCount- countCG;
                        count++;
                    }

                    if (invExp is DelegateCallInfo)
                    {
						var delegateInfo = invExp as DelegateCallInfo;
						// TODO???
                    }
                }
            }

            writer.WriteLine(";;;;{0} ; {1}",  max, (float)sum/count);
            writer.WriteLine("More than {1};;;;{0} ", countDiff,5);
            writer.Close();
            //callgraph.Save("cg.dot");
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
                var callSitesForMethod = CallGraphQueryInterface.GetCalleesInfo(methodEntity, codeProvider).Result;

				foreach (var callSiteNode in callSitesForMethod.Keys)
				{
					foreach (var calleeAMethod in callSitesForMethod[callSiteNode])
					{
						//var callee = Utils.FindMethodSymbolDeclaration(this.Solution, ((AMethod)calleeAMethod).RoslynMethod);
						var callee = calleeAMethod;
						Logger.Instance.Log("SolutionAnalyzer", "UpdateCallGraph", "\t-> {0}", callee);
						callgraph.AddCallAtLocation(callSiteNode.LocationDescriptor, callerMethod, callee);
					}
				}
			}
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
