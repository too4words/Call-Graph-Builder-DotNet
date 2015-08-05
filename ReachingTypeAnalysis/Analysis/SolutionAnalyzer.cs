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

		public static int MessageCounter { get; private set; }

		internal IAnalysisStrategy Strategy { get; private set; }

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
                        this.AnalyzeOnDemand();
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
						this.Strategy = new OnDemandAsyncStrategy();
                        ISolutionManager solutionManager = null;

                        if (this.source != null)
                        {
                            solutionManager = this.Strategy.CreateFromSourceAsync(this.source).Result;
                        }
                        else
                        {
							solutionManager = this.Strategy.CreateFromSolutionAsync(this.solutionPath).Result;
                        }

						var mainMethods = solutionManager.GetRootsAsync().Result;
						var orchestator = new AnalysisOrchestator(Strategy);
						orchestator.AnalyzeAsync(mainMethods).Wait();

						// This is for debugging just one project
						//var compilerMainMethod = new MethodDescriptor("Microsoft.CodeAnalysis.CSharp.CommandLine", "Program", "Main", true);
						//Console.WriteLine("Analyzing {0}...", compilerMainMethod.Name);
						//orchestator.AnalyzeAsync(compilerMainMethod).Wait();

						var callGraph = orchestator.GenerateCallGraphAsync(solutionManager).Result;
	                    return callGraph;
                    }
                case AnalysisStrategyKind.ONDEMAND_ORLEANS:
					{
						this.Strategy = new OnDemandOrleansStrategy(GrainClient.GrainFactory);

						SolutionAnalyzer.MessageCounter = 0;
						GrainClient.ClientInvokeCallback = OnClientInvokeCallBack;

						var solutionManager = GrainClient.GrainFactory.GetGrain<ISolutionGrain>("Solution");

						if (this.source != null)
						{
							solutionManager.SetSolutionSource(this.source).Wait();
						}
						else
						{
							solutionManager.SetSolutionPath(this.solutionPath).Wait();
						}

						var mainMethods = solutionManager.GetRootsAsync().Result;
						var orchestator = new AnalysisOrchestator(Strategy);
						orchestator.AnalyzeAsync(mainMethods).Wait();

						var callGraph = orchestator.GenerateCallGraphAsync(solutionManager).Result;
						Logger.LogS("SolutionAnalyzer", "Analyze", "Message count {0}", MessageCounter);
						return callGraph;
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
        private void AnalyzeOnDemand()
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

        public void CompareWithRoslynFindReferences(string filename)
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

/*
		private ProjectMethod FindMethodSymbolAndProjectInSolution(MethodDescriptor methodDescriptor, CallGraph<MethodDescriptor, ALocation> callgraph)
        {
            return RoslynSymbolFactory.FindMethodSymbolAndProjectInSolution(this.Solution, methodDescriptor);
        }

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

        public void PerformUpdate(IMethodSymbol oldRoslynMethod, SemanticModel newSemanticModel, IMethodSymbol newRoslynMethod)
        {
            var aMethod = new AMethod(oldRoslynMethod);
            // Get the entity and processor 
            var entityDescriptor = EntityFactory<AMethod>.Create(aMethod);
            var entityProcessor = this.Dispatcher.GetEntityWithProcessor(entityDescriptor) as MethodEntityProcessor<ANode, AType, AMethod>;
            var oldMethodEntity = entityProcessor.MethodEntity;
            var syntaxProcessor = new MethodSyntaxProcessor(newRoslynMethod, newSemanticModel, Dispatcher);
            var newEntity = syntaxProcessor.ParseMethod() as MethodEntity<ANode, AType, AMethod>;

            // I propagate the removal of the node that represent the input parameters of the callees
            // This is to simulate the deletion of the method. 
            // Do I need to do this? 
            var propGraphOld = oldMethodEntity.PropGraph;
            var invoOld = GetInvocations(propGraphOld);
            foreach (var invocation in invoOld)
            {
                foreach (var aCallee in invocation.ComputeCalleesForNode(propGraphOld))
                {
                    RemoveCall(aCallee,invocation);
                }
            }
            // This is to force the callers to call me
            //foreach(var callerConext in  entity.Callers)
            //{
            //    var caller = callerConext.Caller;
            //    var callerEntityProcessor = Dispatcher.GetEntityWithProcessor(new MethodEntityDescriptor<AMethod>(caller));
            //    callerEntityProcessor.DoAnalysis(); 
            //}
            // Here we propagate the removal of the retvalue of the method we eliminate
            if (oldMethodEntity.ReturnVariable != null)
            {
                var returnTypes = oldMethodEntity.GetTypes(oldMethodEntity.ReturnVariable);
                foreach (var callersContext in oldMethodEntity.Callers)
                {
                    RemoveReturnValuesFromCallerLHS(returnTypes, callersContext.Caller, callersContext.CallLHS);
                }
            }

            this.Dispatcher.RegisterEntity(entityDescriptor, newEntity);
            // I get an entity processor to analyze the new entity
            var newEntityProcessor = Dispatcher.GetEntityWithProcessor(entityDescriptor) as MethodEntityProcessor<ANode, AType, AMethod>;
            /// I need to copy all the input data from the old method 
            newEntity.CopyInterfaceDataAndCallers(oldMethodEntity);
            newEntityProcessor.DoAnalysis();

            var propGraphNew = newEntity.PropGraph;
            var invoNew = GetInvocations(propGraphNew);
            //newEntityProcessor.MethodEntity.Save(oldRoslynMethod.ContainingType.Name + "_" + oldRoslynMethod.Name + "_d.dot");
            oldMethodEntity.InvalidateCaches();
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
