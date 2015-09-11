//---------------------------------------------------------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//---------------------------------------------------------------------------------------------------------------------

namespace WebAPI
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Web.Http;
	using System;
	using CodeGraphModel;
	using System.Threading.Tasks;
	using ReachingTypeAnalysis.Statistics;
	using ReachingTypeAnalysis;
	using Orleans;
	using System.IO;
	using OrleansInterfaces;

	public class ExperimentsController : ApiController
    {
		// http://localhost:49176/api/Experiments?testName=Hola&machines=1&numberOfMethods=2
		[HttpGet]
		public async Task<string> RunTestAsync(string testName, int machines, int numberOfMethods)
		{
			var result = string.Empty;

			try
			{
				var analyzer = SolutionAnalyzer.CreateFromTest(testName);
				var analysisClient = new AnalysisClient(analyzer, machines);
				var results = await analysisClient.RunExperiment(GrainClient.GrainFactory);

				result = string.Format("Ready for queries. Time: {0} ms", results.ElapsedTime);
			}
			catch (Exception exc)
			{
				while (exc is AggregateException) exc = exc.InnerException;
				result = "Error connecting to Orleans: " + exc + " at " + DateTime.Now;
			}

			return result;
		}

		[HttpGet]
		public async Task<string> AnalyzeSolutionAsync(string drive, string solutionPath, string solutionName, int machines)
		{
			var result = string.Empty;

			try
			{
				solutionPath = Path.Combine(drive + ":\\" + solutionPath, solutionName + ".sln");
				var analyzer = SolutionAnalyzer.CreateFromSolution(solutionPath);
				var analysisClient = new AnalysisClient(analyzer, machines);
				var results = await analysisClient.RunExperiment(GrainClient.GrainFactory);

				result = string.Format("Ready for queries. Time: {0} ms", results.ElapsedTime);
			}
			catch (Exception exc)
			{
				while (exc is AggregateException) exc = exc.InnerException;
				result = "Error connecting to Orleans: " + exc + " at " + DateTime.Now;
			}

			return result;
		}

		[HttpGet]
		public async Task<string> PerformDeactivationAsync()
		{
			var result = string.Empty;

			try
			{
				var solutionGrain = GrainClient.GrainFactory.GetGrain<ISolutionGrain>("Solution");
				await AnalysisClient.PerformDeactivation(GrainClient.GrainFactory, solutionGrain);

				result = string.Format("All grains are deactivated");
			}
			catch (Exception exc)
			{
				while (exc is AggregateException) exc = exc.InnerException;
				result = "Error connecting to Orleans: " + exc + " at " + DateTime.Now;
			}

			return result;
		}
	}
}
