using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using System.Net.Http;
using System.Reflection;
using System.IO;
using Orleans;
using System.Diagnostics.Contracts;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.Web;

namespace ConsoleCobyClient
{
    public class Program
    {
		private const string SolutionToTest = @"coby\Coby.sln";

		private const string CallGraphPath = @"C:\Temp\callgraph.dgml";

		private const string DefaultBaseAddress = "http://localhost:{0}/api/Orleans";
		private const uint DefaultPort = 49176;

		private string solutionPath;
		private string baseAddress;
        private bool isCheckingForUpdates;
		private Timer checkForUpdatesTimer;
		private string gitDiffOutput;

		public static void Main(string[] args)
        {
            // read port argument
            var port = GetValidPort(args.Length >= 1 ? args[0] : null);
            var baseAddress = string.Format(DefaultBaseAddress, port);
			var solutionPath = Path.Combine(@"C:\Users\t-edzopp\Desktop\Demo", SolutionToTest);

			var program = new Program();
			program.Start(solutionPath, baseAddress);

			Console.WriteLine("Done");
		}

		public void Start(string solutionPath, string baseAddress)
		{
			this.solutionPath = solutionPath;
			this.baseAddress = baseAddress;

			this.InitializeAnalysis();
			this.InitializeTimer();
		}

		private void InitializeAnalysis()
		{
			Console.WriteLine("Analyzing solution...");

			var arguments = new Dictionary<string, object>();
			arguments.Add("solutionPath", this.solutionPath);

			var response = this.RunWebAPICommandAsync(arguments).Result;

			Console.WriteLine("Done");
		}

		private void InitializeTimer()
		{
			using (checkForUpdatesTimer = new Timer(OnCheckForUpdates, null, 10 * 1000, 10 * 1000))
			{
				this.CheckForCommands();
			}
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

			var arguments = new Dictionary<string, object>();
			arguments.Add("outputPath", CallGraphPath);

			var response = this.RunWebAPICommandAsync(arguments).Result;

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
				//result = RunGitCommand(solutionFolder, "diff --name-only origin/orleansdemo");

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
			//var solutionFolder = Path.GetDirectoryName(solutionPath);
			//var modifiedDocuments = gitDiffOutput.Split('\n')
			//									 .Where(docPath => !string.IsNullOrEmpty(docPath))
			//									 .Select(docPath => docPath.Replace("/", @"\"))
			//									 .Select(docPath => Path.Combine(solutionFolder, docPath))
			//									 .ToList();

			Console.WriteLine("Starting incremental analysis...");

			var arguments = new Dictionary<string, object>();
			arguments.Add("gitDiffOutput", gitDiffOutput);

			var response = this.RunWebAPICommandAsync(arguments).Result;

			Console.WriteLine("Incremental analysis finish");
		}

		private async Task<string> RunWebAPICommandAsync(IDictionary<string, object> arguments)
		{
			var argumentsString = new StringBuilder();
			string response = null;

			if (arguments.Count > 0)
			{
				foreach (var argument in arguments)
				{
					var value = Convert.ToString(argument.Value);
					value = HttpUtility.UrlEncode(value);

					argumentsString.AppendFormat("&{0}={1}", argument.Key, value);
				}

				argumentsString.Remove(0, 1);
				argumentsString.Insert(0, '?');
            }

			var address = string.Format("{0}{1}", this.baseAddress, argumentsString);

			using (var client = new WebClient())
			{
				var data = await client.OpenReadTaskAsync(address);

				using (var reader = new StreamReader(data))
				{
					response = await reader.ReadToEndAsync();
				}
			}

			return response;
		}
	}
}
