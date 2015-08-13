using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReachingTypeAnalysis.Roslyn;
using Microsoft.CodeAnalysis;
using System.Linq;
using Orleans;
using ReachingTypeAnalysis;
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

		protected Task LoadSolutionAsync(string solutionPath)
		{
			var tasks = new List<Task>();
			var cancellationTokenSource = new CancellationTokenSource();
			this.solution = Utils.ReadSolution(solutionPath);

			var projectsCount = this.solution.ProjectIds.Count;
			var currentProjectNumber = 1;

			foreach (var project in solution.Projects)
            {
				Console.WriteLine("Compiling project {0} ({1} of {2})", project.Name, currentProjectNumber++, projectsCount);

				var task = this.CreateProjectCodeProviderAsync(project.FilePath, project.AssemblyName);
				tasks.Add(task);
            }

			return Task.WhenAll(tasks);
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
}
