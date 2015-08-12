// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace ReachingTypeAnalysis
{
	public partial class BasicTests
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

			AnalyzeExample(source, (s, callgraph) =>
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
			var source = BasicTests.Test["LongGeneratedTest2"];

			AnalyzeExample(source, (s, callgraph) =>
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
			var source = BasicTests.Test["LongGeneratedTest3"];
			
            AnalyzeExample(source, (s, callgraph) =>
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
			var source = BasicTests.Test["LongGeneratedTest4"];

			AnalyzeExample(source, (s, callgraph) =>
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
