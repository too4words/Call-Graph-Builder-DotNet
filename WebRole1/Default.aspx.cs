using Orleans;
using Orleans.Runtime.Host;
using OrleansInterfaces;
using ReachingTypeAnalysis;
using SolutionTraversal.CallGraph;
using System;
using System.Collections.Generic;
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
				var callgraph = await program.AnalyzeTestAsync(testName);
				var reachableMethods = callgraph.GetReachableMethods();
				string methods = String.Join("\n", reachableMethods);
				this.TextBox1.Text = string.Format("Reachable methods={0} \n{1}", reachableMethods.Count, methods);
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
				var solutionManager = GrainClient.GrainFactory.GetGrain<ISolutionGrain>("Solution");
				var drives = await solutionManager.GetDrives();
				string drivesStr = String.Join("\n", drives);
				TextBox1.Text = drivesStr;
			}
			catch (Exception exc)
			{
				while (exc is AggregateException) exc = exc.InnerException;
				this.TextBox1.Text = "Error connecting to Orleans: " + exc + " at " + DateTime.Now;
			}
		}
		
    }

}