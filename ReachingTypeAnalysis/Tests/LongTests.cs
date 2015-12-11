// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using TestSources;
using System;
using System.Reflection;
using System.IO;
using System.IO.Compression;

namespace ReachingTypeAnalysis
{
	public class LongTests
    {
        [TestMethod]
        [TestCategory("Generated")]
        public void LongGeneratedTestSync1()
		{
			LongGeneratedTestAsync1(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Generated")]
        public void LongGeneratedTestSync2()
		{
			LongGeneratedTestAsync2(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Generated")]
        public void LongGeneratedTestSync3()
		{
			LongGeneratedTestAsync3(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("VeryLongRunning")]
        public void LongGeneratedTestSync4()
		{
			LongGeneratedTestAsync4(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        ////////////////////////////////////////////////////////////////////////

		[TestMethod]
		[TestCategory("Generated")]
		public void LongGeneratedTestDemandAsync1()
		{
			LongGeneratedTestAsync1(AnalysisStrategyKind.ONDEMAND_ASYNC);
		}
		
		[TestMethod]
        [TestCategory("Generated")]
        public void LongGeneratedTestDemandAsync2()
        {
            LongGeneratedTestAsync2(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Generated")]
        public void LongGeneratedTestDemandAsync3()
        {
            LongGeneratedTestAsync3(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("VeryLongRunning")]
        public void LongGeneratedTestDemandAsync4()
        {
            LongGeneratedTestAsync4(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("VeryLongRunning")]

        public void TestSynthetic_100()
        {
            TestSyntheticSolution(
                Path.Combine("Tests", "synthetic-100.zip"), 
                100, 585,
                AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("VeryLongRunning")]

        public void TestSynthetic_1_000()
        {
            TestSyntheticSolution(
                Path.Combine("Tests", "synthetic-1000.zip"), 
                1001, 5992,
                AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("VeryLongRunning")]

        public void TestSynthetic_10_000()
        {
            TestSyntheticSolution(
                Path.Combine("Tests", "synthetic-10000.zip"), 
                10001, 37545,
                AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("VeryLongRunning")]

        public void TestSynthetic_100_000()
        {
            TestSyntheticSolution(
                Path.Combine("Tests", "synthetic-100000.zip"), 
                100001, 585,
                AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("VeryLongRunning")]

        public void TestSynthetic_500_000()
        {
            TestSyntheticSolution(
                Path.Combine("Tests", "synthetic-500000.zip"), 
                100001, 585,
                AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("VeryLongRunning")]

        public void TestSynthetic_10_000_000()
        {
            TestSyntheticSolution(
                Path.Combine("Tests", "synthetic-1000000.zip"), 
                100001, 585,
                AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        private static void TestSyntheticSolution(string zipFileName, 
            int correctNodeCount, int correctEdgeCount,
            AnalysisStrategyKind strategy)
        {
            Assert.IsTrue(File.Exists(zipFileName));
            var destinationDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Assert.IsTrue(!Directory.Exists(destinationDirectory));
            Directory.CreateDirectory(destinationDirectory);
            ZipFile.ExtractToDirectory(zipFileName, destinationDirectory);
            Assert.IsTrue(Directory.Exists(destinationDirectory));
            var solutionPath = Path.Combine(destinationDirectory, TestConstants.SolutionPath);
            Assert.IsTrue(File.Exists(solutionPath));

			TestUtils.AnalyzeSolution(solutionPath, (s, callgraph) =>
            {
                //var reachable = s.GetReachableMethods(callgraph);
                //Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "Main", true)));
                //Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N0", true)));
                //Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N1", true)));
                //Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N2", true)));
                //Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N3", true)));
                //Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N4", true)));
                //Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N5", true)));
                //Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N6", true)));
                //Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N7", true)));
                //Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N8", true)));
                //Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N9", true)));

                //Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N8", true), new MethodDescriptor("C", "N4", true)));
                //Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N8", true), new MethodDescriptor("C", "N8", true)));
                //Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N8", true), new MethodDescriptor("C", "N7", true)));
                //Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N8", true), new MethodDescriptor("C", "N0", true)));

                //Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N6", true)));
                //Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N2", true)));
                //Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N3", true)));
                //Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N7", true)));
                //Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N4", true)));

                var nodeCount = callgraph.GetNodes().Count();
				Assert.IsTrue(nodeCount == correctNodeCount,
                    string.Format("Found {0} nodes instead of {1}", 
                   nodeCount, correctNodeCount));
                var edgeCount = callgraph.GetEdges().Count();
                
               Assert.IsTrue(edgeCount == correctEdgeCount, 
                   string.Format("Found {0} edges instead of {1}", 
                   edgeCount, correctEdgeCount));
            }, strategy);
        }

        ////////////////////////////////////////////////////////////////////////

        public static void LongGeneratedTestAsync1(AnalysisStrategyKind strategy)
		{
			#region Test Source Code
			var source = @"
class C
{
    public static void N0()
    {
        N2();
        N7();
        N1();
        N8();
    }

    public static void N1()
    {
        N8();
        N1();
        N3();
        N5();
        N2();
    }

    public static void N2()
    {
        N6();
        N0();
        N3();
    }

    public static void N3()
    {
        N3();
        N0();
        N8();
        N6();
    }

    public static void N4()
    {
        N3();
        N7();
        N4();
        N5();
    }

    public static void N5()
    {
        N6();
        N2();
        N3();
        N7();
        N4();
    }

    public static void N6()
    {
        N1();
    }

    public static void N7()
    {
        N2();
        N7();
        N8();
        N3();
        N4();
    }

    public static void N8()
    {
        N4();
        N8();
        N7();
        N0();
    }

    public static void N9()
    {
    }

    public static void Main()
    {
        N0();
        N1();
        N2();
        N3();
        N4();
        N5();
        N6();
        N7();
        N8();
        N9();
        Main();
    }
}
";
			#endregion

			TestUtils.AnalyzeExample(source, (s, callgraph) =>
			{
				Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "Main", true), callgraph));
				Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "N0", true), callgraph));
				Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "N1", true), callgraph));
				Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "N2", true), callgraph));
				Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "N3", true), callgraph));
				Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "N4", true), callgraph));
				Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "N5", true), callgraph));
				Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "N6", true), callgraph));
				Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "N7", true), callgraph));
				Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "N8", true), callgraph));
				Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "N9", true), callgraph));

				Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N8", true), new MethodDescriptor("C", "N4", true), callgraph));
				Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N8", true), new MethodDescriptor("C", "N8", true), callgraph));
				Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N8", true), new MethodDescriptor("C", "N7", true), callgraph));
				Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N8", true), new MethodDescriptor("C", "N0", true), callgraph));

				Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N6", true), callgraph));
				Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N2", true), callgraph));
				Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N3", true), callgraph));
				Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N7", true), callgraph));
				Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N4", true), callgraph));

				Assert.IsTrue(callgraph.GetNodes().Count() == 11);
				var edgeCount = callgraph.GetEdges().Count();
				//s.Callgraph.Save(@"c:\temp\test.dot");
				Assert.IsTrue(edgeCount == 46, string.Format("Found {0} edges", edgeCount));
			}, strategy);
		}

		public static void LongGeneratedTestAsync2(AnalysisStrategyKind strategy)
		{
            var source = BasicTestsSources.Test["LongGeneratedTest2"];

			TestUtils.AnalyzeExample(source, (s, callgraph) =>
            {
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "Main", true), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "N0", true), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "N1", true), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "N2", true), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "N3", true), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "N4", true), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "N5", true), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "N6", true), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "N7", true), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "N8", true), callgraph));
                Assert.IsTrue(s.IsReachable(new MethodDescriptor("C", "N9", true), callgraph));

				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N8", true), new MethodDescriptor("C", "N4", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N8", true), new MethodDescriptor("C", "N8", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N8", true), new MethodDescriptor("C", "N7", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N8", true), new MethodDescriptor("C", "N0", true)));

				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N6", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N2", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N3", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N7", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N4", true)));

				Assert.IsTrue(callgraph.GetNodes().Count() == 101);
                var edgeCount = callgraph.GetEdges().Count();
                //s.Callgraph.Save(@"c:\temp\test.dot");
                Assert.IsTrue(edgeCount == 589, string.Format("Found {0} edges", edgeCount));
            }, strategy);
        }

		public static void LongGeneratedTestAsync3(AnalysisStrategyKind strategy)
		{
            var source = BasicTestsSources.Test["LongGeneratedTest3"];

			TestUtils.AnalyzeExample(source, (s, callgraph) =>
            {
                var reachable = s.GetReachableMethods(callgraph);
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "Main", true)));
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N0", true)));
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N1", true)));
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N2", true)));
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N3", true)));
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N4", true)));
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N5", true)));
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N6", true)));
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N7", true)));
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N8", true)));
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N9", true)));

				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N8", true), new MethodDescriptor("C", "N4", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N8", true), new MethodDescriptor("C", "N8", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N8", true), new MethodDescriptor("C", "N7", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N8", true), new MethodDescriptor("C", "N0", true)));

				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N6", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N2", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N3", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N7", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N4", true)));

				Assert.IsTrue(callgraph.GetNodes().Count() == 1001);
                var edgeCount = callgraph.GetEdges().Count();
                //s.Callgraph.Save(@"c:\temp\test.dot");
                Assert.IsTrue(edgeCount == 5990, string.Format("Found {0} edges", edgeCount));
            }, strategy);
        }

		public static void LongGeneratedTestAsync4(AnalysisStrategyKind strategy)
		{
            var source = BasicTestsSources.Test["LongGeneratedTest4"];

			TestUtils.AnalyzeExample(source, (s, callgraph) =>
            {
                var reachable = s.GetReachableMethods(callgraph);
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "Main", true)));
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N0", true)));
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N1", true)));
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N2", true)));
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N3", true)));
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N4", true)));
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N5", true)));
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N6", true)));
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N7", true)));
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N8", true)));
                Assert.IsTrue(reachable.Contains(new MethodDescriptor("C", "N9", true)));

				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N8", true), new MethodDescriptor("C", "N4", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N8", true), new MethodDescriptor("C", "N8", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N8", true), new MethodDescriptor("C", "N7", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N8", true), new MethodDescriptor("C", "N0", true)));

				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N6", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N2", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N3", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N7", true)));
				//Assert.IsTrue(s.IsCalled(new MethodDescriptor("C", "N5", true), new MethodDescriptor("C", "N4", true)));

				Assert.IsTrue(callgraph.GetNodes().Count() == 10001);
                var edgeCount = callgraph.GetEdges().Count();
                //s.Callgraph.Save(@"c:\temp\test.dot");
                Assert.IsTrue(edgeCount == 59989, string.Format("Found {0} edges", edgeCount));
            }, strategy);
        }
    }
}
