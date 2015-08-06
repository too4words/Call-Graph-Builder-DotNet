using Orleans.Runtime.Host;
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
                var reachableMethods = await RunAnalysisAsync();
                this.TextBox1.Text = string.Format("Reachable methods={0}", reachableMethods.Count);
            }
            catch (Exception exc)
            {
                while (exc is AggregateException) exc = exc.InnerException;

                this.TextBox1.Text = "Error connecting to Orleans: " + exc + " at " + DateTime.Now;
            }
        }
        private static async Task<ISet<MethodDescriptor>> RunAnalysisAsync()
        {
            string currentSolutionPath = @"\\t-digarb-z440\share\solutions";
            //string currentSolutionPath = @"\\MSR-LENX1-001\Users\t-digarb\Temp";        
            string solutionFileName = Path.Combine(currentSolutionPath, @"ConsoleApplication1\ConsoleApplication1.sln");
            #region source code
            var source = @"
using System;
public class D:C
{
    public  override void m2(C b)
    {
    }
}
public class C 
{
    int f = 0;
    C g;
    public C m1(C a)
    {
         f = 0;
         g = this;
         this.m2(a);
         m2(g);
         return a;
    }
    public virtual void m2(C b)
    {
        Console.WriteLine(f);
    }
}
class Program
{

    public static void Main()
    {
        C d = new D();
        C c;
        c = new C();
        C h = d.m1(d);
        h.m2(c);
        d.Equals(c);
    }
}";
            #endregion

            //var solutionFileName = args[0];
            var program = new AnalysisClient();
            //var callgraph = await program.AnalyzeSolutionAsync(solutionFileName);
            var callgraph = await program.AnalyzeSourceCodeAsync(source);
            var reachableMethods = callgraph.GetReachableMethods();
            return await Task.FromResult(reachableMethods);
        }

        protected void TextBox1_TextChanged(object sender, EventArgs e)
        {

        }

    }

}