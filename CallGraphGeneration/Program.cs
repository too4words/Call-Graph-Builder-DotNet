// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using ReachingTypeAnalysis;
using SolutionTraversal.CallGraph;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Configuration;
using Orleans;
using System.Diagnostics;
using System.Collections.Generic;

namespace CallGraphGeneration
{
    class Program
    {
		private AnalysisStrategyKind strategyKind;
		private AppDomain hostDomain;
		private static OrleansHostWrapper hostWrapper;

		public Program(AnalysisStrategyKind strategyKind)
		{
			this.strategyKind = strategyKind;
		}

        static void Main(string[] args)
        {
			//// This is to generate big synthetic tests in x64 to avoid getting OutOfMemory exceptions
			//var a = new ReachingTypeAnalysis.Tests.CallGraphGenerator();
			//a.GenerateSyntheticSolution();
			//Console.WriteLine("Done!");
			//Console.ReadKey();
			//return;

			args = new string[]
			{
				//@"..\..\..\ConsoleApplication1\ConsoleApplication1.sln", "OnDemandAsync"
				//@"..\..\..\ConsoleApplication1\ConsoleApplication1.sln", "OnDemandOrleans"
				@"..\..\..\TestsSolutions\LongTest2\LongTest2.sln", "OnDemandOrleans"
                //@"C:\Users\diegog\Temp\newSynthetic\synthetic-1000\test.sln", "OnDemandOrleans"
				//@"C:\Users\Edgar\Projects\Call-Graph-Builder\TestsSolutions\synthetic-1000\test.sln", "OnDemandOrleans"
                //@"C:\Users\diegog\Temp\newSynthetic\synthetic-1000000\test.sln", "OnDemandOrleans"
				//@"C:\Users\t-edzopp\Desktop\Roslyn\Roslyn.sln", "OnDemandAsync"
				//@"C:\Users\t-edzopp\Desktop\Roslyn\Roslyn.sln", "OnDemandOrleans"
				//@"C:\Users\t-edzopp\Desktop\ArcusClientPrototype\src\ArcusClient\data\Coby\Coby.sln", "OnDemandAsync"
                //@"C:\Users\t-digarb\Source\Coby\Coby.sln", "OnDemandAsync"
                //@"C:\Users\t-edzopp\Desktop\ArcusClientPrototype\src\ArcusClient\data\Coby\Coby.sln", "OnDemandOrleans"
				
				//@"C:\Users\Edgar\Projects\Test projects\de4dot\de4dot.sln", "OnDemandAsync"
				//@"C:\Users\Edgar\Projects\Test projects\RestSharp\RestSharp.sln", "OnDemandAsync"
				//@"C:\Users\Edgar\Projects\Test projects\buildtools\src\BuildTools.sln", "OnDemandAsync"
				//@"C:\Users\Edgar\Projects\Test projects\codeformatter\src\CodeFormatter.sln", "OnDemandAsync" // works!
				//@"C:\Users\Edgar\Projects\Test projects\Json\Src\Newtonsoft.Json.sln", "OnDemandAsync" // with errors
				//@"C:\azure-powershell\src\ResourceManager.ForRefactoringOnly.sln", "OnDemandAsync"
                //@"C:\Users\Edgar\Projects\Call-Graph-Builder\RealSolutions\codeformatter\src\CodeFormatter.sln", "OnDemandAsync"
				//@"C:\Users\Edgar\Projects\Call-Graph-Builder\RealSolutions\ShareX\ShareX.sln", "OnDemandOrleans"
				//@"C:\Users\Edgar\Projects\Test projects\ShareX\ShareX.sln", "OnDemandOrleans"
			};

			//// This is to compute solution statistics
			//ReachingTypeAnalysis.Statistics.SolutionStats.ComputeSolutionStats(args[0]);
			//Console.WriteLine("Done!");
			//Console.ReadKey();
			//return;

			if (args.Length == 2)
			{
				try
				{
					var solutionPath = args[0];
					var strategyName = args[1];
					var strategyKind = SolutionAnalyzer.StringToAnalysisStrategy(strategyName);
					var outputPath = Path.ChangeExtension(solutionPath, ".dgml");
					var program = new Program(strategyKind);
					var callGraph = program.BuildCallGraph(solutionPath);

					callGraph.Save(outputPath);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
			}

			Console.WriteLine("Done!");
			Console.ReadKey();
        }

		private CallGraph<MethodDescriptor, LocationDescriptor> BuildCallGraph(string solutionPath)
        {
			Console.WriteLine("Analyzing solution...");

			this.Initialize();
			var analyzer = SolutionAnalyzer.CreateFromSolution(solutionPath);
            analyzer.AnalyzeAsync(strategyKind).Wait();

			var rootMethods = analyzer.SolutionManager.GetRootsAsync().Result;
			Console.WriteLine("Root methods={0} ({1})", rootMethods.Count(), AnalysisRootKind.Default);

			var reachableMethods = analyzer.SolutionManager.GetReachableMethodsAsync().Result;
			Console.WriteLine("Reachable methods={0}", reachableMethods.Count());

			var reachableMethods2 = reachableMethods;

			Console.WriteLine("Generating call graph...");

			var callgraph = analyzer.GenerateCallGraphAsync().Result;
			this.Cleanup();

			reachableMethods2 = callgraph.GetReachableMethods();
			Console.WriteLine("Reachable methods={0}", reachableMethods2.Count());

			// TODO: Remove these lines
			var newMethods = reachableMethods2.Except(reachableMethods).ToList();
			var missingMethods = reachableMethods.Except(reachableMethods2).ToList();

			var allMethods = ReachingTypeAnalysis.Statistics.SolutionStats.ComputeSolutionStats(solutionPath);
			missingMethods = allMethods.Except(reachableMethods2).ToList();

			allMethods = allMethods.OrderByDescending(m => m.Name).ToList();
			missingMethods = missingMethods.OrderByDescending(m => m.Name).ToList();

			return callgraph;
        }

		private void Initialize()
		{
			if (strategyKind != AnalysisStrategyKind.ONDEMAND_ORLEANS) return;
			Console.WriteLine("Initializing Orleans silo...");

			var applicationPath = Environment.CurrentDirectory;

			var appDomainSetup = new AppDomainSetup
			{
				AppDomainInitializer = InitSilo,
				ApplicationBase = applicationPath,
				ApplicationName = "CallGraphGeneration",
				AppDomainInitializerArguments = new string[] { },
				ConfigurationFile = "CallGraphGeneration.exe.config"
			};

			// set up the Orleans silo
			hostDomain = AppDomain.CreateDomain("OrleansHost", null, appDomainSetup);

			var xmlConfig = "ClientConfigurationForTesting.xml";
			Contract.Assert(File.Exists(xmlConfig), "Can't find " + xmlConfig);

			GrainClient.Initialize(xmlConfig);
			Console.WriteLine("Orleans silo initialized successfully");
		}

		private void Cleanup()
		{
			if (strategyKind != AnalysisStrategyKind.ONDEMAND_ORLEANS) return;

			hostDomain.DoCallBack(ShutdownSilo);
		}

		private static void InitSilo(string[] args)
		{
			hostWrapper = new OrleansHostWrapper();
			hostWrapper.Init();
			var ok = hostWrapper.Run();

			if (!ok)
			{
				Console.WriteLine("Failed to initialize Orleans silo");
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
    }
}
