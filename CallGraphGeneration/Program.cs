// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using ReachingTypeAnalysis;
using SolutionTraversal.Callgraph;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Configuration;
using Orleans;

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
				//@"C:\Users\t-edzopp\Desktop\Roslyn\RoslynLight.sln", "OnDemandAsync"
			};

			if (args.Length == 2)
			{
				try
				{
					var solutionFileName = args[0];
					var strategyName = args[1];
					var strategyKind = SolutionAnalyzer.StringToAnalysisStrategy(strategyName);
					var outputFileName = Path.ChangeExtension(solutionFileName, ".dgml");

					var program = new Program(strategyKind);
					var callGraph = program.Analyze(solutionFileName);

					callGraph.Save(outputFileName);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
			}

			Console.WriteLine("Done!");
			Console.ReadKey();
        }

		public CallGraph<MethodDescriptor, LocationDescriptor> Analyze(string solutionFileName)
		{
			var solution = this.LoadSolution(solutionFileName);
			var callGraph = this.BuildCallGraph(solution);

			return callGraph;
		}

		private Solution LoadSolution(string solutionFileName)
        {
			var solutionName = Path.GetFileName(solutionFileName);
			Console.WriteLine("Loading solution {0}...", solutionName);

            var ws = MSBuildWorkspace.Create();
			var solution = ws.OpenSolutionAsync(solutionFileName).Result;
            var pathNetFramework = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            var pathToDll = pathNetFramework + @"Facades\";
            //@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5.1\Facades\";
            // Didn't work
            pathToDll = ConfigurationManager.AppSettings["PathToDLLs"];
            Contract.Assert(Directory.Exists(pathToDll));

            var references = new string[]
			{
				"System.Runtime.dll",
				"System.Threading.Tasks.dll",
				"System.Reflection.dll",
				"System.Text.Encoding.dll"
			};
					
			var metadataReferences = references.Select(s => MetadataReference.CreateFromFile(pathToDll + s));

			foreach (var projectId in solution.ProjectIds)
            {
                solution = solution.AddMetadataReferences(projectId, metadataReferences);
            }

            return solution;
        }

		private CallGraph<MethodDescriptor, LocationDescriptor> BuildCallGraph(Solution solution)
        {
			Console.WriteLine("Analyzing solution...");
            var analyzer = new SolutionAnalyzer(solution);

			this.Initialize();
            var callgraph = analyzer.Analyze(strategyKind);
			this.Cleanup();

            Console.WriteLine("Reachable methods={0}", callgraph.GetReachableMethods().Count);
            return callgraph;
        }

		private void Initialize()
		{
			if (strategyKind != AnalysisStrategyKind.ONDEMAND_ORLEANS) return;

			var applicationPath = System.Environment.CurrentDirectory;

			var appDomainSetup = new AppDomainSetup
			{
				AppDomainInitializer = InitSilo,
				//ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
				ApplicationBase = applicationPath,
				ApplicationName = "CallGraphGeneration",
				AppDomainInitializerArguments = new string[] { },
				//ConfigurationFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReachingTypeAnalysis.exe.config")
				ConfigurationFile = "CallGraphGeneration.exe.config"
			};

			// set up the Orleans silo
			hostDomain = AppDomain.CreateDomain("OrleansHost", null, appDomainSetup);

			var xmlConfig = "ClientConfigurationForTesting.xml";
			Contract.Assert(File.Exists(xmlConfig), "Can't find " + xmlConfig);

			GrainClient.Initialize(xmlConfig);
			Console.WriteLine("Orleans silo initialized");
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
