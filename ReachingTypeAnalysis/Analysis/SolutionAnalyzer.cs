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
using ReachingTypeAnalysis.Statistics;

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

	public class SolutionAnalyzer : IAnalysisObserver
	{
		private string source;
		private string solutionPath;
		private string testName;
		private IDictionary<Guid, EffectsDispatcherStatus> dispatchersStatus;

		public ISolutionManager SolutionManager { get; private set; }
        public AnalysisRootKind RootKind { get; set; } 

		public static int MessageCounter { get; private set; }

		private SolutionAnalyzer()
		{
			this.dispatchersStatus = new Dictionary<Guid, EffectsDispatcherStatus>();
		}

		public static SolutionAnalyzer CreateFromSolution(string solutionPath)
        {
            var analyzer = new SolutionAnalyzer();
			analyzer.solutionPath = solutionPath;
            analyzer.RootKind = AnalysisRootKind.Default;
			return analyzer;
        }

		public static SolutionAnalyzer CreateFromSource(string source)
		{
			var analyzer = new SolutionAnalyzer();
			analyzer.source = source;
            analyzer.RootKind = AnalysisRootKind.Default;
            return analyzer;
		}

		public static SolutionAnalyzer CreateFromTest(string testName)
		{
			var analyzer = new SolutionAnalyzer();
			analyzer.testName= testName;
            analyzer.RootKind = AnalysisRootKind.Default;
            return analyzer;
		}

		/// <summary>
		/// IMPORTANT: OnDemandSolvers need an OnDemand Dispatcher
		/// We cannot use the SyncronousDistacther because it doesn't look for the 
		/// methods when they are not available. This only works with the entire solution
		/// analysis!!!!
		/// </summary>
		public CallGraph<MethodDescriptor, LocationDescriptor> Analyze(AnalysisStrategyKind strategyKind = AnalysisStrategyKind.NONE)
        {
            if (strategyKind == AnalysisStrategyKind.NONE)
            {
                strategyKind = StringToAnalysisStrategy(ConfigurationManager.AppSettings["Strategy"]);
            }

            switch (strategyKind)
            {
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
                default:
                    {
                        throw new ArgumentException("Unknown value for Solver " + ConfigurationManager.AppSettings["Solver"]);
                    }
            }
        }

		public Task ApplyModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			var orchestator = new AnalysisOrchestrator(this.SolutionManager);
			return orchestator.ApplyModificationsAsync(modifiedDocuments);
		}

		internal Task RemoveMethodAsync(MethodDescriptor methodDescriptor, string newSource)
		{
			var orchestator = new AnalysisOrchestrator(this.SolutionManager);
			return orchestator.RemoveMethodAsync(methodDescriptor, newSource);
		}

		internal Task AddMethodAsync(MethodDescriptor methodDescriptor, string newSource)
		{
			var orchestator = new AnalysisOrchestrator(this.SolutionManager);
			return orchestator.AddMethodAsync(methodDescriptor, newSource);
		}

		internal Task UpdateMethodAsync(MethodDescriptor methodDescriptor, string newSource)
		{
			var orchestator = new AnalysisOrchestrator(this.SolutionManager);
			return orchestator.UpdateMethodAsync(methodDescriptor, newSource);
		}

		public async Task AnalyzeAsync(AnalysisStrategyKind strategyKind = AnalysisStrategyKind.NONE)
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
						break;
                    }
                case AnalysisStrategyKind.ONDEMAND_ORLEANS:
                    {
						await this.AnalyzeOnDemandOrleans();
						break;
                    }
                default:
                    {
                        throw new ArgumentException("Unknown value for Solver " + ConfigurationManager.AppSettings["Solver"]);
                    }
            }
        }

        private async Task AnalyzeOnDemandAsync()
        {
            if (this.source != null)
            {
                this.SolutionManager = await AsyncSolutionManager.CreateFromSourceAsync(this.source);
            }
			else if (this.testName != null)
			{
				this.SolutionManager = await AsyncSolutionManager.CreateFromTestAsync(this.testName);
			}
			else if(this.solutionPath != null)
            {
                this.SolutionManager = await AsyncSolutionManager.CreateFromSolutionAsync(this.solutionPath);
            }
			else
			{
				throw new Exception("We need a solutionPath, source code or testName to analyze");
			}

			var roots = await this.SolutionManager.GetRootsAsync(this.RootKind);
			var orchestator = new AnalysisOrchestrator(this.SolutionManager);
            await orchestator.AnalyzeAsync(roots);
        }

		public async Task AnalyzeOnDemandOrleans()
        {
			await this.InitializeOnDemandOrleansAnalysis();
			await this.WaitForOnDemandOrleansAnalysisToBeReady();
			await this.ContinueOnDemandOrleansAnalysis();
        }

		public async Task InitializeOnDemandOrleansAnalysis()
		{
			SolutionAnalyzer.MessageCounter = 0;
			//GrainClient.ClientInvokeCallback = OnClientInvokeCallBack;

			// For orleans we cannot use the strategy to create a solution
			// The solution grain creates an internal strategy and contain an internal 
			// solution manager. We obtain the solution grain that handles everything
			var solutionGrain = OrleansSolutionManager.GetSolutionGrain(GrainClient.GrainFactory);
			this.SolutionManager = solutionGrain;

			if (this.source != null)
			{
				await solutionGrain.SetSolutionSourceAsync(this.source);
			}
			else if (this.testName != null)
			{
				await solutionGrain.SetSolutionFromTestAsync(this.testName);
			}
			else if (this.solutionPath != null)
			{
				await solutionGrain.SetSolutionPathAsync(this.solutionPath);
                Logger.LogWarning(GrainClient.Logger, "SolutionAnalyzer", "InitializeOnDemandOrleansAsync", "Exit SetSolutionPath");
            }
            else
			{
				throw new Exception("We need a solutionPath, source code or testName to analyze");
			}
		}

		public Task<EntityGrainStatus> GetOnDemandOrleansAnalysisStatus()
		{
			var solutionGrain = this.SolutionManager as ISolutionGrain;
			return solutionGrain.GetStatusAsync();
		}

		public async Task WaitForOnDemandOrleansAnalysisToBeReady(int millisecondsDelay = 100)
		{
			var solutionGrain = this.SolutionManager as ISolutionGrain;
			var solutionGrainStatus = await solutionGrain.GetStatusAsync();

			while (solutionGrainStatus != EntityGrainStatus.Ready && AnalysisClient.ExperimentStatus != ExperimentStatus.Cancelled)
			{
				await Task.Delay(millisecondsDelay);
				solutionGrainStatus = await solutionGrain.GetStatusAsync();
			}
		}

		public async Task ContinueOnDemandOrleansAnalysis()
		{
			Logger.LogForDebug(GrainClient.Logger, "@@[Client] Starting analysis...");

			await this.SubscribeToAllDispatchersAsync();
			await this.ProcessRootMethodsAsync();
			await this.WaitForTerminationAsync();

			Logger.LogForDebug(GrainClient.Logger, "@@[Client] Analysis end");

			//var roots = await this.SolutionManager.GetRootsAsync(this.RootKind);
			//Logger.LogWarning(GrainClient.Logger, "SolutionAnalyzer", "ContinueOnDemandOrleansAnalysis", "Roots count {0} ({1})", roots.Count(), this.RootKind);
			//
			//var orchestator = new AnalysisOrchestrator(this.SolutionManager);
			//await orchestator.AnalyzeAsync(roots);
			////await orchestator.AnalyzeDistributedAsync(roots);

			//var callGraph = await orchestator.GenerateCallGraphAsync();
			Logger.LogInfo(GrainClient.Logger, "SolutionAnalyzer", "ContinueOnDemandOrleansAnalysis", "Message count {0}", MessageCounter);
			//return callGraph;
		}
		
		private async Task SubscribeToAllDispatchersAsync()
		{
			var tasks = new List<Task>();
			var objref = await GrainClient.GrainFactory.CreateObjectReference<IAnalysisObserver>(this);

			this.dispatchersStatus.Clear();

			for (var i = 0; i < AnalysisConstants.StreamCount; ++i)
			{
				var dispatcherId = string.Format(AnalysisConstants.StreamGuidFormat, i);
				var dispatcherGuid = Guid.Parse(dispatcherId);				
				var dispatcher = OrleansEffectsDispatcherManager.GetEffectsDispatcherGrain(GrainClient.GrainFactory, dispatcherGuid);

				this.dispatchersStatus.Add(dispatcherGuid, EffectsDispatcherStatus.Busy);

				var task = dispatcher.Subscribe(objref);
				//await task;
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
		}

		private async Task ProcessRootMethodsAsync()
		{
			var tasks = new List<Task>();
			var roots = await this.SolutionManager.GetRootsAsync(this.RootKind);

			Logger.LogWarning(GrainClient.Logger, "SolutionAnalyzer", "ContinueOnDemandOrleansAnalysis", "Roots count {0} ({1})", roots.Count(), this.RootKind);

			foreach (var method in roots)
			{
				var task = this.ProcessMethodAsync(method);
				//await task;
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
		}

		private async Task ProcessMethodAsync(MethodDescriptor method)
		{
			Logger.LogS("SolutionAnalyzer", "ProcessMethod", "Analyzing: {0}", method);

			//var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(method);
			var methodEntityProc = await this.GetMethodEntityGrainAndActivateInProject(method);

			await methodEntityProc.UseDeclaredTypesForParameters();
			await methodEntityProc.PropagateAndProcessAsync(PropagationKind.ADD_TYPES);

			Logger.LogS("SolutionAnalyzer", "ProcessMethod", "End Analyzing {0} ", method);
		}

		private Task<IMethodEntityGrain> GetMethodEntityGrainAndActivateInProject(MethodDescriptor method)
		{
			var methodEntityGrain = OrleansMethodEntity.GetMethodEntityGrain(GrainClient.GrainFactory, method);
			return Task.FromResult(methodEntityGrain);

			//var methodEntityProc = await this.SolutionManager.GetMethodEntityAsync(method); //as IMethodEntityGrain;
			//// Force MethodGrain placement near projects
			////var codeProvider = await this.SolutionManager.GetProjectCodeProviderAsync(method);
			////var methodEntityProc = await codeProvider.GetMethodEntityAsync(method) as IMethodEntityGrain;
			//return methodEntityProc as IMethodEntityGrain;
		}

		private async Task WaitForTerminationAsync()
		{
			while (this.dispatchersStatus.Any(e => e.Value == EffectsDispatcherStatus.Busy))
			{
				Logger.LogForDebug(GrainClient.Logger, "@@[Client] Waiting for termination...");
				await Task.Delay(AnalysisConstants.WaitForTerminationDelay);
			}
		}

		void IAnalysisObserver.OnEffectsDispatcherStatusChanged(IEffectsDispatcherGrain sender, EffectsDispatcherStatus newStatus)
		{
			var dispatcherGuid = sender.GetPrimaryKey();
			this.dispatchersStatus[dispatcherGuid] = newStatus;

			Logger.LogForDebug(GrainClient.Logger, "@@[Client] Dispatcher {0} is {1}", dispatcherGuid, newStatus);
		}

		public async Task<CallGraph<MethodDescriptor, LocationDescriptor>> GenerateCallGraphAsync()
		{
			Logger.LogS("SolutionAnalyzer", "GenerateCallGraphAsync", "Start building CG");
			var callgraph = new CallGraph<MethodDescriptor, LocationDescriptor>();
			var roots = await this.SolutionManager.GetRootsAsync(this.RootKind);
			var worklist = new Queue<MethodDescriptor>(roots);
			var visited = new HashSet<MethodDescriptor>();

			callgraph.AddRootMethods(roots);

			while (worklist.Count > 0)
			{
				var currentMethodDescriptor = worklist.Dequeue();
				visited.Add(currentMethodDescriptor);
				Logger.LogS("SolutionAnalyzer", "GenerateCallGraphAsync", "Proccesing  {0}", currentMethodDescriptor);

				var methodEntity = await this.SolutionManager.GetMethodEntityAsync(currentMethodDescriptor);
				var calleesInfoForMethod = await methodEntity.GetCalleesInfoAsync();

				foreach (var entry in calleesInfoForMethod)
				{
					var analysisNode = entry.Key;
					var callees = entry.Value;

					foreach (var calleeDescriptor in callees)
					{
						Logger.LogS("SolutionAnalyzer", "GenerateCallGraphAsync", "Adding {0}-{1} to CG", currentMethodDescriptor, calleeDescriptor);
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
				solution = SolutionFileGenerator.CreateSolution(this.source);
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

		//private void AnalyzeEntireSolutionAsync()
		//{
		//	// TOOD: hack -- set the global solution
		//	ProjectCodeProvider.Solution = this.solution;

		//	foreach (var project in this.solution.Projects)
		//	{
		//		var compilation = project.GetCompilationAsync().Result;
		//		var diag = compilation.GetDiagnostics();
		//		var theAssembly = compilation.Assembly;
		//		var continuations = new List<Task>();

		//		foreach (var tree in compilation.SyntaxTrees)
		//		{
		//			var provider = new ProjectCodeProvider(project, compilation);
		//			var model = compilation.GetSemanticModel(tree);
		//			var allMethodsVisitor = new AllMethodsVisitor(model, tree);

		//			continuations.Add(allMethodsVisitor.Run(tree));
		//		}

		//		Task.WhenAll(continuations);
		//	}
		//}

        internal void CompareWithRoslynFindReferences(string filename)
        {
			//var writer = File.CreateText(filename);
			//writer.WriteLine("Caller; Callee; CG; Roslyn; CG vs R; R vs CG");
			//var allEntities = new HashSet<IEntity>(this.dispatcher.GetAllEntites());
			//var max = 0;
			//var count = 0;
			//var sum = 0;
			//var countDiff = 0;

			//foreach (var e in allEntities)
			//{
			//	var methodEntity = e as MethodEntity;
			//	// Updates the callGraph
			//	var method = methodEntity.MethodDescriptor;
			//	//var methodEntityProcessor = (MethodEntityProcessor)methodEntity.GetEntityProcessor(this.Dispatcher);
			//	var methodEntityProcessor = (MethodEntityProcessor)this.dispatcher.GetEntityWithProcessor(methodEntity.EntityDescriptor);

			//	foreach (var callNode in methodEntity.PropGraph.CallNodes)
			//	{
			//		var countCG = CallGraphQueryInterface.GetCalleesAsync(methodEntity, callNode, methodEntityProcessor.codeProvider).Result.Count();
			//		var invExp = methodEntity.PropGraph.GetInvocationInfo(callNode);

			//		if (invExp is MethodCallInfo)
			//		{
			//			var callInfo = invExp as MethodCallInfo;
			//			var calleeRoslynMethod = RoslynSymbolFactory.FindMethodSymbolInSolution(this.solution, callInfo.Method);
			//			var calleeReferences = SymbolFinder.FindImplementationsAsync(calleeRoslynMethod, this.solution).Result;
			//			var roslynCount = calleeReferences.Count();

			//			writer.WriteLine("{0}; {1}; {2}; {3}; {4}; {5}", method.ToString(), callInfo.Method.ToString(), countCG,roslynCount ,roslynCount-countCG, countCG-roslynCount );
			//			max = Math.Max(max, roslynCount-countCG);

			//			if (roslynCount - countCG > 5) countDiff++;

			//			sum += roslynCount- countCG;
			//			count++;
			//		}

			//		if (invExp is DelegateCallInfo)
			//		{
			//			var delegateInfo = invExp as DelegateCallInfo;
			//			// TODO???
			//		}
			//	}
			//}

			//writer.WriteLine(";;;;{0} ; {1}",  max, (float)sum/count);
			//writer.WriteLine("More than {1};;;;{0} ", countDiff,5);
			//writer.Close();
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
	}
}
