using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using System.Net.Http;
using System.Reflection;
using ReachingTypeAnalysis;
using ConsoleServer.Controllers;
using System.IO;

namespace ConsoleServer
{
    public class Program
    {
        const uint DefaultPort = 7412;

        const string WelcomeMessage = @"Console Server started
-----------------
Routes: 
   - Get All Files........:  api/{{graph}}/files
   - Get File.............:  api/{{graph}}/entities/file/{{*filePath}}
   - Get References.......:  api/{{graph}}/references/{{uid}}
   - Get Top References...:  api/{{graph}}/referencecount/{{count}}
   - Get Top Co-occurences:  api/{{graph}}/co-occurs/file/{{count}}
(Enter 'Exit' to shutdown)

Listening on Port {0} ...

";

        static void Main(string[] args)
        {
            // read port argument
            var port = GetValidPort(args.Length >= 1 ? args[0] : null);
            var baseAddress = string.Format("http://localhost:{0}/", port);
			//var solutionPath = @"..\..\..\ConsoleApplication1\ConsoleApplication1.sln";
			var solutionPath = Path.Combine(OrleansController.ROOT_DIR, @"ConsoleApplication1\ConsoleApplication1.sln");
			//var solutionPath = @"C:\Users\t-edzopp\Desktop\Roslyn\Roslyn.sln";

			InitializeAnalysis(solutionPath);
			InitializeServer(baseAddress, port);

			Console.WriteLine("Done");
		}

		private static void InitializeAnalysis(string solutionPath)
		{
			Console.WriteLine("Analyzing solution...");

			var analyzer = SolutionAnalyzer.CreateFromSolution(solutionPath);
			analyzer.Analyze(AnalysisStrategyKind.ONDEMAND_ASYNC);
			OrleansController.SolutionManager = analyzer.Strategy.SolutionManager;

			Console.WriteLine("Done");
		}

		private static void InitializeServer(string baseAddress, uint port)
		{
			Console.WriteLine("Starting Local Server...");

			// Start OWIN host 
			using (WebApp.Start<Startup>(url: baseAddress))
			{
				Console.WriteLine(WelcomeMessage, port);
				CheckForExit();
			}

			Console.WriteLine("Shutting down Local Server...");
		}

		private static uint GetValidPort(string port)
        {
            uint numericPort;

            if (port == null || !uint.TryParse(port, out numericPort))
            {
                numericPort = DefaultPort;
            }

            return numericPort;
        }

        private static void CheckForExit()
        {
            string command;

			do
			{
				command = Console.ReadLine();
			}
			while (!command.Equals("Exit", StringComparison.InvariantCultureIgnoreCase));
        }
	}
}
