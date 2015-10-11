using Microsoft.WindowsAzure.ServiceRuntime;
using System.IO;
using Orleans;
using Orleans.Runtime.Host;
using OrleansInterfaces;
using ReachingTypeAnalysis;
using SolutionTraversal.CallGraph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ReachingTypeAnalysis.Statistics;

namespace WebRole1
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
			var ipAddr = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["EndPoint1"].IPEndpoint.Address.ToString();
			Environment.SetEnvironmentVariable("MyIPAddr", ipAddr);
            Environment.SetEnvironmentVariable("ISORLEANSCLIENT", "True");

            if (Page.IsPostBack)
            {
            // No longer needed. The emulator handles the same configuration
            //if (!RoleEnvironment.IsEmulated || true)
            //{
					if (!AzureClient.IsInitialized)
					{
						FileInfo clientConfigFile = AzureConfigUtils.ClientConfigFileLocation;
						if (!clientConfigFile.Exists)
						{
							throw new FileNotFoundException(string.Format("Cannot find Orleans client config file for initialization at {0}", clientConfigFile.FullName), clientConfigFile.FullName);
						}
						AzureClient.Initialize(clientConfigFile);
					}
                //}
                //else
                //{
                //    GrainClient.Initialize(Server.MapPath(@"~/LocalConfiguration.xml"));
                //}
				
            }
        }

		protected void ButtonTestSolution_Click(object sender, EventArgs e)
        {
            try
            {
                this.TextBox1.Text = "Accessing Grains...";
				var solutionPath = TextBoxPath.Text;
				var pathPrefix = TextBoxPathPrefix.Text;

				try
				{
                    var tokens = TextRandomQueryInput.Text.Split(';');
                    var className = tokens[0];
                    var methodPrejix = tokens[1];
                    var numberOfMethods = int.Parse(tokens[2]);
                    var repetitions = int.Parse(tokens[3]);
                    var machines = int.Parse(tokens[4]);

					var solutionFileName = Path.Combine(pathPrefix, solutionPath);
					var analyzer = SolutionAnalyzer.CreateFromSolution(solutionFileName);

					var analysisClient = new AnalysisClient(analyzer, machines);
					// await analysisClient.Analyze();
					//await analysisClient.RunExperiment(GrainClient.GrainFactory, solutionPath.Replace('\\','-').Replace('.','-'));
					analysisClient.StartRunningExperiment(GrainClient.GrainFactory, solutionPath.Replace('\\', '-').Replace('.', '-'));

					//var reachableMethods = await RunAnalysisAsync(machines, pathPrefix, solutionPath);
					//string methods = String.Join("\n", reachableMethods);
					//this.TextBox1.Text = string.Format("Reachable methods={0} \n{1}", reachableMethods.Count, methods);
					this.TextBox1.Text = "Done";
				}
				catch(Exception exc)
				{
					while (exc is AggregateException) exc = exc.InnerException;
					System.Diagnostics.Trace.TraceError("Error dutring initialization of WorkerRole {0}", exc.ToString());
					this.TextBox1.Text = exc.ToString();
				}
            }
            catch (Exception exc)
            {
                while (exc is AggregateException) exc = exc.InnerException;

                this.TextBox1.Text = "Error connecting to Orleans: " + exc + " at " + DateTime.Now;
            }
        }

        private static async Task<ISet<MethodDescriptor>> RunAnalysisAsync(int machines, string pathPrefix, string solutionRelativePath)
        {
			var currentSolutionPath = pathPrefix;			
            var solutionFileName = Path.Combine(currentSolutionPath, solutionRelativePath);

			var analyzer = SolutionAnalyzer.CreateFromSolution(solutionFileName);

			var analysisClient = new AnalysisClient(analyzer, machines);
			var callgraph =  await analysisClient.Analyze();

            var reachableMethods = callgraph.GetReachableMethods();
            return await Task.FromResult(reachableMethods);
        }

		private static async Task<ISet<MethodDescriptor>> RunAnalysisFromSourceAsync(int machines, string source)
		{
			var analyzer = SolutionAnalyzer.CreateFromSource(source);
			var analysisClient = new AnalysisClient(analyzer, machines);
			var callgraph = await analysisClient.Analyze();

			var reachableMethods = callgraph.GetReachableMethods();
			return await Task.FromResult(reachableMethods);
		}

		protected async void ButtonTestSource_Click(object sender, EventArgs e)
		{
			try
            {
                var tokens = TextRandomQueryInput.Text.Split(';');
                var className = tokens[0];
                var methodPrejix = tokens[1];
                var numberOfMethods = int.Parse(tokens[2]);
                var repetitions = int.Parse(tokens[3]);
                var machines = int.Parse(tokens[4]);

				var solutionSource = TextBox1.Text;
                var reachableMethods = await RunAnalysisFromSourceAsync(machines, solutionSource);
				var methods = string.Join("\n", reachableMethods);
                this.TextBox1.Text = string.Format("Reachable methods={0} \n{1}", reachableMethods.Count,methods);
            }
            catch (Exception exc)
            {
                while (exc is AggregateException) exc = exc.InnerException;
                this.TextBox1.Text = "Error connecting to Orleans: " + exc + " at " + DateTime.Now;
            }
		}

		protected void ButtonTestTest_Click(object sender, EventArgs e)
        {
            try
            {
                var testName = TextBoxPath.Text;
                var tokens = TextRandomQueryInput.Text.Split(';');

                var className = tokens[0];
                var methodPrejix = tokens[1];
                var numberOfMethods = int.Parse(tokens[2]);
                var repetitions = int.Parse(tokens[3]);
                var machines = int.Parse(tokens[4]);

				var analyzer = SolutionAnalyzer.CreateFromTest(testName);
				var analysisClient = new AnalysisClient(analyzer, machines);

				//var stopWatch = Stopwatch.StartNew();
				//var results = await analysisClient.RunExperiment(GrainClient.GrainFactory);
				analysisClient.StartRunningExperiment(GrainClient.GrainFactory);

				//stopWatch.Stop();
				Application["AnalysisClient"] = analysisClient;

				//this.TextBox1.Text = string.Format("Ready for queries. Time: {0} ms", results.ElapsedTime);
				this.TextBox1.Text = string.Format("Running test {0}.", testName);

				//Logger.LogInfo(GrainClient.Logger, "Stats", "Query", "Analyzing {0} took:{1} ms", testName, results.ElapsedTime);
				Logger.LogInfo(GrainClient.Logger, "Stats", "Query", "Analyzing {0}.", testName);

                //var result = await analysisClient.ComputeRandomQueries(className, methodPrejix, numberOfMethods, repetitions);

                //var result = await analysisClient.ComputeRandomQueries(repetitions);


                //program.RetriveInfoFromAnalysis();

                //System.Diagnostics.Trace.TraceInformation("Analyzing {0} took:{1} ms", testName, stopWatch.ElapsedMilliseconds);
            }
            catch (Exception exc)
            {
                while (exc is AggregateException) exc = exc.InnerException;
                this.TextBox1.Text = "Error connecting to Orleans: " + exc + " at " + DateTime.Now;
            }
        }

		protected void ButtonQueryStatus_Click(object sender, EventArgs e)
		{
			try
			{
				var status = AnalysisClient.ExperimentStatus;
				this.TextBox1.Text = string.Format("Analysis status={0}", status);
			}
			catch (Exception exc)
			{
				while (exc is AggregateException) exc = exc.InnerException;
				this.TextBox1.Text = "Error connecting to Orleans: " + exc + " at " + DateTime.Now;
			}
		}

		protected async void ButtonQueryCallees_Click(object sender, EventArgs e)
		{
			try
            {
				var analysisClient = (AnalysisClient)Application.Get("AnalysisClient"); 

				if (analysisClient != null)
				{
					string[] tokens = TextBoxPathPrefix.Text.Split(';');

                    var type = new TypeDescriptor("", tokens[0]);
					var parameters = new TypeDescriptor[] { };

					var methodDescriptor = new MethodDescriptor(type, tokens[1], true,parameters);

					var invocation = int.Parse(tokens[2]);


					IEnumerable<MethodDescriptor> result = null;
					var stopWatch = Stopwatch.StartNew();
					if (invocation > 0)
					{
						result = await CallGraphQueryInterface.GetCalleesAsync(analysisClient.SolutionManager, methodDescriptor, invocation, "");
					}
					else
					{
						result = await CallGraphQueryInterface.GetCalleesAsync(analysisClient.SolutionManager, methodDescriptor);
					}
					
					stopWatch.Stop();

					string calleesStr = String.Join("\n", result);
					TextBox1.Text = String.Format("Callees:{0} \nTime:{1}", calleesStr,stopWatch.ElapsedMilliseconds);

					Logger.LogInfo(GrainClient.Logger,"Stats","Query","Callees of {0} :{1} \nTime:{2}", methodDescriptor, calleesStr,stopWatch.ElapsedMilliseconds);

					// System.Diagnostics.Trace.TraceInformation("Callees of {0} :{1} \nTime:{2}", methodDescriptor, calleesStr,stopWatch.ElapsedMilliseconds);
					//var solutionManager = analysisClient.SolutionManager as ISolutionGrain;
					//var drives = await solutionManager.GetDrives();
					//string drivesStr = String.Join("\n", drives);
					//TextBox1.Text = drivesStr;
				}
				else
				{
					TextBox1.Text = "SolutionManager is null...";
				}
			}
			catch (Exception exc)
			{
				while (exc is AggregateException) exc = exc.InnerException;
				this.TextBox1.Text = "Error connecting to Orleans: " + exc + " at " + DateTime.Now;
			}
		}

		protected async void ButtonRandomQueries_Click(object sender, EventArgs e)
		{
			var analysisClient= (AnalysisClient)Application.Get("AnalysisClient");
			if (analysisClient != null)
			{
				string[] tokens = TextRandomQueryInput.Text.Split(';');

				var className = tokens[0];
				var methodPrejix = tokens[1];
				var numberOfMethods = int.Parse(tokens[2]);
				var repetitions = int.Parse(tokens[3]);
                var machines = int.Parse(tokens[4]);
                try
                {
                    var testName = TextBoxPath.Text;
                    var result = await analysisClient.ComputeRandomQueries(className, methodPrejix, numberOfMethods, repetitions);
                    var avgTime = result.Item1;
                    var minTime = result.Item2;
                    var maxTime = result.Item3;

                    TextBox1.Text = string.Format("Random Query times; Avg; {0}; Min {1}; Max; {2}", avgTime, minTime, maxTime);
                    System.Diagnostics.Trace.TraceInformation("Random Query times; Avg; {0}; Min {1}; Max; {2}", avgTime, minTime, maxTime);
                    Logger.LogInfo(GrainClient.Logger, "Stats", "Random Query times; ", "Avg; {0}; Min {1}; Max; {2}", avgTime, minTime, maxTime);
                }
                catch (Exception exc)
                {
                    while (exc is AggregateException) exc = exc.InnerException;
                    this.TextBox1.Text = "Error connecting to Orleans: " + exc + " at " + DateTime.Now;
                }
			}
		}

		protected async void ButtonRemoveGrains_Click(object sender, EventArgs e)
		{
			// This doesn't work. I need to move OrleansSolutionManager no another project
			// var solutionGrain = OrleansSolutionManager.GetSolutionGrain(GrainClient.GrainFactory);
			var solutionGrain = GrainClient.GrainFactory.GetGrain<ISolutionGrain>("Solution");			

			// var analysisClient = (AnalysisClient)Application.Get("AnalysisClient");
			//if (analysisClient != null && analysisClient.SolutionManager is ISolutionGrain)
			{
				await AnalysisClient.PerformDeactivation(GrainClient.GrainFactory, solutionGrain);
				this.TextBox1.Text = "All Grains Deactivated!";
			}
		}

		protected async void ButtonStats_Click(object sender, EventArgs e)
		{
			//var analysisClient = (AnalysisClient)Application.Get("AnalysisClient");
			//if (analysisClient != null)
			//{
				// var res = await OrleansManager.Program.RunCommand("fullgrainstats", new string[] { });
				var res = await AnalysisClient.PrintGrainStatistics(GrainClient.GrainFactory);
				this.TextBox1.Text = res;
			//}
		}
    }
}