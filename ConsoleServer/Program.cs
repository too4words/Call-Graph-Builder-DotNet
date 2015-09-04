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
using System.Threading;

namespace ConsoleServer
{
    public class Program
    {
        const uint DefaultPort = 7413;

		const AnalysisStrategyKind StrategyKind = AnalysisStrategyKind.ONDEMAND_ORLEANS;

		const string SolutionToTest = @"ConsoleApplication1\ConsoleApplication1.sln";
		//const string SolutionToTest = @"Coby\Coby.sln";

		const string CallGraphPath = @"C:\Temp\callgraph.dgml";

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
		private bool isCheckingForUpdates;
		private Timer checkForUpdatesTimer;
		private string gitDiffOutput;

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

			var program = new Program(StrategyKind);
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
			analyzer.AnalyzeAsync(strategyKind).Wait();

			OrleansController.SolutionManager = analyzer.SolutionManager;

			Console.WriteLine("Done");
		}

		private void InitializeServer(string baseAddress, uint port)
		{
			Console.WriteLine("Starting Local Server...");

			// Start OWIN host 
			using (WebApp.Start<Startup>(url: baseAddress))
			using (checkForUpdatesTimer = new Timer(OnCheckForUpdates, null, 10 * 1000, 10 * 1000))
			{
				Console.WriteLine(WelcomeMessage, port);
				this.CheckForCommands();
			}

			this.Cleanup();
			Console.WriteLine("Shutting down Local Server...");
		}

		private void OnCheckForUpdates(object state)
		{
            this.CheckForUpdates(false);
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

        private void CheckForCommands()
        {
            string command;

			do
			{
				command = Console.ReadLine();

				if (!string.IsNullOrWhiteSpace(gitDiffOutput))
				{
					if (command.StartsWith("y", StringComparison.InvariantCultureIgnoreCase))
					{
						var solutionFolder = Path.GetDirectoryName(solutionPath);
						RunGitCommand(solutionFolder, "pull");

						this.UpdateAnalysis();
					}
					else
					{
						Console.WriteLine("Keeping local version.");
						command = string.Empty;
					}

					this.gitDiffOutput = null;
				}
				else if (command.Equals("callgraph", StringComparison.InvariantCultureIgnoreCase))
				{
					this.GenerateCallGraph();
				}
				else if (command.Equals("update", StringComparison.InvariantCultureIgnoreCase))
				{
					this.CheckForUpdates(true);
				}

			}
			while (!command.Equals("exit", StringComparison.InvariantCultureIgnoreCase));
        }

		private void GenerateCallGraph()
		{
			Console.WriteLine("Generating call graph...");

			var callgraph =  analyzer.GenerateCallGraphAsync().Result;
			callgraph.Save(CallGraphPath);

			Console.WriteLine("Call graph generated successfully.");
		}
		
		private class CommandResult
		{
			public string Error { get; private set; }
			public string Output { get; private set; }

			public CommandResult(string output, string error)
			{
				this.Output = output;
				this.Error = error;
			}
		}

		private static CommandResult RunGitCommand(string workingDirectory, string command)
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
					RedirectStandardError = true,
					CreateNoWindow = true
				}
			};

			process.Start();
			process.StandardInput.WriteLine();

			var output = process.StandardOutput.ReadToEnd();
			var error = process.StandardError.ReadToEnd();
			process.WaitForExit();

			//var output = File.ReadAllText(outputTempFilePath);
			return new CommandResult(output, error);
		}

		private void CheckForUpdates(bool updateRequestedByUser)
		{
			if (isCheckingForUpdates) return;
			this.isCheckingForUpdates = true;

			var solutionFolder = Path.GetDirectoryName(solutionPath);
			var result = RunGitCommand(solutionFolder, "fetch");
			var isUpToDate = string.IsNullOrEmpty(result.Error);

			if (!isUpToDate || updateRequestedByUser)
			{
				result = RunGitCommand(solutionFolder, "diff --name-only origin/master");

				if (!string.IsNullOrWhiteSpace(result.Output))
				{
					Console.WriteLine("Incoming commits detected:");
					Console.Write(result.Output);
					Console.WriteLine("Do you want to pull the changes (y/n)?");

					this.gitDiffOutput = result.Output;
				}
				else if (updateRequestedByUser)
				{
					Console.WriteLine("There are no incoming commits.");
				}
			}
			else if (updateRequestedByUser)
			{
				Console.WriteLine("There are no incoming commits.");
			}

			this.isCheckingForUpdates = false;
		}

		private void UpdateAnalysis()
		{
			var solutionFolder = Path.GetDirectoryName(solutionPath);
			var modifiedDocuments = gitDiffOutput.Split('\n')
												 .Where(docPath => !string.IsNullOrEmpty(docPath))
												 .Select(docPath => docPath.Replace("/", @"\"))
												 .Select(docPath => Path.Combine(solutionFolder, docPath))
												 .ToList();

			Console.WriteLine("Starting incremental analysis...");

			analyzer.ApplyModificationsAsync(modifiedDocuments).Wait();

			Console.WriteLine("Incremental analysis finish");

			//var callGraph = analyzer.GenerateCallGraphAsync().Result;
			//callGraph.Save(CallGraphPath);
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
