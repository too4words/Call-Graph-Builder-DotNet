using Orleans.Runtime.Host;
using ReachingTypeAnalysis;
using SolutionTraversal.CallGraph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        protected void Button1_Click(object sender, EventArgs e)
        {
            var reachableMethods = RunAnalysis();
            this.TextBox1.Text = string.Format("Reachable methods={0}", reachableMethods.Count);
        }
        private static ISet<MethodDescriptor> RunAnalysis()
        {
            //string currentSolutionPath = @"\\t-digarb-z440\share\solutions";
            string currentSolutionPath = @"\\MSR-LENX1-001\Users\t-digarb\Temp";        
            string solutionFileName = Path.Combine(currentSolutionPath, @"ConsoleApplication1\ConsoleApplication1.sln");

            //var solutionFileName = args[0];
            var program = new AnalysisClient();
            var callgraph = program.Analyze(solutionFileName);
            var reachableMethods = callgraph.GetReachableMethods();
            return reachableMethods;
        }

        protected void TextBox1_TextChanged(object sender, EventArgs e)
        {

        }

    }

}