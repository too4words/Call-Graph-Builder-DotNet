// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orleans;
using ReachingTypeAnalysis.Analysis;
using SolutionTraversal.CallGraph;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace ReachingTypeAnalysis
{
	internal delegate void RunChecks(SolutionAnalyzer s, CallGraph<MethodDescriptor, LocationDescriptor> callgraph);

	internal static class TestUtils
    {
        public static void AnalyzeExample(string source, RunChecks checker, AnalysisStrategyKind strategy = AnalysisStrategyKind.NONE)
        {
			Environment.SetEnvironmentVariable("MyIPAddr", "127.0.0.1");
            //var solution = ReachingTypeAnalysis.Utils.CreateSolution(source);
            //var solAnalyzer = new SolutionAnalyzer(solution);
            var solAnalyzer = SolutionAnalyzer.CreateFromSource(source);
            var callgraph = solAnalyzer.Analyze(strategy);

			if (strategy == AnalysisStrategyKind.ONDEMAND_ORLEANS)
			{
				var myStatsGrain = StatsHelper.GetStatGrain(GrainClient.GrainFactory);
				myStatsGrain.ResetStats().Wait();
			}

            checker(solAnalyzer, callgraph);
        }

		public static void AnalyzeExample(string source, RunChecks initialChecker, Action<SolutionAnalyzer> updates, RunChecks updatesChecker, AnalysisStrategyKind strategy = AnalysisStrategyKind.NONE)
		{
			Environment.SetEnvironmentVariable("MyIPAddr", "127.0.0.1");
			//var solution = ReachingTypeAnalysis.Utils.CreateSolution(source);
			//var solAnalyzer = new SolutionAnalyzer(solution);
			var solAnalyzer = SolutionAnalyzer.CreateFromSource(source);
			var callgraph = solAnalyzer.Analyze(strategy);

			if (strategy == AnalysisStrategyKind.ONDEMAND_ORLEANS)
			{
				var myStatsGrain = StatsHelper.GetStatGrain(GrainClient.GrainFactory);
				myStatsGrain.ResetStats().Wait();
			}

			initialChecker(solAnalyzer, callgraph);
			updates(solAnalyzer);
			callgraph = solAnalyzer.GenerateCallGraphAsync().Result;
			updatesChecker(solAnalyzer, callgraph);
		}

		public static void AnalyzeSolution(string solutionPath, RunChecks checker, AnalysisStrategyKind strategy = AnalysisStrategyKind.NONE)
        {
			Environment.SetEnvironmentVariable("MyIPAddr", "127.0.0.1");

			var solAnalyzer = SolutionAnalyzer.CreateFromSolution(solutionPath);
            var callgraph = solAnalyzer.Analyze(strategy);

			if (strategy == AnalysisStrategyKind.ONDEMAND_ORLEANS)
			{
				var myStatsGrain = StatsHelper.GetStatGrain(GrainClient.GrainFactory);
				myStatsGrain.ResetStats().Wait();
			}

			checker(solAnalyzer, callgraph);
        }

		public static void AnalyzeSolution(string solutionPath, RunChecks initialChecker, Action<SolutionAnalyzer> updates, RunChecks updatesChecker, AnalysisStrategyKind strategy = AnalysisStrategyKind.NONE)
		{
			Environment.SetEnvironmentVariable("MyIPAddr", "127.0.0.1");

			var solAnalyzer = SolutionAnalyzer.CreateFromSolution(solutionPath);
			var callgraph = solAnalyzer.Analyze(strategy);

			if (strategy == AnalysisStrategyKind.ONDEMAND_ORLEANS)
			{
				var myStatsGrain = StatsHelper.GetStatGrain(GrainClient.GrainFactory);
				myStatsGrain.ResetStats().Wait();
			}

			initialChecker(solAnalyzer, callgraph);
			updates(solAnalyzer);
			callgraph = solAnalyzer.GenerateCallGraphAsync().Result;
			updatesChecker(solAnalyzer, callgraph);
		}
		
        public static string GetTestSolutionPath(string solutionPath)
        {
			var currentSolutionPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
			currentSolutionPath = Path.GetDirectoryName(currentSolutionPath);
			
			solutionPath = Path.Combine(currentSolutionPath, @"..\..\..\TestsSolutions", solutionPath);
			solutionPath = Path.GetFullPath(solutionPath);

			Assert.IsTrue(File.Exists(solutionPath));
			return solutionPath;
        }

		public static string GetRealTestSolutionPath(string solutionPath)
		{
			var currentSolutionPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
			currentSolutionPath = Path.GetDirectoryName(currentSolutionPath);

			solutionPath = Path.Combine(currentSolutionPath, @"..\..\..\..\RealSolutions", solutionPath);
			solutionPath = Path.GetFullPath(solutionPath);

            Assert.IsTrue(File.Exists(solutionPath), "Can't find " + solutionPath);
			return solutionPath;
		}

		public static IEnumerable<string> CopyFiles(string sourceFolder, string destinationFolder)
		{
			var copiedFiles = new List<string>();
			var sourceFiles = Directory.EnumerateFiles(sourceFolder, "*", SearchOption.AllDirectories);

			foreach (var fileName in sourceFiles)
			{
				var targetFileName = fileName.Replace(sourceFolder, destinationFolder);
				var targetFolder = Path.GetDirectoryName(targetFileName);

				if (!Directory.Exists(targetFolder))
				{
					Directory.CreateDirectory(targetFolder);
				}

				File.Copy(fileName, targetFileName, true);
				copiedFiles.Add(targetFileName);
            }

			return copiedFiles;
		}

        public static void CompareWithRoslyn(string solutionPath)
        {
            AnalyzeSolution(solutionPath,
				(s, callgraph) =>
				{
					callgraph.Save("solution1.dot");
					s.CompareWithRoslynFindReferences(solutionPath + ".txt");
				});
        }        
    }
}
