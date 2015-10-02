// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
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
			args = new string[]
			{
				//@"..\..\..\ConsoleApplication1\ConsoleApplication1.sln", "OnDemandAsync"
				@"..\..\..\ConsoleApplication1\ConsoleApplication1.sln", "OnDemandOrleans"
                //@"c:\Users\diegog\Temp\Projects3\test.sln", "OnDemandOrleans"
				//@"C:\Users\t-edzopp\Desktop\Roslyn\Roslyn.sln", "OnDemandAsync"
				//@"C:\Users\t-edzopp\Desktop\Roslyn\Roslyn.sln", "OnDemandOrleans"
				//@"C:\Users\t-edzopp\Desktop\ArcusClientPrototype\src\ArcusClient\data\Coby\Coby.sln", "OnDemandAsync"
                //@"C:\Users\t-digarb\Source\Coby\Coby.sln", "OnDemandAsync"
                //@"C:\Users\t-edzopp\Desktop\ArcusClientPrototype\src\ArcusClient\data\Coby\Coby.sln", "OnDemandOrleans"
				
				//@"C:\Users\Edgar\Projects\Test projects\de4dot\de4dot.sln", "OnDemandAsync"
				//@"C:\Users\Edgar\Projects\Test projects\RestSharp\RestSharp.sln", "OnDemandAsync"
			};
			
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
            var callgraph = analyzer.Analyze(strategyKind);
			this.Cleanup();

			//// TODO: remove this assert, it is just for debugging
			//Debug.Assert(false);

			var reachableMethods = callgraph.GetReachableMethods();
			Console.WriteLine("Reachable methods={0}", reachableMethods.Count);
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
