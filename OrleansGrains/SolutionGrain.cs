// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using OrleansInterfaces;



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
				this.solutionManager = await strategy.CreateFromSolutionAsync(this.State.SolutionFullPath);
            }
            else if(this.State.SourceCode != null)
            {
				this.solutionManager = await strategy.CreateFromSourceAsync(this.State.SourceCode);
            }

        }

        public async Task SetSolutionPath(string solutionPath)
        {
			Logger.Log(this.GetLogger(), "SolGrain", "SetSolution", "Enter");

            this.State.SolutionFullPath = solutionPath;
			this.solutionManager = await strategy.CreateFromSolutionAsync(this.State.SolutionFullPath);

			await this.WriteStateAsync();
			Logger.Log(this.GetLogger(), "SolGrain", "SetSolution", "Exit");
        }

        public async Task SetSolutionSource(string solutionSource)
        {
            Logger.Log(this.GetLogger(), "SolGrain", "SetSolSource", "");
            this.State.SourceCode = solutionSource;
			this.solutionManager = await strategy.CreateFromSourceAsync(this.State.SourceCode);
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
			Logger.Log(this.GetLogger(), "SolGrain", "GetRoots", "Enter");
            return this.solutionManager.GetRootsAsync();
            //return ProjectCodeProvider.GetMainMethodsAsync(this.solution);
			Logger.Log(this.GetLogger(), "SolGrain", "GetRoots", "Exit");
        }
    }
    
}
