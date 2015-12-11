using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using ReachingTypeAnalysis.Analysis;
using Orleans.TestingHost;

namespace ReachingTypeAnalysis
{
    [TestClass]
    public partial class QueryTestsOnDemandAsync 
    {
		[TestMethod]
		[TestCategory("Query")]
		public void TestQueryAsync1()
		{
			BasicQueryTests.TestQuery1(AnalysisStrategyKind.ONDEMAND_ASYNC);
		}

		[TestMethod]
		[TestCategory("Query")]
		public void TestQueryAsync2()
        {
			BasicQueryTests.TestQuery2(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }
    }

    [TestClass]
    public partial class QueryTestsOrleans : TestingSiloHost
    {
		[TestMethod]
		[TestCategory("Query")]
		public void TestQueryOrleans1()
        {
			BasicQueryTests.TestQuery1(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
		[TestCategory("Query")]
		public void TestQueryOrleans2()
        {
			BasicQueryTests.TestQuery2(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }
    }

    public class BasicQueryTests
    {
        public static void TestQuery1(AnalysisStrategyKind strategy)
        {
            #region source code
            var source = @"    class C
    {
        public static void N0()
        {
            N7();
            N4();
            N0();
            N2();
            N6();
            N8();
            N3();
        }

        public static void N1()
        {
            N6();
            N4();
            N5();
            N2();
        }

        public static void N2()
        {
            N5();
            N0();
            N8();
            N1();
            N6();
        }

        public static void N3()
        {
            N3();
        }

        public static void N4()
        {
            N8();
            N4();
            N5();
            N6();
            N3();
        }

        public static void N5()
        {
            N6();
            N4();
            N1();
        }

        public static void N6()
        {
            N2();
            N0();
            N4();
            N1();
        }

        public static void N7()
        {
            N5();
            N2();
            N4();
            N3();
            N6();
            N7();
            N0();
        }

        public static void N8()
        {
            N0();
            N1();
            N7();
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
    }";
			#endregion

			TestUtils.AnalyzeExample(source, (s, callgraph) =>
            {

                var queryTimes2 = Q.DoQueries1(s.SolutionManager);
                var averageQueryTimes2 = queryTimes2.Average(t => t.Elapsed.Milliseconds);
                Debug.WriteLine("Average Query Time {0} ", averageQueryTimes2);
            }, strategy);
        }

        public static void TestQuery2(AnalysisStrategyKind strategy)
        {
            #region source code
            var source = @"    class C
    {
        public static void N0()
        {
            N7();
            N4();
            N0();
            N2();
            N6();
            N8();
            N3();
        }

        public static void N1()
        {
            N6();
            N4();
            N5();
            N2();
        }

        public static void N2()
        {
            N5();
            N0();
            N8();
            N1();
            N6();
        }

        public static void N3()
        {
            N3();
        }

        public static void N4()
        {
            N8();
            N4();
            N5();
            N6();
            N3();
        }

        public static void N5()
        {
            N6();
            N4();
            N1();
        }

        public static void N6()
        {
            N2();
            N0();
            N4();
            N1();
        }

        public static void N7()
        {
            N5();
            N2();
            N4();
            N3();
            N6();
            N7();
            N0();
        }

        public static void N8()
        {
            N0();
            N1();
            N7();
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
    }";
            #endregion

            TestUtils.AnalyzeExample(source, (s, callgraph) =>
            {
                var queryTimes2 = Q.DoQueries2(s.SolutionManager);
                var averageQueryTimes2 = queryTimes2.Average(t => t.Elapsed.Milliseconds);
                Debug.WriteLine("Average Query Time {0} ", averageQueryTimes2);
            }, strategy);
        }
    }

    public static class Q
    {
        public static IEnumerable<Stopwatch> DoQueries1(ISolutionManager solutionManager)
        {
            {
                var sw = Stopwatch.StartNew();
                var a = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "N0", true), 4).Result;
                sw.Stop();
                yield return sw;
            }

            {
                var sw = Stopwatch.StartNew();
                var a = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "N1", true), 2).Result;
                sw.Stop();
                yield return sw;
            }

            {
                var sw = Stopwatch.StartNew();
                var a = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "N2", true), 2).Result;
                sw.Stop();
                yield return sw;
            }

