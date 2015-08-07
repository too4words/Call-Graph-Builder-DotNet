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
    internal abstract class SolutionManager : ISolutionManager
    {
        protected Solution solution;
        protected ISet<TypeDescriptor> instantiatedTypes;

		protected SolutionManager()
		{
			this.instantiatedTypes = new HashSet<TypeDescriptor>();
		}

		protected async Task LoadSolutionAsync(string solutionPath)
		{
			var cancellationTokenSource = new CancellationTokenSource();
			this.solution = Utils.ReadSolution(solutionPath);

            foreach (var project in solution.Projects)
            {
				await this.CreateProjectCodeProviderAsync(project.FilePath, project.AssemblyName);
            }
		}

		protected Task LoadSourceAsync(string source)
		{
			this.solution = Utils.CreateSolution(source);
			return this.CreateProjectCodeProviderFromSourceAsync(source, "MyProject");
		}

		protected abstract Task CreateProjectCodeProviderAsync(string projectFilePath, string assemblyName);
		protected abstract Task CreateProjectCodeProviderFromSourceAsync(string source, string assemblyName);

        public async Task<IEnumerable<MethodDescriptor>> GetRootsAsync()
        {
			var cancellationTokenSource = new CancellationTokenSource();
			var result = new List<MethodDescriptor>();

            foreach (var project in this.solution.Projects)
            {
				var provider = await this.GetProjectCodeProviderAsync(project.AssemblyName);
				var roots = await provider.GetRootsAsync();

				foreach (var root in roots)
				{
					result.Add(root);
				}
			}

			return result;
        }

		public async Task<IEnumerable<IProjectCodeProvider>> GetProjectCodeProvidersAsync()
		{
			var cancellationTokenSource = new CancellationTokenSource();
			var result = new List<IProjectCodeProvider>();

			foreach (var project in this.solution.Projects)
			{
				var provider = await this.GetProjectCodeProviderAsync(project.AssemblyName);
				result.Add(provider);
			}

			return result;
		}

		public abstract Task<IProjectCodeProvider> GetProjectCodeProviderAsync(string assemblyName);

		public async Task<IProjectCodeProvider> GetProjectCodeProviderAsync(MethodDescriptor methodDescriptor)
		{
			var typeDescriptor = methodDescriptor.ContainerType;
			var assemblyName = typeDescriptor.AssemblyName;
			var provider = await this.GetProjectCodeProviderAsync(assemblyName);

			return provider;
		}

        /// <summary>
        /// For RTA analysis
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        public Task AddInstantiatedTypesAsync(IEnumerable<TypeDescriptor> types)
        {
            this.instantiatedTypes.UnionWith(types);
			return TaskDone.Done;
        }

		/// <summary>
		/// For RTA analysis
		/// </summary>
		/// <param name="types"></param>
		/// <returns></returns>
        public Task<ISet<TypeDescriptor>> GetInstantiatedTypesAsync()
        {
			return Task.FromResult(instantiatedTypes);
        }
    }

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

		protected override async Task CreateProjectCodeProviderAsync(string projectFilePath, string assemblyName)
		{
			var provider = await ProjectCodeProvider.ProjectCodeProviderAsync(projectFilePath);
			projectsCache.Add(assemblyName, provider);
		}

		protected override async Task CreateProjectCodeProviderFromSourceAsync(string source, string assemblyName)
		{
			var provider = await ProjectCodeProvider.ProjectCodeProviderByNameAsync(this.solution, assemblyName);
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
			var provider = new DummyCodeProvider();
			return provider;
		}
    }

}
