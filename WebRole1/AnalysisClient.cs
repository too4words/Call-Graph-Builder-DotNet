using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orleans.Runtime.Host;
using ReachingTypeAnalysis;
using SolutionTraversal.CallGraph;
using System.Threading.Tasks;

namespace WebRole1
{
    public class AnalysisClient
    {
        public async Task<CallGraph<MethodDescriptor, LocationDescriptor>> AnalyzeSolutionAsync(string solutionFileName)
        {
            var analyzer = SolutionAnalyzer.CreateFromSolution(solutionFileName);
            var callgraph = await analyzer.AnalyzeAsync(AnalysisStrategyKind.ONDEMAND_ORLEANS);
            return callgraph;
        }
        public async Task<CallGraph<MethodDescriptor, LocationDescriptor>> AnalyzeSourceCodeAsync(string source)
        {
            var analyzer = SolutionAnalyzer.CreateFromSource(source);
            var callgraph = await analyzer.AnalyzeAsync(AnalysisStrategyKind.ONDEMAND_ORLEANS);
            return callgraph;
        }

        public async Task<string> RunSingleTestAsync(string testFullName, int iterations)
        {
            var index = testFullName.LastIndexOf('.');
            var testClass = testFullName.Substring(0, index);
            var testMethod = testFullName.Substring(index + 1);

            return await this.RunSingleTestAsync(testClass, testMethod, iterations);
        }

        public async Task<string> RunSingleTestAsync(string testClass, string testMethod, int iterations)
        {
            try
            {
                Console.WriteLine("Executing {0}", testMethod);
                var minTime = long.MaxValue;
                var maxTime = 0L;
                var acumTime = 0D;

                for (var i = 0; i < iterations; i++)
                {
                    Console.WriteLine("Iteration {0}", i);
                    //var watch = new Stopwatch();
                    var testType = Type.GetType(testClass + ", ReachingTypeAnalysis");
                    var test = Activator.CreateInstance(testType);
                    var methodToExecute = test.GetType().GetMethod(testMethod);

                    //watch.Start();
                    methodToExecute.Invoke(test, new object[1] {AnalysisStrategyKind.ONDEMAND_ORLEANS });
                    //watch.Stop();

                    //var time = watch.ElapsedMilliseconds;
                    //if (time > maxTime) maxTime = time;
                    //if (time < minTime) minTime = time;

                    //acumTime += time;
                }

                //var avgTime = acumTime / iterations;
                //outputWriter.WriteLine("{3}, {0}, {1}, {2} {4]", avgTime, maxTime, minTime, testMethod, SolutionAnalyzer.MessageCounter);
                //outputWriter.Flush();
                return "Done";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return "Done";

        }


    }
}