// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReachingTypeAnalysis.Communication;
using ReachingTypeAnalysis.Roslyn;
using SolutionTraversal.Callgraph;
using System;
using System.Configuration;
using System.IO;
using System.Linq;

namespace ReachingTypeAnalysis
{
    [TestClass]
    public partial class Tests
    {
        delegate void RunChecks(SolutionAnalyzer s, CallGraph<MethodDescriptor, LocationDescriptor> callgraph);

        private static void AnalyzeExample(string source, RunChecks checker, AnalysisStrategyKind strategy = AnalysisStrategyKind.NONE)
        {
            //var solution = ReachingTypeAnalysis.Utils.CreateSolution(source);
            //var solAnalyzer = new SolutionAnalyzer(solution);
            var solAnalyzer = new SolutionAnalyzer(source);
            var callgraph = solAnalyzer.Analyze(strategy, true);

            checker(solAnalyzer, callgraph);
        }

        private static void AnalizeSolution(Solution solution, RunChecks checker, AnalysisStrategyKind type = AnalysisStrategyKind.NONE)
        {
            var solAnalyzer = new SolutionAnalyzer(solution);
            var callgraph = solAnalyzer.Analyze(type, true);
            
            checker(solAnalyzer, callgraph);
        }

//        [TestMethod]
//        [TestCategory("Solutions")]
//        public void TestSolution1()
//        {
//            string currentSolutionPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
//            string solutionPath = Path.Combine(
//                Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetParent(currentSolutionPath).FullName).FullName).FullName).FullName, 
//                ConfigurationManager.AppSettings["Solution1"]);
//            //string solutionPath = ConfigurationManager.AppSettings["TestSolutionsPath"]+  ConfigurationManager.AppSettings["Solution1"];
//            var solution = Utils.ReadSolution(solutionPath);
//            AnalizeSolution(solution, (s, callgraph) =>
//            {
//                //callgraph.Save("solution1.dot");
//                Assert.IsTrue(s.IsReachable(new MethodDescriptor("Test", "CallBar"), callgraph)); // ConsoleApplication1
//                // Fails is I use only Contains with hascode!
//                Assert.IsTrue(s.IsReachable(new MethodDescriptor("ClassLibrary1", "RemoteClass1", "Bar",false), callgraph)); // ClassLibrary
//                Assert.IsTrue(s.IsReachable(new MethodDescriptor("LocalClass2", "Bar"), callgraph)); // ConsoleApplication1

//                var roslynMethod = RoslynSymbolFactory.FindMethodSymbolAndProjectInSolution(solution, new MethodDescriptor("Test", "CallBar")).Method;
//                Assert.IsTrue(roslynMethod != null);
//                Assert.IsTrue(s.IsReachable(Utils.CreateMethodDescriptor(roslynMethod), callgraph)); // ConsoleApplication1
//            });
//        }

//        [TestMethod]
//        [TestCategory("Solutions")]
//        public void TestPrecisionVsFindReferences()
//        {
//            string currentSolutionPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
//            string solutionPath = Path.Combine(
//                Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetParent(currentSolutionPath).FullName).FullName).FullName).FullName,
//                ConfigurationManager.AppSettings["Solution1"]);
//            //string solutionPath = ConfigurationManager.AppSettings["TestSolutionsPath"]+  ConfigurationManager.AppSettings["Solution1"];
//            CompareWithRoslyn(solutionPath);
//        }
//        [TestMethod]
//        [TestCategory("Solutions")]
//        public void TestPrecisionVsFindReferences2()
//        {
//            string solutionPath = @"C:\Users\t-digarb\Source\Workspaces\VSO\RosInt VSO\ReachingTypeAnalysis\ReachingTypeAnalysis.sln";
//            CompareWithRoslyn(solutionPath);
//        }

//        [TestMethod]
//        [TestCategory("Solutions")]
//        public void TestPrecisionVsFindReferences3()
//        {
//            string solutionPath = @"C:\Users\t-digarb\Source\gillopy\Source\Gillopy.sln";
//            //string solutionPath = @"C:\Users\t-digarb\Source\Repos\contractor-net\Contractor.sln";
//            CompareWithRoslyn(solutionPath);
//        }

//        public void CompareWithRoslyn(string solutionPath)
//        {
//            var solution = Utils.ReadSolution(solutionPath);
//            AnalizeSolution(solution, (s, callgraph) =>
//            {
//                callgraph.Save("solution1.dot");
//                s.CompareWithRoslynFindReferences(solution, solutionPath + ".txt");
//            });
//        }


//        private static void CompareExample(string source)
//        {
//            var solution = ReachingTypeAnalysis.Utils.CreateSolution(source);
//            var analyzerLocal = new SolutionAnalyzer(solution);
//            var callgraphLocal = analyzerLocal.Analyze(AnalysisStrategyKind.ONDEMAND_SYNC, true);

//            var analyzerParallel = new SolutionAnalyzer(solution);
//            var queueingDispatcher = new QueueingDispatcher();
//            var callgraphQueuing = analyzerParallel.Analyze(AnalysisStrategyKind.ENTIRE_ASYNC, true);

//            var localReachable = callgraphLocal.GetReachableMethods().Count;
//            var queuingReachable = callgraphQueuing.GetReachableMethods().Count;

//            Assert.IsTrue(localReachable == queuingReachable);
//            Assert.IsTrue(callgraphLocal.GetEdges().Count() == callgraphQueuing.GetEdges().Count());
//        }

//        //[TestMethod]
//        public void CompareRemoveAlloc()
//        {
//            var source = @"
//public class D
//{
//    public D(){}
//    public C f;
//    public void m2(C b)
//    {
//        f = b;
//    }
//}
//
//public class C 
//{
//    public C(){}
//    public C f;
//    public C m1(C a)
//    {
//         f = a;
//         return a;
//    }
//    public virtual void m2(C b)
//    {
//    }
//}
//class Program
//{
//
//    public static void Main()
//    {
//        D d = new D();
//        C c = new C();
//        d.f  = c.m1(c);
//        c.f   = c; 
//        d.m2(c);
//
//    }
//}";
//            CompareExample(source);
//        }
    }
}
