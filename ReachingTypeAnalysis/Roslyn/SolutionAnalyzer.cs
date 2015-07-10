// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using ReachingTypeAnalysis.Analysis;
using ReachingTypeAnalysis.Communication;
using ReachingTypeAnalysis.Roslyn;
using SolutionTraversal.Callgraph;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using OrleansInterfaces;

namespace ReachingTypeAnalysis
{
	public enum AnalysisStrategy
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
		// Just for text
		internal Solution Solution { get; private set; }

		internal IDispatcher Dispatcher { get; private set; }

        internal string SourceCode { get; private set; }
		public SolutionAnalyzer(Solution solution)
		{
			Solution = solution;
			//dispatcher = new SynchronousLocalDispatcher();
		}

        public SolutionAnalyzer(string sourceCode)
        {
            Solution = Utils.CreateSolution(sourceCode);
            this.SourceCode = sourceCode;
            //dispatcher = new SynchronousLocalDispatcher();
        }

		/// <summary>
		/// IMPORTANT: OnDemandSolvers need an OnDemand Dispatcher
		/// We cannot use the SyncronousDistacther because it doesn't look for the 
		/// methods when they are not available. This only works with the entire solution
		/// analysis!!!!
		/// </summary>
		/// <param name="dispatcher"></param>
        public CallGraph<MethodDescriptor, LocationDescriptor>
            Analyze(AnalysisStrategy strategy = AnalysisStrategy.NONE, bool produceCallGraph = true)
        {
            if (strategy == AnalysisStrategy.NONE)
            {
                strategy = ConvertToEnum(ConfigurationManager.AppSettings["Strategy"]);
            }


            // TOOD: hack -- set the global solution
            ProjectCodeProvider.Solution = this.Solution;

            switch (strategy)
            {
                case AnalysisStrategy.ONDEMAND_SYNC:
                    {
                        this.Dispatcher = new OnDemandSyncDispatcher();
                        AnalyzeOnDemand();
                        return this.GenerateCallGraph();
                    }
                case AnalysisStrategy.ENTIRE_SYNC:
                    {
                        this.Dispatcher = new SynchronousLocalDispatcher();
                        AnalyzeEntireSolution();

                        return this.GenerateCallGraph();
                    }
                case AnalysisStrategy.ONDEMAND_ASYNC:
                    {
                        //this.Dispatcher = new QueueingDispatcher(this.Solution);
                        this.Dispatcher = new AsyncDispatcher();
                        AnalyzeOnDemandAsync(AnalysisStrategy.ONDEMAND_ASYNC).Wait();
                        
                        return this.GenerateCallGraph();
                    }
                case AnalysisStrategy.ONDEMAND_ORLEANS:
                    {
                        //var applicationPath = AppDomain.CurrentDomain.BaseDirectory;
                        var orleansPath = System.Environment.GetEnvironmentVariable("ORLEANSSDK");
                        //@"C:\Microsoft Project Orleans SDK v1.0\SDK\LocalSilo\Applications\ReachingTypeAnalysis";
                        var applicationPath = Path.Combine(Path.Combine(orleansPath, @"LocalSilo\Applications"), "ReachingTypeAnalysis");

                        var appDomainSetup = new AppDomainSetup
                        {
                            AppDomainInitializer = InitSilo,
                            //ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                            ApplicationBase = applicationPath,
                            ApplicationName = "ReachingTypeAnalysis",
                            AppDomainInitializerArguments = new string[] { },
                            ConfigurationFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReachingTypeAnalysis.exe.config")
                        };
                        // set up the Orleans silo
                        var hostDomain = AppDomain.CreateDomain("OrleansHost", null, appDomainSetup);

                        var xmlConfig = "DevTestClientConfiguration.xml";
                        Contract.Assert(File.Exists(xmlConfig), "Can't find " + xmlConfig);
                        try
                        {
                            GrainClient.Initialize(xmlConfig);
                            Logger.Instance.Log("SolutionAnalyzer", "Analyze", "Orleans silo initialized");
                        }
                        catch (Exception e)
                        {
                            Logger.Instance.Log("SolutionAnalyzer", "Analyze", e.Message);
                            throw e;
                            //break;
                        }
                        // Create a Grain for the solution
                        ISolutionGrain solutionGrain = SolutionGrainFactory.GetGrain("Solution");
                        Contract.Assert(solutionGrain != null);
                        if (SourceCode != null)
                        {
                            solutionGrain.SetSolutionSource(SourceCode).Wait();
                            IProjectCodeProviderGrain projectGrain = ProjectCodeProviderGrainFactory.GetGrain("MyProject");
                            projectGrain.SetProjectSourceCode(SourceCode);
                        }
                        else
                        {
                            Contract.Assert(Solution.FilePath != null);
                            solutionGrain.SetSolutionPath(this.Solution.FilePath).Wait();
                            foreach (var project in this.Solution.Projects)
                            {
                                IProjectCodeProviderGrain projectGrain = ProjectCodeProviderGrainFactory.GetGrain(project.Name);
                                projectGrain.SetProjectPath(project.FilePath).Wait();
                            }
                        }
                        // make a dispatcher
                        //this.Dispatcher = new OrleansDispatcher();
                        this.Dispatcher = null;
                        // run
                        var cancellationSource = new CancellationTokenSource();
                        var triple = ProjectCodeProvider.GetProviderContainingEntryPointAsync(this.Solution, cancellationSource.Token).Result;
                        if (triple != null)
                        {
                            // cancel out outstanding processing tasks
                            //cancellationSource.Cancel();	
                            var provider = triple.Item1;
                            var mainSymbol = triple.Item2;
                            Contract.Assert(mainSymbol != null);
                            //var tree = triple.Item3;
                            //var model = provider.Compilation.GetSemanticModel(tree);
                            var mainMethodDescriptor = Utils.CreateMethodDescriptor(mainSymbol);
                            var mainMethodEntityDescriptor = mainMethodDescriptor;

                            AnalysisOrchestator.AnalyzeAsync(mainMethodEntityDescriptor).Wait();
                            var callGraph = AnalysisOrchestator.GenerateCallGraph(solutionGrain).Result;

                            hostDomain.DoCallBack(ShutdownSilo);
                            return callGraph;
                        }

                        return null;
                    }
                case AnalysisStrategy.ENTIRE_ASYNC:
                    {
                        //this.Dispatcher = new QueueingDispatcher(this.Solution);
                        this.Dispatcher = new AsyncDispatcher();
                        AnalyzeEntireSolutionAsync();

                        return this.GenerateCallGraph();
                    }
                default:
                    {
                        throw new ArgumentException("Unknown value for Solver " + ConfigurationManager.AppSettings["Solver"]);
                    }
            }
        }

