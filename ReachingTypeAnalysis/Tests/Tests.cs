// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReachingTypeAnalysis.Communication;
using ReachingTypeAnalysis.Roslyn;
using SolutionTraversal.CallGraph;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace ReachingTypeAnalysis
{
    [TestClass]
    public partial class BasicTests
    {
        delegate void RunChecks(SolutionAnalyzer s, CallGraph<MethodDescriptor, LocationDescriptor> callgraph);

        private static void AnalyzeExample(string source, RunChecks checker, AnalysisStrategyKind strategy = AnalysisStrategyKind.NONE)
        {
            //var solution = ReachingTypeAnalysis.Utils.CreateSolution(source);
            //var solAnalyzer = new SolutionAnalyzer(solution);
            var solAnalyzer = SolutionAnalyzer.CreateFromSource(source);
            var callgraph = solAnalyzer.Analyze(strategy);

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

		private static void AnalizeSolution(string solutionPath, RunChecks checker, AnalysisStrategyKind strategy = AnalysisStrategyKind.NONE)
        {
            var solAnalyzer = SolutionAnalyzer.CreateFromSolution(solutionPath);
            var callgraph = solAnalyzer.Analyze(strategy);
            
            checker(solAnalyzer, callgraph);
        }

		private static void AnalizeSolution(string solutionPath, RunChecks initialChecker, Action<SolutionAnalyzer> updates, RunChecks updatesChecker, AnalysisStrategyKind strategy = AnalysisStrategyKind.NONE)
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
        public void TestSolution1ShareOnDemandAsync()
        {
            BasicTests.TestSolution1Share(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }
		
        public static void TestSolution1Local(AnalysisStrategyKind strategy)
        {
            string currentSolutionPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string solutionPath = Path.Combine(
                Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetParent(currentSolutionPath).FullName).FullName).FullName).FullName, 
                ConfigurationManager.AppSettings["Solution1"]);
            //string solutionPath = ConfigurationManager.AppSettings["TestSolutionsPath"]+  ConfigurationManager.AppSettings["Solution1"];
            TestSolution1(strategy, solutionPath);
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
            string solutionPath = Path.Combine(currentSolutionPath, ConfigurationManager.AppSettings["Solution1"]);

            TestSolution1(strategy, solutionPath);
        }

        private static void TestSolution1(AnalysisStrategyKind strategy, string solutionPath)
        {
            AnalizeSolution(solutionPath, (s, callgraph) =>
            {
                //callgraph.Save("solution1.dot");
                Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "Test", "ConsoleApplication1"), "CallBar"), callgraph)); // ConsoleApplication1
                // Fails is I use only Contains with hascode!
                Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ClassLibrary1", "RemoteClass1", "ClassLibrary1"), "Bar", false), callgraph)); // ClassLibrary
                Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "LocalClass2", "ConsoleApplication1"), "Bar"), callgraph)); // ConsoleApplication1
                Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "Test", "ConsoleApplication1"), "CallBar"), callgraph)); // ConsoleApplication1
            }, strategy);
        }



		private static void TestSolutionIncremental1(AnalysisStrategyKind strategy, string solutionPath)
		{
			var baseFolder = Path.GetDirectoryName(solutionPath);
			var testRootFolder = Directory.GetParent(baseFolder).FullName;
			var currentFolder= Path.Combine(testRootFolder, "current");
			var changesFolder = Path.Combine(testRootFolder, "changes");

			solutionPath = solutionPath.Replace(baseFolder, currentFolder);

			foreach (var fileName in Directory.EnumerateFiles(baseFolder, "*", SearchOption.AllDirectories))
			{
				var newPathForFile = fileName.Replace(baseFolder,currentFolder);
				var targetPath = Path.GetDirectoryName(newPathForFile);
				if (!System.IO.Directory.Exists(targetPath))
				{
					System.IO.Directory.CreateDirectory(targetPath);
				}

				File.Copy(fileName, newPathForFile, true);
			}

			AnalizeSolution(solutionPath,
				(s, callgraph) =>
				{
					//callgraph.Save("solution1.dot");
					Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "Test", "ConsoleApplication1"), "CallBar"), callgraph)); // ConsoleApplication1
																																										// Fails is I use only Contains with hascode!
					Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ClassLibrary1", "RemoteClass1", "ClassLibrary1"), "Bar", false), callgraph)); // ClassLibrary
					Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "LocalClass2", "ConsoleApplication1"), "Bar"), callgraph)); // ConsoleApplication1
					Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "Test", "ConsoleApplication1"), "CallBar"), callgraph)); // ConsoleApplication1
				},
				(s) =>
				{
					var modifications = new List<string>();
					foreach (var fileName in Directory.EnumerateFiles(changesFolder, "*.cs", SearchOption.AllDirectories))
					{
						var newPathForFile = fileName.Replace(changesFolder, currentFolder);
						var targetPath = Path.GetDirectoryName(newPathForFile);
						if (!System.IO.Directory.Exists(targetPath))
						{
							System.IO.Directory.CreateDirectory(targetPath);
						}

						modifications.Add(newPathForFile);
						File.Copy(fileName, newPathForFile, true);
					}

					s.ApplyModificationsAsync(modifications).Wait();
				},
				(s, callgraph) =>
				{
					
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
            CompareWithRoslyn(solutionPath);
        }

        [TestMethod]
        [TestCategory("Solutions")]
        public void TestPrecisionVsFindReferences3()
        {
            string solutionPath = @"C:\Users\t-digarb\Source\gillopy\Source\Gillopy.sln";
            //string solutionPath = @"C:\Users\t-digarb\Source\Repos\contractor-net\Contractor.sln";
            CompareWithRoslyn(solutionPath);
        }

        public void CompareWithRoslyn(string solutionPath)
        {
            AnalizeSolution(solutionPath, (s, callgraph) =>
            {
                callgraph.Save("solution1.dot");
				s.CompareWithRoslynFindReferences(solutionPath + ".txt");
            });
        }

        private static void CompareExample(string source)
        {
            var analyzerLocal = SolutionAnalyzer.CreateFromSource(source);
            var callgraphLocal = analyzerLocal.Analyze(AnalysisStrategyKind.ONDEMAND_SYNC);

            var analyzerParallel = SolutionAnalyzer.CreateFromSource(source);
            var queueingDispatcher = new QueueingDispatcher();
            var callgraphQueuing = analyzerParallel.Analyze(AnalysisStrategyKind.ENTIRE_ASYNC);

            var localReachable = callgraphLocal.GetReachableMethods().Count;
            var queuingReachable = callgraphQueuing.GetReachableMethods().Count;

            Assert.IsTrue(localReachable == queuingReachable);
            Assert.IsTrue(callgraphLocal.GetEdges().Count() == callgraphQueuing.GetEdges().Count());
        }

        //[TestMethod]
        public void CompareRemoveAlloc()
        {
			#region source code
			var source = @"
public class D
{
    public D(){}
    public C f;
    public void m2(C b)
    {
        f = b;
    }
}

public class C 
{
    public C(){}
    public C f;
    public C m1(C a)
    {
         f = a;
         return a;
    }
    public virtual void m2(C b)
    {
    }
}
class Program
{

    public static void Main()
    {
        D d = new D();
        C c = new C();
        d.f  = c.m1(c);
        c.f   = c; 
        d.m2(c);

    }
}";
			#endregion

			CompareExample(source);
        }
    }
}
