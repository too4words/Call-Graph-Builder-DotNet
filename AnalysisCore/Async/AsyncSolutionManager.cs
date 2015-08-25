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
		private IDictionary<AssemblyName, IProjectCodeProvider> newProjectsCache;

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

		public static async Task<AsyncSolutionManager> CreateFromTestAsync(string testName)
		{
			var manager = new AsyncSolutionManager();
			await manager.LoadTestAsync(testName);
			return manager;
		}

		private IDictionary<AssemblyName, IProjectCodeProvider> ProjectsCache
		{
			get { return useNewFieldsVersion ? newProjectsCache : projectsCache; }
		}

		protected override async Task CreateProjectCodeProviderAsync(string projectPath, string assemblyName)
		{
			if (this.ProjectsCache.ContainsKey(assemblyName))
			{
				var message = string.Format("Same assembly name used in more than one project: {0}", assemblyName);
				Console.WriteLine(message);
				return;
				//throw new Exception(message);
			}

			var provider = await AsyncProjectCodeProvider.CreateFromProjectAsync(projectPath);
			this.ProjectsCache.Add(assemblyName, provider);
		}

		protected override async Task CreateProjectCodeProviderFromSourceAsync(string source, string assemblyName)
		{
			if (this.ProjectsCache.ContainsKey(assemblyName))
			{
				var message = string.Format("Same assembly name used in more than one project: {0}", assemblyName);
				Console.WriteLine(message);
				return;
				//throw new Exception(message);
			}

			var provider = await AsyncProjectCodeProvider.CreateFromSourceAsync(source, assemblyName);
			this.ProjectsCache.Add(assemblyName, provider);
		}

		protected override async Task CreateProjectCodeProviderFromTestAsync(string testName, string assemblyName)
		{
			if (this.ProjectsCache.ContainsKey(assemblyName))
			{
				var message = string.Format("Same assembly name used in more than one project: {0}", assemblyName);
				Console.WriteLine(message);
				return;
				//throw new Exception(message);
			}

			var provider = await AsyncProjectCodeProvider.CreateFromTestAsync(testName, assemblyName);
			this.ProjectsCache.Add(assemblyName, provider);
		}

		public override Task<IProjectCodeProvider> GetProjectCodeProviderAsync(string assemblyName)
		{
			IProjectCodeProvider provider = null;

			if (!this.ProjectsCache.TryGetValue(assemblyName, out provider))
			{
				provider = this.GetDummyProjectCodeProvider();
				this.ProjectsCache.Add(assemblyName, provider);
			}

			return Task.FromResult(provider);
		}

		private IProjectCodeProvider GetDummyProjectCodeProvider()
		{
			var provider = new AsyncDummyProjectCodeProvider();
			return provider;
		}

		public override async Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			var projectProvider = await this.GetProjectCodeProviderAsync(methodDescriptor);
			var methodEntity = await projectProvider.GetMethodEntityAsync(methodDescriptor);

			return methodEntity;
		}

		public override Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			this.newProjectsCache = new Dictionary<AssemblyName, IProjectCodeProvider>(this.ProjectsCache);
			return base.GetModificationsAsync(modifiedDocuments);
		}

		public override async Task ReloadAsync()
		{
			if (newProjectsCache != null)
			{
				this.projectsCache = newProjectsCache;
				this.newProjectsCache = null;
			}

			await base.ReloadAsync();
		}
    }
}