        private static OrleansHostWrapper hostWrapper;

        private static void InitSilo(string[] args)
        {
            hostWrapper = new OrleansHostWrapper(args);

            if (!hostWrapper.Run())
            {
                Logger.Instance.Log("SolutionAnalyzer", "InitSilo", "Failed to initialize Orleans silo");
            }
        }

        private static void ShutdownSilo()
        {
            if (hostWrapper != null)
            {
                hostWrapper.Dispose();
                GC.SuppressFinalize(hostWrapper);
            }
        }

        private static AnalysisStrategy ConvertToEnum(string strategy)
		{
			switch (strategy)
			{
				case "OnDemandSync":
					{
						return AnalysisStrategy.ONDEMAND_SYNC;
					}
				case "EntireSync":
					{
						return AnalysisStrategy.ENTIRE_SYNC;
					}
				case "OnDemandAsync":
					{
						return AnalysisStrategy.ONDEMAND_ASYNC;
					}
                case "OrleansAsync":
                    {
                        return AnalysisStrategy.ONDEMAND_ORLEANS;
                    }
                case "EntireAsync":
					{
						return AnalysisStrategy.ENTIRE_ASYNC;
					}
				default:
					{
						throw new ArgumentException("Unknown value for the strategy: " + strategy);
					}
			}
		}

		public void AnalyzeWithoutAppSettings(Dispatcher dispatcher = null)
        {
            if (dispatcher == null)
            {
                this.Dispatcher = new OnDemandSyncDispatcher();
            }
            AnalyzeOnDemand();
        }

