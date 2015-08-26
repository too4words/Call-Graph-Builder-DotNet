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
        delegate void RunChecks(SolutionAnalyzerSync s, CallGraph<MethodDescriptor, LocationDescriptor> callgraph);

        private static void AnalyzeExample(string source, RunChecks checker, AnalysisStrategyKind strategy = AnalysisStrategyKind.NONE)
        {
            //var solution = ReachingTypeAnalysis.Utils.CreateSolution(source);
            //var solAnalyzer = new SolutionAnalyzerSync(solution);
            var solAnalyzer = SolutionAnalyzerSync.CreateFromSource(source);
            var callgraph = solAnalyzer.Analyze(strategy);

            checker(solAnalyzer, callgraph);
        }
				
		private static void AnalizeSolution(string solutionPath, RunChecks checker, AnalysisStrategyKind strategy = AnalysisStrategyKind.NONE)
        {
            var solAnalyzer = SolutionAnalyzerSync.CreateFromSolution(solutionPath);
            var callgraph = solAnalyzer.Analyze(strategy);
            
            checker(solAnalyzer, callgraph);
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


		        
        private static void CompareExample(string source)
        {
            var analyzerLocal = SolutionAnalyzerSync.CreateFromSource(source);
            var callgraphLocal = analyzerLocal.Analyze(AnalysisStrategyKind.ONDEMAND_SYNC);

            var analyzerParallel = SolutionAnalyzerSync.CreateFromSource(source);
            var queueingDispatcher = new QueueingDispatcher();
            var callgraphQueuing = analyzerParallel.Analyze(AnalysisStrategyKind.ENTIRE_SYNC);

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
