// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using OrleansInterfaces;
using ReachingTypeAnalysis.Roslyn;
using Microsoft.CodeAnalysis;
using System.Linq;

using AssemblyName = System.String;
using System.Threading;


namespace ReachingTypeAnalysis.Analysis
{
    // TODO: Add instantiated types
    public interface ISolutionState : IGrainState
    {
        string SolutionFullPath { get; set; }
        string SourceCode { get; set; }
    }

    //[StorageProvider(ProviderName = "FileStore")]
    [StorageProvider(ProviderName = "MemoryStore")]
    public class SolutionGrain : Grain<ISolutionState>, ISolutionGrain
    {
        //[NonSerialized]
        //private Dictionary<MethodDescriptor, IProjectCodeProviderGrain> methodDescriptors2Project;
        //private ISet<TypeDescriptor> instantiadtedTypes;
        //[NonSerialized]
        //private Microsoft.CodeAnalysis.Solution solution;
        [NonSerialized]
        ISolutionManager solutionManager;
        [NonSerialized]
        IAnalysisStrategy strategy;

        public override  async Task OnActivateAsync()
        {
            //methodDescriptors2Project = new Dictionary<MethodDescriptor, IProjectCodeProviderGrain>();
            //instantiadtedTypes = new HashSet<TypeDescriptor>();

            //if (this.State.SolutionFullPath != null)
            //{
            //    this.solution = Utils.ReadSolution(this.State.SolutionFullPath);
            //}
            //else
            //{
            //    if (this.State.SourceCode != null)
            //    {
            //        this.solution = Utils.CreateSolution(this.State.SourceCode);
            //    }

            //}

            Logger.Log(this.GetLogger(), "SolGrain", "OnActivate","");

            strategy = new OnDemandOrleansStrategy(this.GrainFactory);

            if (this.State.SolutionFullPath != null)
            {
                this.solutionManager = await SolutionManager.CreateFromSolution(strategy, this.State.SolutionFullPath);
            }
            else if(this.State.SourceCode != null)
            {
                this.solutionManager = await SolutionManager.CreateFromSourceCode(strategy, this.State.SourceCode);
            }

        }

        public async Task SetSolutionPath(string solutionPath)
        {
            this.State.SolutionFullPath = solutionPath;
            var solution = Utils.ReadSolution(solutionPath);
            this.solutionManager = await SolutionManager.CreateFromSolution(strategy, this.State.SolutionFullPath);
            //this.solutionManager = new SolutionManager(strategy, solution);
            
            await this.WriteStateAsync();
        }

        public async Task SetSolutionSource(string solutionSource)
        {
            Logger.Log(this.GetLogger(), "SolGrain", "SetSolSource", "");
            this.State.SourceCode = solutionSource;
            this.solutionManager = await SolutionManager.CreateFromSourceCode(strategy, this.State.SourceCode);

            //var solution = Utils.CreateSolution(solutionSource);
            //var solutionManager = new SolutionManager(strategy, solution);

            //var projectGrain = this.GrainFactory.GetGrain<IProjectCodeProviderGrain>("MyProject");
            //await projectGrain.SetProjectSourceCode(solutionSource);
            //solutionManager.AddToCache("MyProject", projectGrain);

            //this.solutionManager = solutionManager;
            

            await this.WriteStateAsync();
        }

        
        //public Task<IProjectCodeProvider> GetCodeProviderAsync(MethodDescriptor methodDescriptor)
        //{
        //    //IProjectCodeProviderGrain projectCodeProviderGrain;
        //    //if (this.methodDescriptors2Project.TryGetValue(methodDescriptor, out projectCodeProviderGrain))
        //    //{
        //    //    return projectCodeProviderGrain;
        //    //}
        //    //else
        //    //{
        //    //    projectCodeProviderGrain = await ProjectCodeProvider.GetCodeProviderGrainAsync(methodDescriptor, this.solution, GrainFactory);
        //    //    this.methodDescriptors2Project.Add(methodDescriptor, projectCodeProviderGrain);
        //    //    await TaskDone.Done; //this.State.WriteStateAsync();
        //    //}
			
        //    //return projectCodeProviderGrain;
        //}

        public Task<IEnumerable<IProjectCodeProvider>> GetProjectsAsync()
        {
            return this.solutionManager.GetProjectsAsync();
        }