        public void AnalyzeEntireSolution()
        {
			foreach (var project in this.Solution.Projects)
			{
				var compilation = project.GetCompilationAsync().Result;
				var diag = compilation.GetDiagnostics();

				var theAssembly = compilation.Assembly;

				foreach (var tree in compilation.SyntaxTrees)
				{
                    var model = compilation.GetSemanticModel(tree);
					var codeProvider = new ProjectCodeProvider(project, compilation);
					var allMethodsVisitor = new AllMethodsVisitor(model, tree, this.Dispatcher);
					allMethodsVisitor.Visit(tree.GetRoot());
				}
			}
            //if (this.Dispatcher is QueueingDispatcher)
            //{
            //    var qd = (QueueingDispatcher)this.Dispatcher;
            //    while (!qd.IsDoneProcessing)
            //    {
            //        Logger.Instance.Log("Waiting for the queue to empty up...");
            //        Thread.Sleep(1000);
            //    }
            //}

            /*
            if (mainMethod != null)
            {
                var methodDescriptor = new MethodDescriptor(mainMethod);
                callgraph.AddRootMethod(methodDescriptor);

				var mainMethodEntityDescriptor = new MethodEntityDescriptor<AMethod>(new AMethod(mainMethod));
				var mainEntityProcessor = this.Dispatcher.GetEntityWithProcessor(mainMethodEntityDescriptor);

				mainEntityProcessor.DoAnalysis();
				GenerateCallGraph();
            }*/
        }

        public void AnalyzeEntireSolutionAsync()
        {
			foreach (var project in this.Solution.Projects)
			{
				var compilation = project.GetCompilationAsync().Result;
				var diag = compilation.GetDiagnostics();

				var theAssembly = compilation.Assembly;

				var continuations = new List<Task>();
				foreach (var tree in compilation.SyntaxTrees)
				{
					var provider = new ProjectCodeProvider(project, compilation);
                    var model = compilation.GetSemanticModel(tree);
                    var allMethodsVisitor = new AllMethodsVisitor(model, tree, this.Dispatcher);
					continuations.Add(allMethodsVisitor.Run(tree));
				}
				Task.WhenAll(continuations);
			}
            //if (this.Dispatcher is QueueingDispatcher)
            //{
            //    var qd = (QueueingDispatcher)this.Dispatcher;
            //    while (!qd.IsDoneProcessing)
            //    {
            //        Logger.Instance.Log("Waiting for the queue to empty up...");
            //        Thread.Sleep(1000);
            //    }
            //}

            /*
            if (mainMethod != null)
            {
                var methodDescriptor = new MethodDescriptor(mainMethod);
                callgraph.AddRootMethod(methodDescriptor);

                var mainMethodEntityDescriptor = new MethodEntityDescriptor<AMethod>(new AMethod(mainMethod));
                var mainEntityProcessor = this.Dispatcher.GetEntityWithProcessor(mainMethodEntityDescriptor) as MethodEntityProcessor<ANode,AType,AMethod>;
                
                // Just a test
                //mainEntityProcessor.MethodEntity.CurrentContext = new CallConext<AMethod, ANode>(mainEntityProcessor.MethodEntity.Method, null, null);
                
                var task = mainEntityProcessor.DoAnalysisAsync();
                task.Start();
                task.Wait();

                //var methodEntity = (MethodEntity<ANode, AType, AMethod>)mainEntityProcessor.Entity;
                var methodEntity = mainEntityProcessor.MethodEntity;
                Thread.Sleep(10);

                //if (this.Dispatcher is AsyncDispatcher)
                //{
                //    while (!mainEntityProcessor.HasFinishedAllProgatations())
                //    {
                //        Logger.Instance.Log("Waiting in Main for the queue to empty up... {0}",mainEntityProcessor.MethodEntity.NodesProcessing.Count());
                //        Thread.Sleep(10);
                //    }
                //}
                GenerateCallGraph();
            }*/
        }

