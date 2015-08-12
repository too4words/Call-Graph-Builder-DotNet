﻿using System;
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

		public async Task<CallGraph<MethodDescriptor, LocationDescriptor>> AnalyzeTestAsync(string testFullName)
        {
            var source = BasicTests.Test[testFullName];
			var analyzer = SolutionAnalyzer.CreateFromSource(source);
			var callgraph = await analyzer.AnalyzeAsync(AnalysisStrategyKind.ONDEMAND_ORLEANS);
			return callgraph;
        }
    }
}