        public Task<IProjectCodeProvider> GetProjectCodeProviderAsync(MethodDescriptor methodDescriptor)
        {
            return this.solutionManager.GetProjectCodeProviderAsync(methodDescriptor);
        }
        public Task AddInstantiatedTypesAsync(IEnumerable<TypeDescriptor> types)
        {
            return solutionManager.AddInstantiatedTypesAsync(types);
            //instantiadtedTypes.UnionWith(types);
            //await this.WriteStateAsync();
        }

        public Task<ISet<TypeDescriptor>> GetInstantiatedTypesAsync()
        {
            return this.solutionManager.GetInstantiatedTypesAsync();
            // return await Task.FromResult(instantiadtedTypes);
        }

        public Task<IEnumerable<MethodDescriptor>> GetRootsAsync()
        {
            return this.solutionManager.GetRootsAsync();
            //return ProjectCodeProvider.GetMainMethodsAsync(this.solution);
        }
    }
    internal class SolutionManager : ISolutionManager
    {
        internal Solution Solution { get; private set; }
        private IAnalysisStrategy strategy;


        private ISet<TypeDescriptor> instantiatedTypes = new HashSet<TypeDescriptor>();

        private IDictionary<AssemblyName, IProjectCodeProvider> projectsCache = new Dictionary<AssemblyName, IProjectCodeProvider>();

        internal SolutionManager(IAnalysisStrategy strategy, Solution solution)
        {
            this.Solution = solution;
            this.strategy = strategy;
        }

        private SolutionManager(IAnalysisStrategy strategy, Solution solution, string sourceCode)
        {
            this.Solution = solution;
            this.strategy = strategy;
        }
        public static async Task<SolutionManager> CreateFromSolution(IAnalysisStrategy strategy, string solutionPath)
        {
            var solution = Utils.ReadSolution(solutionPath);
            var solutionManager = new SolutionManager(strategy, solution);
            await solutionManager.GenerateProjectProviders();
            return solutionManager;
        }
        public static async Task<SolutionManager> CreateFromSourceCode(IAnalysisStrategy strategy, string source)
        {
            var solution = Utils.CreateSolution(source);
            var solutionManager = new SolutionManager(strategy, solution, source);
            await solutionManager.GenerateProjectProviders(source);
            return solutionManager;
        }

        internal async Task GenerateProjectProviders()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            foreach (var project in Solution.Projects)
            {
                var codeProvider = await this.strategy.CreateProjectCodeProviderAsync(project.FilePath, project.Name);
                AddToCache(project.AssemblyName, codeProvider);
            }
        }

        internal void AddToCache(string assemblyName, IProjectCodeProvider codeProvider)
        {
            projectsCache.Add(assemblyName, codeProvider);
        }

        internal async Task GenerateProjectProviders(string sourceCode)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var codeProvider = await this.strategy.CreateProjectCodeFromSourceAsync(sourceCode, "MyProject");
            AddToCache("MyProject", codeProvider);
        }

        public Task<IEnumerable<MethodDescriptor>> GetRootsAsync()
        {
            // TODO: Iterate for each project to obtain roots
            return ProjectCodeProvider.GetMainMethodsAsync(Solution);
        }

        public Task<IEnumerable<IProjectCodeProvider>> GetProjectsAsync()
        {
            return Task.FromResult(this.projectsCache.Values.AsEnumerable());
        }

        public async Task<IProjectCodeProvider> GetProjectCodeProviderAsync(MethodDescriptor methodDescriptor)
        {
            var typeDescriptor = methodDescriptor.ContainerType;
            var assemblyName = typeDescriptor.AssemblyName;
            IProjectCodeProvider codeProvider = null;
            if (projectsCache.TryGetValue(assemblyName, out codeProvider))
            {
                return codeProvider;
            }
            codeProvider = await strategy.GetDummyProjectCodeProviderAsync();
            projectsCache.Add(assemblyName, codeProvider);
            return codeProvider;
        }

        /// <summary>
        /// For RTA analysis
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        public Task AddInstantiatedTypesAsync(IEnumerable<TypeDescriptor> types)
        {
            instantiatedTypes.UnionWith(types);
            return TaskDone.Done;
        }
        public Task<ISet<TypeDescriptor>> GetInstantiatedTypesAsync()
        {
            return Task.FromResult(instantiatedTypes);
        }
    }
    
}
