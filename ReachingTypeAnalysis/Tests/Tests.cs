﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
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
    [TestClass]
    public partial class BasicTests
    {
        delegate void RunChecks(SolutionAnalyzer s, CallGraph<MethodDescriptor, LocationDescriptor> callgraph);

        private static void AnalyzeExample(string source, RunChecks checker, AnalysisStrategyKind strategy = AnalysisStrategyKind.NONE)
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

		private static void AnalyzeExample(string source, RunChecks initialChecker, Action<SolutionAnalyzer> updates, RunChecks updatesChecker, AnalysisStrategyKind strategy = AnalysisStrategyKind.NONE)
		{
			//var solution = ReachingTypeAnalysis.Utils.CreateSolution(source);
			//var solAnalyzer = new SolutionAnalyzer(solution);
			var solAnalyzer = SolutionAnalyzer.CreateFromSource(source);
			var callgraph = solAnalyzer.Analyze(strategy);

			initialChecker(solAnalyzer, callgraph);
			updates(solAnalyzer);
			callgraph = solAnalyzer.GenerateCallGraphAsync().Result;
			updatesChecker(solAnalyzer, callgraph);
		}

		private static void AnalyzeSolution(string solutionPath, RunChecks checker, AnalysisStrategyKind strategy = AnalysisStrategyKind.NONE)
        {
            var solAnalyzer = SolutionAnalyzer.CreateFromSolution(solutionPath);
            var callgraph = solAnalyzer.Analyze(strategy);
            
            checker(solAnalyzer, callgraph);
        }

		private static void AnalyzeSolution(string solutionPath, RunChecks initialChecker, Action<SolutionAnalyzer> updates, RunChecks updatesChecker, AnalysisStrategyKind strategy = AnalysisStrategyKind.NONE)
		{
			var solAnalyzer = SolutionAnalyzer.CreateFromSolution(solutionPath);
			var callgraph = solAnalyzer.Analyze(strategy);

			initialChecker(solAnalyzer, callgraph);
			updates(solAnalyzer);
			callgraph = solAnalyzer.GenerateCallGraphAsync().Result;
			updatesChecker(solAnalyzer, callgraph);
		}

		[TestMethod]
        [TestCategory("Solutions")]
        public void TestSolution1OnDemandAsync()
        {
            BasicTests.TestSolution1Local(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

		[TestMethod]
		[TestCategory("Solutions")]
		public void TestRealSolutionOnDemandAsync()
		{
			BasicTests.TestRealSolutionLocal(AnalysisStrategyKind.ONDEMAND_ASYNC);
		}

        [TestMethod]
        [TestCategory("Solutions")]
        public void TestSolution1ShareOnDemandAsync()
        {
            BasicTests.TestSolution1Share(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }
		
        public static void TestSolution1Local(AnalysisStrategyKind strategy)
        {
			var currentSolutionPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
			currentSolutionPath = Path.GetDirectoryName(currentSolutionPath);
			
            var solutionPath = ConfigurationManager.AppSettings["Solution1"];
			solutionPath = Path.Combine(currentSolutionPath, @"..\..\..\TestsSolutions", solutionPath);
			solutionPath = Path.GetFullPath(solutionPath);

			TestSolution1(strategy, solutionPath);
        }

		public static void TestRealSolutionLocal(AnalysisStrategyKind strategy)
		{
			var currentSolutionPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
			currentSolutionPath = Path.GetDirectoryName(currentSolutionPath);

			var solutionPath = @"de4dot\de4dot.sln";
			solutionPath = Path.Combine(currentSolutionPath, @"..\..\..\RealSolutions", solutionPath);
			solutionPath = Path.GetFullPath(solutionPath);

			TestRealSolution(strategy, solutionPath);
		}

		[TestMethod]
		[TestCategory("Solutions")]
		[TestCategory("IncrementalAsync")]
		public void TestSolution1IncrementalOnDemandAsync()
		{
			BasicTests.TestSolution1Incremental(AnalysisStrategyKind.ONDEMAND_ASYNC);
		}

		public static void TestSolution1Incremental(AnalysisStrategyKind strategy)
		{
			string currentSolutionPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
			string solutionPath = Path.Combine(
				Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetParent(currentSolutionPath).FullName).FullName).FullName).FullName,
				ConfigurationManager.AppSettings["TestIncremental"]);
			TestSolutionIncremental1(strategy, solutionPath);
		}

        public static void TestSolution1Share(AnalysisStrategyKind strategy)
        {
            string currentSolutionPath = @"\\t-digarb-z440\share\solutions";
            Assert.IsTrue(Directory.Exists(currentSolutionPath));
            string solutionPath = Path.Combine(currentSolutionPath, ConfigurationManager.AppSettings["Solution1"]);

            TestSolution1(strategy, solutionPath);
        }

        private static void TestSolution1(AnalysisStrategyKind strategy, string solutionPath)
        {
            AnalyzeSolution(solutionPath, (s, callgraph) =>
            {
                //callgraph.Save("solution1.dot");
                Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "Test", "ConsoleApplication1"), "CallBar"), callgraph)); // ConsoleApplication1
                // Fails is I use only Contains with hascode!
                Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ClassLibrary1", "RemoteClass1", "ClassLibrary1"), "Bar", false), callgraph)); // ClassLibrary
                Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "LocalClass2", "ConsoleApplication1"), "Bar"), callgraph)); // ConsoleApplication1
                Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "Test", "ConsoleApplication1"), "CallBar"), callgraph)); // ConsoleApplication1
            }, strategy);
        }

		private static void TestRealSolution(AnalysisStrategyKind strategy, string solutionPath)
		{
			AnalyzeSolution(solutionPath, (s, callgraph) =>
			{
				var reachableMethods = callgraph.GetReachableMethods();
				Assert.IsTrue(reachableMethods.Count > 0);
			}, strategy);
		}

		private static IEnumerable<string> CopyFiles(string sourceFolder, string destinationFolder)
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

		private static void TestSolutionIncremental1(AnalysisStrategyKind strategy, string solutionPath)
		{
			var baseFolder = Path.GetDirectoryName(solutionPath);
			var testRootFolder = Directory.GetParent(baseFolder).FullName;
			var currentFolder= Path.Combine(testRootFolder, "current");
			var changesFolder = Path.Combine(testRootFolder, "changes");

			solutionPath = solutionPath.Replace(baseFolder, currentFolder);

			CopyFiles(baseFolder, currentFolder);

			AnalyzeSolution(solutionPath,
				(s, callgraph) =>
				{
					//callgraph.Save("solution1.dot");
					Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "Test", "ConsoleApplication1"), "CallBar"), callgraph)); // ConsoleApplication1
																																										// Fails is I use only Contains with hascode!
					Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ClassLibrary1", "RemoteClass1", "ClassLibrary1"), "Bar", false), callgraph)); // ClassLibrary
					Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "LocalClass2", "ConsoleApplication1"), "Bar"), callgraph)); // ConsoleApplication1
					Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "Test", "ConsoleApplication1"), "CallBar"), callgraph)); // ConsoleApplication1
                    Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "Test3", "ConsoleApplication1"), "DoTest"), callgraph)); // ConsoleApplication1
					Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "Test", "ConsoleApplication1"), "DoTest"), callgraph));
                },
				(s) =>
				{
					var modifications = CopyFiles(changesFolder, currentFolder);

					s.ApplyModificationsAsync(modifications).Wait();
				},
				(s, callgraph) =>
				{
					Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "Test4", "ConsoleApplication1"), "DoTest"), callgraph)); 

					Assert.IsFalse(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "Test", "ConsoleApplication1"), "DoTest"), callgraph));
                },
				strategy);
		}

		[TestMethod]
        [TestCategory("Solutions")]
        public void TestPrecisionVsFindReferences()
        {
            string currentSolutionPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string solutionPath = Path.Combine(
                Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetParent(currentSolutionPath).FullName).FullName).FullName).FullName,
                ConfigurationManager.AppSettings["Solution1"]);
            //string solutionPath = ConfigurationManager.AppSettings["TestSolutionsPath"]+  ConfigurationManager.AppSettings["Solution1"];
            CompareWithRoslyn(solutionPath);
        }
        [TestMethod]
        [TestCategory("Solutions")]
        public void TestPrecisionVsFindReferences2()
        {
            string solutionPath = @"C:\Users\t-digarb\Source\Workspaces\VSO\RosInt VSO\ReachingTypeAnalysis\ReachingTypeAnalysis.sln";
            Assert.IsTrue(Directory.Exists(solutionPath));
            CompareWithRoslyn(solutionPath);
        }

        [TestMethod]
        [TestCategory("Solutions")]
        public void TestPrecisionVsFindReferences3()
        {
            string solutionPath = @"C:\Users\t-digarb\Source\gillopy\Source\Gillopy.sln";
            Assert.IsTrue(Directory.Exists(solutionPath));

            //string solutionPath = @"C:\Users\t-digarb\Source\Repos\contractor-net\Contractor.sln";
            CompareWithRoslyn(solutionPath);
        }

        public void CompareWithRoslyn(string solutionPath)
        {
            AnalyzeSolution(solutionPath, (s, callgraph) =>
            {
                callgraph.Save("solution1.dot");
				s.CompareWithRoslynFindReferences(solutionPath + ".txt");
            });
        }
        
    }
}
