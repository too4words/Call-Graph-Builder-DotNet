using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orleans.Runtime.Host;
using ReachingTypeAnalysis;
using SolutionTraversal.CallGraph;

namespace WebRole1
{
    public class AnalysisClient
    {
        public CallGraph<MethodDescriptor, LocationDescriptor> Analyze(string solutionFileName)
        {
            var analyzer = SolutionAnalyzer.CreateFromSolution(solutionFileName);
            var callgraph = analyzer.Analyze(AnalysisStrategyKind.ONDEMAND_ORLEANS);
            return callgraph;
        }
    }
}