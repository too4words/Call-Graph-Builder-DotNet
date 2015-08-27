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
using System.Diagnostics;

namespace ConsoleServer
{
    public class Program
    {
        const uint DefaultPort = 7413;

		const string SolutionToTest = @"ConsoleApplication1\ConsoleApplication1.sln";
		//const string SolutionToTest = @"Coby\Coby.sln";

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
		private SolutionAnalyzer analyzer;
		private string solutionPath;

		public Program(AnalysisStrategyKind strategyKind)
		{
			this.strategyKind = strategyKind;
		}

		static void Main(string[] args)
        {
            // read port argument
            var port = GetValidPort(args.Length >= 1 ? args[0] : null);
            var baseAddress = string.Format("http://localhost:{0}/", port);
			var solutionPath = Path.Combine(OrleansController.ROOT_DIR, SolutionToTest);

			var program = new Program(AnalysisStrategyKind.ONDEMAND_ASYNC);
			program.Start(solutionPath, baseAddress, port);

			Console.WriteLine("Done");
		}

		public void Start(string solutionPath, string baseAddress, uint port)
		{
            InitializeAnalysis(solutionPath);
			InitializeServer(baseAddress, port);
		}

		private void InitializeAnalysis(string solutionPath)
		{
			Console.WriteLine("Analyzing solution...");

			this.Initialize();
			this.solutionPath = solutionPath;
			this.analyzer = SolutionAnalyzer.CreateFromSolution(solutionPath);
			//analyzer.Analyze(AnalysisStrategyKind.ONDEMAND_ASYNC);
			analyzer.Analyze(strategyKind);
			OrleansController.SolutionManager = analyzer.SolutionManager;

			Console.WriteLine("Done");
		}

		private void InitializeServer(string baseAddress, uint port)
		{
			Console.WriteLine("Starting Local Server...");

			// Start OWIN host 
			using (WebApp.Start<Startup>(url: baseAddress))
			{
				Console.WriteLine(WelcomeMessage, port);
				this.CheckForExit();
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

        private void CheckForExit()
        {
            string command;

			do
			{
				command = Console.ReadLine();

				if (command.Equals("update", StringComparison.InvariantCultureIgnoreCase))
				{
					this.CheckForUpdatesAsync().Wait();
				}
			}
			while (!command.Equals("exit", StringComparison.InvariantCultureIgnoreCase));
        }

		private static string RunGitCommand(string workingDirectory, string command)
		{
			var programFilesDirectory = Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%");
			//var outputTempFilePath = Path.GetTempFileName();

			var process = new Process()
			{
				StartInfo = new ProcessStartInfo()
				{
					//FileName = "git.exe",
					//Arguments = "diff --name-only",
					//FileName = "cmd",
					FileName = Path.Combine(programFilesDirectory, @"Git\bin\git.exe"),
					Arguments = command,
					WorkingDirectory = workingDirectory,
					UseShellExecute = false,
					RedirectStandardInput = true,
					RedirectStandardOutput = true,
					CreateNoWindow = true
				}
			};

			process.Start();
			process.StandardInput.WriteLine();

			var output = process.StandardOutput.ReadToEnd();
			process.WaitForExit();

			//var output = File.ReadAllText(outputTempFilePath);
			return output;
		}

		private async Task CheckForUpdatesAsync()
		{
			Console.WriteLine("Checking for updates...");

			var solutionFolder = Path.GetDirectoryName(solutionPath);
			RunGitCommand(solutionFolder, "fetch");

			var output = RunGitCommand(solutionFolder, "diff --name-only origin/master");
			var modifiedDocuments = output.Split('\n')
				.Where(docPath => !string.IsNullOrEmpty(docPath))
				.Select(docPath => docPath.Replace("/", @"\"))
				.Select(docPath => Path.Combine(solutionFolder, docPath));

			if (modifiedDocuments.Any())
			{
				Console.WriteLine("Modified documents found:");
				Console.WriteLine(output);
				Console.WriteLine("Pull changes (y/n)?");

				var command = Console.ReadLine();

				if (command.StartsWith("y", StringComparison.InvariantCultureIgnoreCase))
				{
					RunGitCommand(solutionFolder, "pull");
					await this.UpdateAnalysisAsync(modifiedDocuments);
				}
			}
			else
			{
				Console.WriteLine("There are no modified documents.");
			}
        }

		private async Task UpdateAnalysisAsync(IEnumerable<string> modifiedDocuments)
		{
			Console.WriteLine("Starting incremental analysis...");

			await analyzer.ApplyModificationsAsync(modifiedDocuments);

			Console.WriteLine("Incremental analysis finish");
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
