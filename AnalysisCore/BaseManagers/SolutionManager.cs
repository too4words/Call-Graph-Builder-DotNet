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
		private IList<Project> projects;
		private IList<Project> newProjects;
		protected ISet<TypeDescriptor> instantiatedTypes;
		protected bool useNewFieldsVersion;

		protected SolutionManager()
		{
			this.instantiatedTypes = new HashSet<TypeDescriptor>();
		}

		protected IList<Project> Projects
		{
			get { return useNewFieldsVersion ? newProjects : projects; }
		}

		protected async Task LoadSolutionAsync(string solutionPath)
		{
			var tasks = new List<Task>();
			var solution = await Utils.ReadSolutionAsync(solutionPath);

			this.solutionPath = solutionPath;
			this.projects = Utils.FilterProjects(solution);

			var projectsCount = this.Projects.Count;
			var currentProjectNumber = 1;

			foreach (var project in this.Projects)
            {
				Console.WriteLine("Compiling project {0} ({1} of {2})", project.Name, currentProjectNumber++, projectsCount);

				var task = this.CreateProjectCodeProviderAsync(project.FilePath, project.AssemblyName);
				//await task;
				tasks.Add(task);
            }

			await Task.WhenAll(tasks);
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

            foreach (var project in this.Projects)
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

			foreach (var project in this.Projects)
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

		public virtual async Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			var tasks = new List<Task<IEnumerable<MethodModification>>>();
			var existingProjectNames = this.Projects.Select(p => p.Name).ToList();
			var solution = await Utils.ReadSolutionAsync(this.solutionPath);

			this.newProjects = Utils.FilterProjects(solution);
			this.useNewFieldsVersion = true;

			var newProjectsCount = newProjects.Count(p => !existingProjectNames.Contains(p.Name));
			var currentProjectNumber = 1;			

			foreach (var project in newProjects)
			{
				var isNewProject = !existingProjectNames.Contains(project.Name);

				if (isNewProject)
				{
					Console.WriteLine("Compiling project {0} ({1} of {2})", project.Name, currentProjectNumber++, newProjectsCount);

					await this.CreateProjectCodeProviderAsync(project.FilePath, project.AssemblyName);
				}

				var provider = await this.GetProjectCodeProviderAsync(project.AssemblyName);
				var task = provider.GetModificationsAsync(modifiedDocuments);
				//await task;
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
			this.useNewFieldsVersion = false;

			var result = tasks.SelectMany(t => t.Result).ToList();
			return result;
		}

		public virtual async Task ReloadAsync()
		{
			if (newProjects != null)
			{
				var tasks = new List<Task>();

				this.projects = newProjects;
				this.newProjects = null;

				foreach (var project in this.Projects)
				{
					var provider = await this.GetProjectCodeProviderAsync(project.AssemblyName);
					var task = provider.ReloadAsync();
					//await task;
					tasks.Add(task);
				}

				await Task.WhenAll(tasks);
			}
        }
	}
}