            {
                var sw = Stopwatch.StartNew();
                var a = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "N3", true), 1).Result;
                sw.Stop();
                yield return sw;
            }

            {
                var sw = Stopwatch.StartNew();
                var a = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "N4", true), 4).Result;
                sw.Stop();
                yield return sw;
            }

            {
                var sw = Stopwatch.StartNew();
                var a = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "N5", true), 2).Result;
                sw.Stop();
                yield return sw;
            }

            {
                var sw = Stopwatch.StartNew();
                var a = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "N6", true), 4).Result;
                sw.Stop();
                yield return sw;
            }

            {
                var sw = Stopwatch.StartNew();
                var a = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "N7", true), 1).Result;
                sw.Stop();
                yield return sw;
            }

            {
                var sw = Stopwatch.StartNew();
                var a = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "N8", true), 3).Result;
                sw.Stop();
                yield return sw;
            }

			//{
			//    var sw = Stopwatch.StartNew();
			//    var a = CallGraphQueryInterface.GetCalleesOrleansAsync(solutionManager, new MethodDescriptor("C", "N9", true), 1, TestConstants.TestProjectName).Result;
			//    sw.Stop();
			//    yield return sw;
			//}

			{
				var sw = Stopwatch.StartNew();
                var a = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "Main", true), 8).Result;
                sw.Stop();
                yield return sw;
            }
        }

        public static IEnumerable<Stopwatch> DoQueries2(ISolutionManager solutionManager)
        {
            {
                var sw = Stopwatch.StartNew();
                var _ = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "N0", true), 2, TestConstants.ProjectName).Result;
                sw.Stop();
                yield return sw;
            }

            {
                var sw = Stopwatch.StartNew();
                var _ = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "N1", true), 2, TestConstants.ProjectName).Result;
                sw.Stop();
                yield return sw;
            }

            {
                var sw = Stopwatch.StartNew();
                var _ = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "N2", true), 1, TestConstants.ProjectName).Result;
                sw.Stop();
                yield return sw;
            }

            {
                var sw = Stopwatch.StartNew();
                var _ = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "N3", true), 1, TestConstants.ProjectName).Result;
                sw.Stop();
                yield return sw;
            }

            {
                var sw = Stopwatch.StartNew();
                var _ = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "N4", true), 2, TestConstants.ProjectName).Result;
                sw.Stop();
                yield return sw;
            }

            {
                var sw = Stopwatch.StartNew();
                var _ = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "N5", true), 1, TestConstants.ProjectName).Result;
                sw.Stop();
                yield return sw;
            }

            {
                var sw = Stopwatch.StartNew();
                var _ = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "N6", true), 2, TestConstants.ProjectName).Result;
                sw.Stop();
                yield return sw;
            }

            {
                var sw = Stopwatch.StartNew();
                var _ = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "N7", true), 6, TestConstants.ProjectName).Result;
                sw.Stop();
                yield return sw;
            }

            {
                var sw = Stopwatch.StartNew();
                var _ = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "N8", true), 1, TestConstants.ProjectName).Result;
                sw.Stop();
                yield return sw;
            }

			//{
			//    var sw = Stopwatch.StartNew();
			//    var _ = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "N9", true), 1, TestConstants.TestProjectName).Result;
			//    sw.Stop();
			//    yield return sw;
			//}

			{
				var sw = Stopwatch.StartNew();
                var _ = CallGraphQueryInterface.GetCalleesAsync(solutionManager, new MethodDescriptor("C", "Main", true), 9, TestConstants.ProjectName).Result;
                sw.Stop();
                yield return sw;
            }
        }
    }
}
