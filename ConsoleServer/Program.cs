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
using Orleans;
using System.Diagnostics.Contracts;

namespace ConsoleServer
{
    public class Program
    {
        const uint DefaultPort = 7413;

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

		private AnalysisStrategyKind strategyKind;
		private AppDomain hostDomain;
		private static OrleansHostWrapper hostWrapper;

		public Program(AnalysisStrategyKind strategyKind)
		{
			this.strategyKind = strategyKind;
		}

		public void Start(string baseAddress, uint port, string solutionPath)
		{
			InitializeAnalysis(solutionPath);
			InitializeServer(baseAddress, port);
		}

		static void Main(string[] args)
        {
            // read port argument
            var port = GetValidPort(args.Length >= 1 ? args[0] : null);
            var baseAddress = string.Format("http://localhost:{0}/", port);
			//var solutionPath = @"..\..\..\ConsoleApplication1\ConsoleApplication1.sln";
			var solutionPath = Path.Combine(OrleansController.ROOT_DIR, @"ConsoleApplication1\ConsoleApplication1.sln");
			//var solutionPath = @"C:\Users\t-edzopp\Desktop\Roslyn\Roslyn.sln";

			var program = new Program(AnalysisStrategyKind.ONDEMAND_ASYNC);
			program.Start(baseAddress, port, solutionPath);

			Console.WriteLine("Done");
		}

		private void InitializeAnalysis(string solutionPath)
		{
			Console.WriteLine("Analyzing solution...");

			this.Initialize();
			var analyzer = SolutionAnalyzer.CreateFromSolution(solutionPath);
			//analyzer.Analyze(AnalysisStrategyKind.ONDEMAND_ASYNC);
			analyzer.Analyze(strategyKind);
			OrleansController.Strategy = analyzer.Strategy;

			Console.WriteLine("Done");
		}

		private void InitializeServer(string baseAddress, uint port)
		{
			Console.WriteLine("Starting Local Server...");

			// Start OWIN host 
			using (WebApp.Start<Startup>(url: baseAddress))
			{
				Console.WriteLine(WelcomeMessage, port);
				CheckForExit();
			}

			this.Cleanup();
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

		private void Initialize()
		{
			if (strategyKind != AnalysisStrategyKind.ONDEMAND_ORLEANS) return;
			Console.WriteLine("Initializing Orleans silo...");

			var applicationPath = Environment.CurrentDirectory;

			var appDomainSetup = new AppDomainSetup
			{
				AppDomainInitializer = InitSilo,
				ApplicationBase = applicationPath,
				ApplicationName = "ConsoleServer",
				AppDomainInitializerArguments = new string[] { },
				ConfigurationFile = "ConsoleServer.exe.config"
			};

			// set up the Orleans silo
			hostDomain = AppDomain.CreateDomain("OrleansHost", null, appDomainSetup);

			var xmlConfig = "ClientConfigurationForTesting.xml";
			Contract.Assert(File.Exists(xmlConfig), "Can't find " + xmlConfig);

			GrainClient.Initialize(xmlConfig);
			Console.WriteLine("Orleans silo initialized successfully");
		}

		private void Cleanup()
		{
			if (strategyKind != AnalysisStrategyKind.ONDEMAND_ORLEANS) return;

			hostDomain.DoCallBack(ShutdownSilo);
		}

		private static void InitSilo(string[] args)
		{
			hostWrapper = new OrleansHostWrapper();
			hostWrapper.Init();
			var ok = hostWrapper.Run();

			if (!ok)
			{
				Console.WriteLine("Failed to initialize Orleans silo");
			}
		}

		private static void ShutdownSilo()
		{
			if (hostWrapper != null)
			{
				hostWrapper.Dispose();
				GC.SuppressFinalize(hostWrapper);
			}
		}
	}
}
