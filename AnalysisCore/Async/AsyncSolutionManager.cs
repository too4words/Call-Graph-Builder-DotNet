using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReachingTypeAnalysis.Roslyn;
using Microsoft.CodeAnalysis;
using System.Linq;
using Orleans;
using ReachingTypeAnalysis;

using AssemblyName = System.String;
using System.Threading;

namespace ReachingTypeAnalysis.Analysis
{
	internal class AsyncSolutionManager : SolutionManager
	{
		private IDictionary<AssemblyName, IProjectCodeProvider> projectsCache;

		private AsyncSolutionManager()
		{
			this.projectsCache = new Dictionary<AssemblyName, IProjectCodeProvider>();
		}

		public static async Task<AsyncSolutionManager> CreateFromSolutionAsync(string solutionPath)
		{
			var manager = new AsyncSolutionManager();
			await manager.LoadSolutionAsync(solutionPath);
			return manager;
		}

		public static async Task<AsyncSolutionManager> CreateFromSourceAsync(string source)
		{
			var manager = new AsyncSolutionManager();
			await manager.LoadSourceAsync(source);
			return manager;
		}

		protected override async Task CreateProjectCodeProviderAsync(string projectPath, string assemblyName)
		{
			var provider = await AsyncProjectCodeProvider.CreateFromProjectAsync(projectPath);

			if (projectsCache.ContainsKey(assemblyName))
			{
				var message = string.Format("Same assembly name used in more than one project: {0}", assemblyName);
				Console.WriteLine(message);
				return;
				//throw new Exception(message);
			}

			projectsCache.Add(assemblyName, provider);
		}

		protected override async Task CreateProjectCodeProviderFromSourceAsync(string source, string assemblyName)
		{
			var provider = await AsyncProjectCodeProvider.CreateFromSourceAsync(source, assemblyName);

			if (projectsCache.ContainsKey(assemblyName))
			{
				var message = string.Format("Same assembly name used in more than one project: {0}", assemblyName);
				Console.WriteLine(message);
				return;
				//throw new Exception(message);
			}

			projectsCache.Add(assemblyName, provider);
		}

		public override Task<IProjectCodeProvider> GetProjectCodeProviderAsync(string assemblyName)
		{
			IProjectCodeProvider provider = null;

			if (!projectsCache.TryGetValue(assemblyName, out provider))
			{
				provider = this.GetDummyProjectCodeProvider();
				projectsCache.Add(assemblyName, provider);
			}

			return Task.FromResult(provider);
		}

		private IProjectCodeProvider GetDummyProjectCodeProvider()
		{
			var provider = new AsyncDummyProjectCodeProvider();
			return provider;
		}
	}
}