        /// <summary>
        /// Try to get the roslyn methods on the fly
        /// Currently works with one project.
        /// </summary>
        public void AnalyzeOnDemand()
        {
			Contract.Assert(this.Dispatcher != null);
			var cancellationToken = new CancellationTokenSource();
			var projectIDs = this.Solution.GetProjectDependencyGraph().GetTopologicallySortedProjects(cancellationToken.Token);
			foreach (var projectId in projectIDs)
			{
				var project = this.Solution.GetProject(projectId);
				var compilation = project.GetCompilationAsync().Result;

                var triple = ProjectCodeProvider.GetProviderContainingEntryPointAsync(project, cancellationToken.Token).Result;
                var provider = triple.Item1;
                IMethodSymbol mainSymbol = triple.Item2;
                var tree = triple.Item3;
				if (provider != null)
				{
                    var model = provider.Compilation.GetSemanticModel(tree);
					cancellationToken.Cancel(); // cancel out outstanding processing tasks
                    var methodVisitor = new MethodSyntaxProcessor(model, tree, mainSymbol);

					var mainMethodEntity = methodVisitor.ParseMethod();
                    this.Dispatcher.RegisterEntity(mainMethodEntity.EntityDescriptor, mainMethodEntity);
                    var mainEntityProcessor = new MethodEntityProcessor((MethodEntity)mainMethodEntity, this.Dispatcher);
                    //var mainMethodDescriptor =  new MethodDescriptor(mainSymbol);
                    //var mainMethodEntityDescriptor = EntityFactory.Create(mainMethodDescriptor);
                    //var mainEntityProcessor = this.Dispatcher.GetEntityWithProcessorAsync(mainMethodEntityDescriptor).Result ;

					// Just a test
					//mainEntityProcessor.MethodEntity.CurrentContext = new CallConext<AMethod, ANode>(mainEntityProcessor.MethodEntity.Method, null, null);

					mainEntityProcessor.DoAnalysis();

					Logger.Instance.Log("SolutionAnalyzer", "AnalyzeOnDemand", "--- Done with propagation ---");
				}
			}

			if (this.Dispatcher is QueueingDispatcher)
            {
                var qd = (QueueingDispatcher)this.Dispatcher;
                while (!qd.IsDoneProcessing)
                {
                    Logger.Instance.Log("SolutionAnalyzer", "AnalyzeOnDemand", "Waiting for the queue to empty up...");
                    Thread.Sleep(1000);
                }
            }

        }

		/// <summary>
		/// Try to get the roslyn methods on the fly
		/// Currently works with one project.
		/// </summary>
		public async Task AnalyzeOnDemandAsync(AnalysisStrategy strategy)
		{
			Contract.Assert(this.Dispatcher != null);
			var cancellationSource = new CancellationTokenSource();
			var triple = await ProjectCodeProvider.GetProviderContainingEntryPointAsync(this.Solution, cancellationSource.Token);
			if (triple != null)
			{
				// cancel out outstanding processing tasks
				//cancellationSource.Cancel();	

				var provider = triple.Item1;
				var mainSymbol = triple.Item2;
                var tree = triple.Item3;
                var model = provider.Compilation.GetSemanticModel(tree);
				var methodVisitor = new MethodSyntaxProcessor(model, tree, mainSymbol);

				//var mainMethodEntity = methodVisitor.ParseMethod();
                var mainMethodDescriptor = Utils.CreateMethodDescriptor(mainSymbol);
                var mainMethodEntityDescriptor = mainMethodDescriptor;

                IEntityProcessor mainMethodEntityProcessor = null;
                switch(strategy)
                {
                    case AnalysisStrategy.ONDEMAND_ORLEANS:
                        await AnalysisOrchestator.AnalyzeAsync(mainMethodDescriptor);
                        
                        //var methodEntityGrain = await OrleansDispatcher.CreateMethodEntityGrain((OrleansEntityDescriptor)mainMethodEntityDescriptor);
                        // Option 1: Direcly using the Grain
                        //await methodEntityGrain.DoAnalysisAsync();
                        // Option 2: Using the processor (requires making the processor serializable)
                        //mainMethodEntityProcessor = await methodEntityGrain.GetEntityWithProcessorAsync();
                        //await mainMethodEntityProcessor.DoAnalysisAsync();
                        break;
                    case AnalysisStrategy.ONDEMAND_ASYNC:
                        mainMethodEntityProcessor = await this.Dispatcher.GetEntityWithProcessorAsync(new MethodEntityDescriptor(mainMethodEntityDescriptor));
					    var mainMethodEntity = ((MethodEntityProcessor)mainMethodEntityProcessor).MethodEntity;
                        this.Dispatcher.RegisterEntity(mainMethodEntity.EntityDescriptor, mainMethodEntity);
                        await mainMethodEntityProcessor.DoAnalysisAsync();
                        break;
                }
                //await Task.WhenAll(mainMethodEntityProcessor.DoAnalysisAsync());
                //Thread.Sleep(1000);
				Logger.Instance.Log("SolutionAnalyzer", "AnalyzeOnDemandAsync", "--- Done with propagation ---");
			}
		}

