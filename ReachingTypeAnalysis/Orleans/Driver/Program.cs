// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Orleans;
using ReachingTypeAnalysis.Communication;
using SolutionTraversal.CallGraph;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using ReachingTypeAnalysis.Analysis;
using System.Reflection;
using System.Xml.Linq;

namespace ReachingTypeAnalysis
{
    public class OldDriver : IDisposable
    {
        StreamWriter file;
        public OldDriver(string fileName)
        {
            file = File.CreateText(fileName);
            file.WriteLine("Test, Avg, Max, Min");
        }
        public void Dispose()
        {
            file.Dispose();
        }

        static void Main(string[] args)
        {
            //RunProgramWithOrleans(args);

            if (args[0].EndsWith(".playlist"))
            {
                RunTests(args);
            }
            else
            {
                RunTestFromCmdLine(args);
            }
            Console.WriteLine("Done");
            Console.ReadKey();
        }
        private static void RunTests(string[] args)
        {
            var program = new OldDriver("stats-edgard.txt");
            
            var playListName = args[0];
            var iterations = int.Parse(args[1]);

            var xdoc = XDocument.Load(playListName);
            var tests = from lv1 in xdoc.Descendants("Add")
                       select lv1.Attribute("Test").Value;
            foreach(var test in tests)
            {
                var name = test.Substring(test.LastIndexOf('.')+1);
                program.RunOneTest(name, iterations);
            }
        }

        private static void RunTestFromCmdLine(string[] args)
        {
            var program = new OldDriver("stats-edgard.txt");

            var testToExecute = args[0];
            var iterations = int.Parse(args[1]);
            program.RunOneTest(testToExecute, iterations);

        }

        private void RunOneTest(string testToExecute, int iterations)
        {

            Console.WriteLine("Executing {0}", testToExecute);
            var minTime = long.MaxValue;
            var maxTime = 0L;
            var acumTime = 0D;
            for (int i = 0; i < iterations; i++)
            {
                Console.WriteLine("Iteration {0}", i); 
                var watch = new Stopwatch();
                var test = new Tests();
                var methodToExecute = test.GetType().GetMethod(testToExecute);
                watch.Start();
                methodToExecute.Invoke(test, new object[0]);
                watch.Stop();
                var time = watch.ElapsedMilliseconds;
                if (time > maxTime)
                    maxTime = time;
                if (time < minTime)
                    minTime = time;
                acumTime += time;
            }
            var avgTime = acumTime / iterations;
            file.WriteLine("{3}, {0}, {1}, {2}", avgTime, maxTime, minTime,testToExecute);
            file.Flush();
        }

        private static void RunProgramWithOrleans(string[] args)
        {
            var hostDomain = AppDomain.CreateDomain("OrleansHost", null, new AppDomainSetup
            {
                AppDomainInitializer = InitSilo,
                AppDomainInitializerArguments = new string[] { },
            });

            GrainClient.Initialize("ClientConfigurationForTesting.xml");

            if (args.Length == 0)
            {
                //throw new ArgumentException("Not enough parameters to main");
            }
            var solutionPath = args[0];
			//CompareDispatchers(solutionPath);
			var callgraph = GenerateCallGraph(solutionPath);
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

        private static CallGraph<MethodDescriptor, LocationDescriptor> GenerateCallGraph(string solutionPath)
        {
            var analyzer = SolutionAnalyzer.CreateFromSolution(solutionPath);
            var timerLocal = new Stopwatch();
            timerLocal.Start();
            // This dispacher doesn't parse the methods... analyzerLocal.Analyze(new SynchronousLocalDispatcher());
            var callgraph = analyzer.Analyze(AnalysisStrategyKind.ONDEMAND_ORLEANS);
            timerLocal.Stop();

            return callgraph;
        }

        private static bool CompareDispatchers(string solutionPath)
        {
            var analyzerLocal = SolutionAnalyzer.CreateFromSolution(solutionPath);
            var timerLocal = new Stopwatch();
            timerLocal.Start();

			// This dispacher doesn't parse the methods... analyzerLocal.Analyze(new SynchronousLocalDispatcher());
			var callgraphLocal = analyzerLocal.Analyze(AnalysisStrategyKind.ONDEMAND_SYNC);
            timerLocal.Stop();

            Logger.Instance.Log("Program", "CompareDispatchers", "Local analysis time: {0}", timerLocal.Elapsed);

            var analyzerParallel = SolutionAnalyzer.CreateFromSolution(solutionPath);
            var timerQueuing = new Stopwatch();
            timerQueuing.Start();
			var callgraphQueuing = analyzerParallel.Analyze(AnalysisStrategyKind.ONDEMAND_ASYNC);
            timerQueuing.Stop();

            Logger.Instance.Log("Program", "CompareDispatchers", "Queueing analysis time: {0}", timerLocal.Elapsed);
            Logger.Instance.Log("Program", "CompareDispatchers", "Analysis {0} {1} methods ", 1, callgraphLocal.GetReachableMethods().Count);
            Logger.Instance.Log("Program", "CompareDispatchers", "Analysis {0} {1} methods ", 2, callgraphQueuing.GetReachableMethods().Count);

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
    }
}
