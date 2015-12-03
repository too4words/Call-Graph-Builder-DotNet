//---------------------------------------------------------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//---------------------------------------------------------------------------------------------------------------------

namespace WebAPI
{
    using System.Web.Http;
    using System;
    using System.Threading.Tasks;
    using ReachingTypeAnalysis.Statistics;
    using ReachingTypeAnalysis;
    using Orleans;
    using System.IO;
    using OrleansInterfaces;
    using System.Collections.ObjectModel;
    using System.Management.Automation.Runspaces;
    using System.Management.Automation;

    public class ExperimentsController : ApiController
    {
		// http://localhost:49176/api/Experiments?testName=Hola&machines=1&numberOfMethods=2&expID=dummy
		[HttpGet]
		public Task<string> RunTestAsync(string testName, int machines, int numberOfMethods, string expID, string rootKind = "Default")
		{
			var result = string.Empty;

			try
			{
				var analyzer = SolutionAnalyzer.CreateFromTest(testName);
				var analysisClient = new AnalysisClient(analyzer, machines);
				//var results = await analysisClient.RunExperiment(GrainClient.GrainFactory, expID);
				analysisClient.StartRunningExperiment(GrainClient.GrainFactory, expID, Utils.ToAnalysisRootKind(rootKind));

				//result = string.Format("Ready for queries. Time: {0} ms", results.ElapsedTime);
				result = string.Format("Running test {0}.", testName);
			}
			catch (Exception exc)
			{
				while (exc is AggregateException) exc = exc.InnerException;
				result = "Error connecting to Orleans: " + exc + " at " + DateTime.Now;
			}

			return Task.FromResult(result);
		}

		[HttpGet]
		public Task<string> AnalyzeSolutionAsync(string drive, string solutionPath, string solutionName, int machines, string expID, string rootKind="Default")
		{
			var result = string.Empty;

			try
			{
                if (string.IsNullOrEmpty(expID))
                {
                    expID = solutionName;
                }
				solutionPath = Path.Combine(drive + ":\\" + solutionPath, solutionName + ".sln");
				var analyzer = SolutionAnalyzer.CreateFromSolution(solutionPath);
				var analysisClient = new AnalysisClient(analyzer, machines);
				//var results = await analysisClient.RunExperiment(GrainClient.GrainFactory, expID);
				analysisClient.StartRunningExperiment(GrainClient.GrainFactory, expID,Utils.ToAnalysisRootKind(rootKind));

				//result = string.Format("Ready for queries. Time: {0} ms", results.ElapsedTime);
				result = string.Format("Analyzing solution {0}.", solutionName);
			}
			catch (Exception exc)
			{
				while (exc is AggregateException) exc = exc.InnerException;
				result = "Error connecting to Orleans: " + exc + " at " + DateTime.Now;
			}

			return Task.FromResult(result);
		}

        [HttpGet]
        public async Task<string> ComputeQueries(string className, string methodPrefix, int machines, int numberOfMethods, int repetitions, string assemblyName, string expID)
        {
            var resultStr = "";
            try
            {
                var solutionGrain = GrainClient.GrainFactory.GetGrain<ISolutionGrain>("Solution");

                var analysisClient = new AnalysisClient(solutionGrain, machines);
                //var result = await analysisClient.ComputeRandomQueries(className, methodPrefix, numberOfMethods, repetitions, assemblyName, expID);
                var result = await analysisClient.ComputeRandomQueries(repetitions, expID);
                var avgTime = result.Item1;
                var minTime = result.Item2;
                var maxTime = result.Item3;
            }
            catch (Exception exc)
            {
                while (exc is AggregateException) exc = exc.InnerException;
                resultStr = "Error connecting to Orleans: " + exc + " at " + DateTime.Now;
            }
            return resultStr;
        }

        [HttpGet]
        public async Task<string> ComputeQueries(int machines, int repetitions, string expID)
        {
            var resultStr = "";
            try
            {
                var solutionGrain = GrainClient.GrainFactory.GetGrain<ISolutionGrain>("Solution");
                var analysisClient = new AnalysisClient(solutionGrain, machines);
                var result = await analysisClient.ComputeRandomQueries(repetitions, expID);
                var avgTime = result.Item1;
                var minTime = result.Item2;
                var maxTime = result.Item3;
            }
            catch (Exception exc)
            {
                while (exc is AggregateException) exc = exc.InnerException;
                resultStr = "Error connecting to Orleans: " + exc + " at " + DateTime.Now;
            }
            return resultStr;
        }

        //[HttpGet]
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

        [HttpGet]
        public async Task<string> ExecuteCmd(string command)
        {
            var result = string.Empty;
            try
            {
                switch (command)
                {
                    case "Deactivate":
                        result = await PerformDeactivationAsync();
                        break;
                    case "RemoveGrainState":
                        result = AnalysisClient.EmptyTable("OrleansGrainState").ToString();
                        break;
                    case "RemoveStats":
                        result = AnalysisClient.EmptyTable("OrleansSiloStatistics").ToString()+" " + AnalysisClient.EmptyTable("OrleansClientStatistics").ToString();
                        break;
                    case "Stats":
                        result = await AnalysisClient.PrintGrainStatistics(GrainClient.GrainFactory);
                        break;
					case "Status":
                        result = Convert.ToString(AnalysisClient.ExperimentStatus);
                        if (AnalysisClient.ExperimentStatus != ExperimentStatus.None
                            && AnalysisClient.ExperimentStatus != ExperimentStatus.Ready)
                        {
                            result += " Message:" + await AnalysisClient.LastMessage +"\n"+ AnalysisClient.ErrorMessage ; // + await AnalysisClient.CurrentAnalyzedMethodsCount;
                        }
						break;
                    case "OperationCount":
                        {

                        }
                        break;
                    case "Cancel":
                        await AnalysisClient.CancelExperimentAsync();
                        result = "Cancelled";
                        break;

                        // Unfortunately I cannot run the following scripts from a WebRole
                        // I can run scripts from Azure PowerShell on the development machine
                        // or use the Azure Web API
                        //case "Restart":
                        //    result = RunScript("Stop-Start-CloudService.ps1").ToString();
                        //    break;
                        //case "Instances":
                        //    result = RunScript("ChangeNumberOfInstances.ps1", "-Instances", "2").ToString();
                        //    break;
                }
            }
            catch (Exception exc)
            {
                while (exc is AggregateException) exc = exc.InnerException;
                result = "Error connecting to Orleans: " + exc + " at " + DateTime.Now;
            }

            return result;
        }

        public static Collection<PSObject> RunScript(string scriptfile, string pKey1 = "", string pValue1 = "")
        {
            var runspaceConfiguration = RunspaceConfiguration.Create();
            var runspace = RunspaceFactory.CreateRunspace(runspaceConfiguration);
            runspace.Open();

            var scriptInvoker = new RunspaceInvoke(runspace);
            var pipeline = runspace.CreatePipeline();

            // Here's how you add a new script with arguments
            var rolePath = Environment.GetEnvironmentVariable("RoleRoot");
            var path = Path.Combine(rolePath, "approot", "bin", "scripts");
            var myCommand = new Command(Path.Combine(path, scriptfile));

            if (!String.IsNullOrEmpty(pKey1))
            {
                var testParam = new CommandParameter(pKey1, pValue1);
                myCommand.Parameters.Add(testParam);
            }

            pipeline.Commands.Add(myCommand);

            // Execute PowerShell script
            var results = pipeline.Invoke();
            return results;
        }
    }
}