        public void CompareWithRoslynFindReferences(Solution solution, string filename)
        {
            var writer = File.CreateText(filename);
            writer.WriteLine("Caller; Callee; CG; Roslyn; CG vs R; R vs CG");
            var allEntities = new HashSet<IEntity>(this.Dispatcher.GetAllEntites());
            int max = 0; int count = 0; int sum = 0;
            int countDiff = 0;
            foreach (var e in allEntities)
            {
				var methodEntity = e as MethodEntity;
                // Updates the callGraph
                var method = methodEntity.MethodDescriptor;
                //var methodEntityProcessor = (MethodEntityProcessor)methodEntity.GetEntityProcessor(this.Dispatcher);
                var methodEntityProcessor = (MethodEntityProcessor)Dispatcher.GetEntityWithProcessor(methodEntity.EntityDescriptor);

                foreach (var callNode in methodEntity.PropGraph.CallNodes)
                {
                    int countCG = QueryInterfaces.CalleesAsync(methodEntity, callNode, methodEntityProcessor.codeProvider).Result.Count();
                    var invExp = methodEntity.PropGraph.GetInvocationInfo(callNode);
                    if (invExp is MethodCallInfo)
                    {
						var callInfo = invExp as MethodCallInfo;
                        var calleeRoslynMethod = RoslynSymbolFactory.FindMethodSymbolInSolution(solution, callInfo.Method);
                        var calleeReferences = SymbolFinder.FindImplementationsAsync(calleeRoslynMethod, solution).Result;
                        var roslynCount = calleeReferences.Count();
                        writer.WriteLine("{0}; {1}; {2}; {3}; {4}; {5}", method.ToString(), callInfo.Method.ToString(), countCG,roslynCount ,roslynCount-countCG, countCG-roslynCount );
                        max = Math.Max(max, roslynCount-countCG);
                        if (roslynCount - countCG > 5)
                            countDiff++;
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
            writer.WriteLine("More than {1};;;;{0} ",countDiff,5);
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
        private static void UpdateCallGraph(IEntityProcessor entityProcessor, 
                                            CallGraph<MethodDescriptor, LocationDescriptor> callgraph, Solution solution)
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
                var callSitesForMethod = QueryInterfaces.GetCalleesInfo(methodEntity, codeProvider).Result;
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

        public CallGraph<MethodDescriptor, LocationDescriptor> GenerateCallGraph()
        {
            var roots = ProjectCodeProvider.GetMainMethods(this.Solution);
            Contract.Assert(this.Dispatcher != null);
            //Contract.Assert(this.Dispatcher.GetAllEntites() != null);

            var callgraph = new CallGraph<MethodDescriptor, LocationDescriptor>();
            callgraph.AddRootMethods(roots);
            // var allEntities = new HashSet<IEntity>(this.Dispatcher.GetAllEntites());
            var allEntityDescriptors = this.Dispatcher.GetAllEntitiesDescriptors();
            foreach (var entityDesc in allEntityDescriptors)
            {
                //  entity.GetEntityProcessor(this.Dispatcher);
                //var entityProcessor = new MethodEntityProcessor((MethodEntity)entity, this.Dispatcher); 
                var entityProcessor = this.Dispatcher.GetEntityWithProcessor(entityDesc);
                
                // Updates the callGraph
                UpdateCallGraph(entityProcessor, callgraph,this.Solution);
                MethodEntity methodEntity = (MethodEntity) entityProcessor.Entity;
                
                methodEntity.Save(Path.Combine(Path.GetTempPath(), 
                    methodEntity.MethodDescriptor.ClassName + "_" + methodEntity.MethodDescriptor.Name + ".dot"));
            }
            //callgraph.Save("cg.dot");

            return callgraph;
        }

        private static CallGraph<MethodDescriptor, LocationDescriptor> ReBuildCallGraph(Dispatcher dispatcher, Solution solution)
        {
            var callgraph = new CallGraph<MethodDescriptor, LocationDescriptor>();
            // pg.PropagateDeletionOfNodes();
            foreach (var e in dispatcher.GetAllEntites())
            {
                var entityProcessor = new MethodEntityProcessor((MethodEntity)e, dispatcher);// e.GetEntityProcessor(dispatcher);
                var methodEntity = (MethodEntity)entityProcessor.Entity;
                if (methodEntity.MethodDescriptor.ToString().Contains("Main"))
                {
                    callgraph.AddRootMethod(methodEntity.MethodDescriptor);
                }
                // Updates the callGraph
                UpdateCallGraph(entityProcessor, callgraph, solution);
            }
            //callgraph.Save("cg_d.dot");

            return callgraph;
        }
        #endregion
    }
}
