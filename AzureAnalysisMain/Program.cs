using Orleans.Runtime.Host;
using ReachingTypeAnalysis;
using SolutionTraversal.Callgraph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace AzureAnalysisMain
{
    class Program
    {
        private AnalysisStrategyKind strategyKind = AnalysisStrategyKind.ONDEMAND_ORLEANS;

        static void Main(string[] args)
        {
            string currentSolutionPath = @"\\t-digarb-z440\share\solutions";
            string solutionFileName = Path.Combine(currentSolutionPath, ConfigurationManager.AppSettings["Solution1"]);

            //var solutionFileName = args[0];
            var program = new Program();
            var callGraph = program.Analyze(solutionFileName);
        }


        public CallGraph<MethodDescriptor, LocationDescriptor> Analyze(string solutionFileName)
        {
            var analyzer = SolutionAnalyzer.CreateFromSolutionFile(solutionFileName);
            
            this.Initialize();
            var callgraph = analyzer.Analyze(strategyKind);
            this.Cleanup();

            var reachableMethods = callgraph.GetReachableMethods();
            Console.WriteLine("Reachable methods={0}", reachableMethods.Count);

            return callgraph;
        }

        private void Initialize()
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
        private void Cleanup()
        {

        }
    }
}
