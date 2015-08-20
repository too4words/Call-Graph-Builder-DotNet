using Microsoft.WindowsAzure.ServiceRuntime;
using Orleans;
using Orleans.Runtime.Host;
using OrleansInterfaces;
using ReachingTypeAnalysis;
using SolutionTraversal.CallGraph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebRole1
{
    public partial class _Default : Page
    {

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Page.IsPostBack)
            {
				if (!RoleEnvironment.IsEmulated)
				{
					if (!AzureClient.IsInitialized)
					{
						FileInfo clientConfigFile = AzureConfigUtils.ClientConfigFileLocation;
						if (!clientConfigFile.Exists)
						{
							throw new FileNotFoundException(string.Format("Cannot find Orleans client config file for initialization at {0}", clientConfigFile.FullName), clientConfigFile.FullName);
						}
						AzureClient.Initialize(clientConfigFile);
					}
				}
				else
				{
					GrainClient.Initialize(Server.MapPath(@"~/LocalConfiguration.xml"));
				}
				
            }
        }

        protected async void Button1_Click(object sender, EventArgs e)
        {
            try
            {
                this.TextBox1.Text = "Accessing Grains...";
				var solutionPath = TextBoxPath.Text;
				var pathPrefix = TextBoxPathPrefix.Text;
                var reachableMethods = await RunAnalysisAsync(pathPrefix, solutionPath);
				string methods = String.Join("\n", reachableMethods);
                this.TextBox1.Text = string.Format("Reachable methods={0} \n{1}", reachableMethods.Count,methods);
            }
            catch (Exception exc)
            {
                while (exc is AggregateException) exc = exc.InnerException;

                this.TextBox1.Text = "Error connecting to Orleans: " + exc + " at " + DateTime.Now;
            }
        }
        private static async Task<ISet<MethodDescriptor>> RunAnalysisAsync(string pathPrefix, 
																	string solutionRelativePath)
        {
			string currentSolutionPath = pathPrefix;
			
            string solutionFileName = Path.Combine(currentSolutionPath, solutionRelativePath);
            
            var program = new AnalysisClient();
            var callgraph = await program.AnalyzeSolutionAsync(solutionFileName);
            var reachableMethods = callgraph.GetReachableMethods();
            return await Task.FromResult(reachableMethods);
        }

		private static async Task<ISet<MethodDescriptor>> RunAnalysisFromSourceAsync(string source)
		{
			var program = new AnalysisClient();
			var callgraph = await program.AnalyzeSourceCodeAsync(source);
			var reachableMethods = callgraph.GetReachableMethods();
			return await Task.FromResult(reachableMethods);
		}


		protected async void Button3_Click(object sender, EventArgs e)
		{
			try
            {
				var solutionSource = TextBox1.Text;
                var reachableMethods = await RunAnalysisFromSourceAsync(solutionSource);
				string methods = String.Join("\n", reachableMethods);
                this.TextBox1.Text = string.Format("Reachable methods={0} \n{1}", reachableMethods.Count,methods);
            }
            catch (Exception exc)
            {
                while (exc is AggregateException) exc = exc.InnerException;
                this.TextBox1.Text = "Error connecting to Orleans: " + exc + " at " + DateTime.Now;
            }


		}

        protected async void Button4_Click(object sender, EventArgs e)
        {
            try
            {
                var program = new AnalysisClient();
                var testName = TextBoxPath.Text;
				//var callgraph = await program.AnalyzeTestAsync(testName);
				// var reachableMethods = callgraph.GetReachableMethods();
				// string methods = String.Join("\n", reachableMethods);
				//this.TextBox1.Text = string.Format("Reachable methods={0} \n{1}", reachableMethods.Count, methods);
				var stopWatch = Stopwatch.StartNew();
				var solutionManager = await program.AnalyzeTestAsync(testName);
				stopWatch.Stop();
				Application["SolutionManager"] = solutionManager;
				this.TextBox1.Text = string.Format("Ready for queries. Time: {0} ms",stopWatch.ElapsedMilliseconds);
            }
            catch (Exception exc)
            {
                while (exc is AggregateException) exc = exc.InnerException;
                this.TextBox1.Text = "Error connecting to Orleans: " + exc + " at " + DateTime.Now;
            }

        }

        protected void TextBox1_TextChanged(object sender, EventArgs e)
        {

        }

		protected async void Button5_Click(object sender, EventArgs e)
		{
			try
            {
				var solutionManager = (ISolutionManager)Application.Get("SolutionManager"); 

				if (solutionManager != null)
				{
					string[] tokens = TextBoxPathPrefix.Text.Split(';');

					var methodDescriptor = new MethodDescriptor(tokens[0], tokens[1], true);

					var invocation = int.Parse(tokens[2]);

					IEnumerable<MethodDescriptor> result = null;
					var stopWatch = Stopwatch.StartNew();
					if (invocation > 0)
					{
						result = await CallGraphQueryInterface.GetCalleesAsync(solutionManager, methodDescriptor, invocation, "");
					}
					else
					{
						result = await CallGraphQueryInterface.GetCalleesAsync(solutionManager, methodDescriptor);
					}
					
					stopWatch.Stop();

					string calleesStr = String.Join("\n", result);
					TextBox1.Text = String.Format("Callees:{0} \nTime:{1}", calleesStr,stopWatch.ElapsedMilliseconds);
					//var solutionManager = GrainClient.GrainFactory.GetGrain<ISolutionGrain>("Solution");
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

		protected void TextBoxPath_TextChanged(object sender, EventArgs e)
		{
			

		}

		protected async void Button6_Click(object sender, EventArgs e)
		{
			var solutionManager = (ISolutionManager)Application.Get("SolutionManager");
			if (solutionManager != null)
			{
				var random = new Random();
				string[] tokens = TextRandomQueryInput.Text.Split(';');

				var className = tokens[0];
				var methodPrejix = tokens[1];
				var numberOfMethods = int.Parse(tokens[2]);
				var repetitions = int.Parse(tokens[3]);

				long sumTime = 0;
				long maxTime = 0;
				long minTime = long.MaxValue;
				

				for (int i = 0; i < repetitions; i++)
				{
					int methodNumber = random.Next(numberOfMethods) + 1;
					var methodDescriptor = new MethodDescriptor(className, methodPrejix + methodNumber, true);
					var invocationCount = await CallGraphQueryInterface.GetInvocationCountAsync(solutionManager, methodDescriptor);

					var invocation = random.Next(invocationCount) + 1;

					IEnumerable<MethodDescriptor> result = null;
					
					var stopWatch = Stopwatch.StartNew();
					if (invocation > 0)
					{
						result = await CallGraphQueryInterface.GetCalleesAsync(solutionManager, methodDescriptor, invocation, "");
					}
					else
					{
						result = await CallGraphQueryInterface.GetCalleesAsync(solutionManager, methodDescriptor);
					}

					stopWatch.Stop();

					var time = stopWatch.ElapsedMilliseconds;
					if (time > maxTime) maxTime = time;
					if (time < minTime) minTime = time;
					sumTime += time;
				}
				if(repetitions>0)
				{
					var avg = sumTime / repetitions;
				}
			}
		}
	
    }

}