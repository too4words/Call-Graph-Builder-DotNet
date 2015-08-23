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
		//protected Solution solution;
		protected string solutionPath;
		protected IList<Project> projects;
        protected ISet<TypeDescriptor> instantiatedTypes;

		protected SolutionManager()
		{
			this.instantiatedTypes = new HashSet<TypeDescriptor>();
		}

		protected Task LoadSolutionAsync(string solutionPath)
		{
			var tasks = new List<Task>();
			var solution = Utils.ReadSolution(solutionPath);

			this.solutionPath = solutionPath;
			this.projects = Utils.FilterProjects(solution);

			var projectsCount = this.projects.Count;
			var currentProjectNumber = 1;

			foreach (var project in this.projects)
            {
				Console.WriteLine("Compiling project {0} ({1} of {2})", project.Name, currentProjectNumber++, projectsCount);

				var task = this.CreateProjectCodeProviderAsync(project.FilePath, project.AssemblyName);
				//await task;
				tasks.Add(task);
            }

			return Task.WhenAll(tasks);
		}

		protected Task LoadSourceAsync(string source)
		{
			var solution = Utils.CreateSolution(source);
			this.projects = Utils.FilterProjects(solution);

			return this.CreateProjectCodeProviderFromSourceAsync(source, TestConstants.ProjectAssemblyName);
		}

		protected Task LoadTestAsync(string testName)
		{
			var source = TestSources.BasicTestsSources.Test[testName];
			var solution = Utils.CreateSolution(source);
			this.projects = Utils.FilterProjects(solution);

			return this.CreateProjectCodeProviderFromTestAsync(testName, TestConstants.ProjectAssemblyName);
		}

		protected abstract Task CreateProjectCodeProviderAsync(string projectFilePath, string assemblyName);
		protected abstract Task CreateProjectCodeProviderFromSourceAsync(string source, string assemblyName);
		protected abstract Task CreateProjectCodeProviderFromTestAsync(string testName, string assemblyName);

        public async Task<IEnumerable<MethodDescriptor>> GetRootsAsync()
        {
			var cancellationTokenSource = new CancellationTokenSource();
			var result = new List<MethodDescriptor>();

            foreach (var project in this.projects)
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

			foreach (var project in this.projects)
			{
				var provider = await this.GetProjectCodeProviderAsync(project.AssemblyName);
				result.Add(provider);
			}

			return result;
		}

		public abstract Task<IProjectCodeProvider> GetProjectCodeProviderAsync(string assemblyName);

		public abstract Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor);

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

		public Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			// TODO: Who returns the modifications for newly added documents?
			// Think the case of new projects added: there are no providers
			// for them but we need to return the newly added methods.
			throw new NotImplementedException();
		}

		public async Task ReloadAsync()
		{
			var tasks = new List<Task>();
			var existingProjectNames = this.projects.Select(p => p.Name).ToList();
			var solution = Utils.ReadSolution(this.solutionPath);
			this.projects = Utils.FilterProjects(solution);

			var projectsCount = this.projects.Count;
			var currentProjectNumber = 1;

			foreach (var project in this.projects)
			{
				Task task = null;

				if (existingProjectNames.Contains(project.Name))
				{
					var provider = await this.GetProjectCodeProviderAsync(project.AssemblyName);
					//task = provider.ReloadAsync();
				}
				else
				{
					Console.WriteLine("Compiling project {0} ({1} of {2})", project.Name, currentProjectNumber++, projectsCount);

					task = this.CreateProjectCodeProviderAsync(project.FilePath, project.AssemblyName);
				}

				//await task;
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
		}
	}
}
