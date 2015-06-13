// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Orleans;
using ReachingTypeAnalysis.Communication;
using SolutionTraversal.Callgraph;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using ReachingTypeAnalysis.Analysis;

namespace ReachingTypeAnalysis
{
    public class Program
    {
        static void Main(string[] args)
        {
            var hostDomain = AppDomain.CreateDomain("OrleansHost", null, new AppDomainSetup
            {
                AppDomainInitializer = InitSilo,
                AppDomainInitializerArguments = new string[] { },
            });

            GrainClient.Initialize("DevTestClientConfiguration.xml");

            if (args.Length == 0)
            {
                //throw new ArgumentException("Not enough parameters to main");
            }
            var solutionPath = args[0];
            var solution = ReadSolution(solutionPath);
            //var callGraph = TypePropagationAnalysis.BuildCallGraph(solution);
            //callGraph.Save("cg.dot");

            //CompareDispatchers(solution);
            var callgraph = GenerateCallGraph(solution);
            Console.WriteLine("Nodes: {0}", callgraph.GetNodes().Count());

            hostDomain.DoCallBack(ShutdownSilo);
        }

        static void InitSilo(string[] args)
        {
            hostWrapper = new OrleansHostWrapper(args);

            if (!hostWrapper.Run())
            {
                Console.Error.WriteLine("Failed to initialize Orleans silo");
            }
        }

        internal static void ShutdownSilo()
        {
            if (hostWrapper != null)
            {
                hostWrapper.Dispose();
                GC.SuppressFinalize(hostWrapper);
            }
        }

        private static OrleansHostWrapper hostWrapper;

        private static CallGraph<MethodDescriptor, LocationDescriptor> GenerateCallGraph(Solution solution)
        {
            var analyzer = new SolutionAnalyzer(solution);
            var timerLocal = new Stopwatch();
            timerLocal.Start();
            // This dispacher doesn't parse the methods... analyzerLocal.Analyze(new SynchronousLocalDispatcher());
            analyzer.Analyze(AnalysisStrategy.ONDEMAND_ORLEANS);
            timerLocal.Stop();

            return analyzer.GenerateCallGraph();
        }

        private static bool CompareDispatchers(Solution solution)
        {
            var analyzerLocal = new SolutionAnalyzer(solution);
            var timerLocal = new Stopwatch();
            timerLocal.Start();
            // This dispacher doesn't parse the methods... analyzerLocal.Analyze(new SynchronousLocalDispatcher());
            analyzerLocal.Analyze(AnalysisStrategy.ONDEMAND_SYNC);
            timerLocal.Stop();
            var callgraphLocal = analyzerLocal.GenerateCallGraph();
            Debug.WriteLine(string.Format("Local analysis time: {0}", timerLocal.Elapsed));

            var analyzerParallel = new SolutionAnalyzer(solution);
            var timerQueuing = new Stopwatch();
            timerQueuing.Start();
            analyzerParallel.Analyze(AnalysisStrategy.ONDEMAND_ASYNC);

            /*           
            using (var queryingDispatcher = new QueueingDispatcher(solution))
            {
                analyzerParallel.AnalyzeEntireSolution();

				// block here waiting
				while (!queryingDispatcher.IsDoneProcessing)
				{
					Console.WriteLine("Queue {0}", queryingDispatcher.GetQueueCount());
					Console.WriteLine("A total of {0} messages delivered", queryingDispatcher.MessageCount);
					System.Threading.Thread.Sleep(10000);
				}
            }*/
            timerQueuing.Stop();

            var callgraphQueuing = analyzerParallel.GenerateCallGraph();
            Debug.WriteLine(string.Format("Queueing analysis time: {0}", timerLocal.Elapsed));

            Debug.WriteLine(string.Format("Analysis {0} {1} methods ", 1, callgraphLocal.GetReachableMethods().Count));
            Debug.WriteLine(string.Format("Analysis {0} {1} methods ", 2, callgraphQueuing.GetReachableMethods().Count));

            if (callgraphLocal.GetReachableMethods().Count != callgraphQueuing.GetReachableMethods().Count)
            {
                return false;
            }
            if (callgraphLocal.GetEdges().Count() != callgraphQueuing.GetEdges().Count())
            {
                return false;
            }

            // seems like they are the same
            return true;
        }

        public static Solution ReadSolution(string path)
        {
            if (!File.Exists(path)) throw new ArgumentException("Missing " + path);
            var ws = MSBuildWorkspace.Create();

            var solution = ws.OpenSolutionAsync(path).Result;
            //string pathNetFramework = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            //string pathToDll = pathNetFramework + @"Facades\";        
            // Didn't work 
            // These ones works
            //pathToDll = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5.1\Facades\";
            string pathToDll = ConfigurationManager.AppSettings["PathToDLLs"];
            Contract.Assert(pathToDll != null && Directory.Exists(pathToDll));

            var metadataReferences = new string[] {
                    "System.Runtime.dll",
                    "System.Threading.Tasks.dll",
                    "System.Reflection.dll",
                    "System.Text.Encoding.dll"}.Select(s => MetadataReference.CreateFromFile(pathToDll + s));
            var pIds = solution.ProjectIds;
            foreach (var pId in pIds)
                solution = solution.AddMetadataReferences(pId, metadataReferences);

            return solution;
        }
    }
}
