// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using OrleansInterfaces;
using ReachingTypeAnalysis.Roslyn;

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
        [NonSerialized]
        private Dictionary<MethodDescriptor, IProjectCodeProviderGrain> methodDescriptors2Project;
        private ISet<TypeDescriptor> instantiadtedTypes;
        [NonSerialized]
        private Microsoft.CodeAnalysis.Solution solution;

        public override Task OnActivateAsync()
        {
            methodDescriptors2Project = new Dictionary<MethodDescriptor, IProjectCodeProviderGrain>();
            instantiadtedTypes = new HashSet<TypeDescriptor>();
			//if (this.State.MethodDescriptors == null)
			//{
			//	this.State.MethodDescriptors = new List<MethodDescriptor>();
			//}
            if (this.State.SolutionFullPath != null)
            {
                this.solution = Utils.ReadSolution(this.State.SolutionFullPath);
            }
            else
            {
                if (this.State.SourceCode != null)
                {
                    this.solution = Utils.CreateSolution(this.State.SourceCode);
                }

            }
            return TaskDone.Done;
        }

        public Task SetSolutionPath(string solutionPath)
        {
            this.State.SolutionFullPath = solutionPath;
            this.solution = Utils.ReadSolution(solutionPath);
            return this.WriteStateAsync();
        }

        public Task SetSolutionSource(string solutionSource)
        {
            this.State.SourceCode = solutionSource;
            this.solution = Utils.CreateSolution(solutionSource);
            return this.WriteStateAsync();
        }

        public async Task<IProjectCodeProviderGrain> GetCodeProviderAsync(MethodDescriptor methodDescriptor)
        {

            IProjectCodeProviderGrain projectCodeProviderGrain;
            if (this.methodDescriptors2Project.TryGetValue(methodDescriptor, out projectCodeProviderGrain))
            {
                return projectCodeProviderGrain;
            }
            else
            {
                projectCodeProviderGrain = await ProjectCodeProvider.GetCodeProviderGrainAsync(methodDescriptor, this.solution, GrainFactory);
                this.methodDescriptors2Project.Add(methodDescriptor, projectCodeProviderGrain);
                await TaskDone.Done; //this.State.WriteStateAsync();
            }
			
            return projectCodeProviderGrain;
        }
        public async Task AddInstantiatedTypes(IEnumerable<TypeDescriptor> types)
        {
            instantiadtedTypes.UnionWith(types);
            await this.WriteStateAsync();
        }
        public async Task<ISet<TypeDescriptor>>  InstantiatedTypes()
        {
            return await Task.FromResult(instantiadtedTypes);
        }

        public Task<IEnumerable<MethodDescriptor>> GetRoots()
        {
            return ProjectCodeProvider.GetMainMethodsAsync(this.solution);
        }
    }
    public class SolutionGrainWrapper : ISolution
    {
        ISolutionGrain grainRef;
        public SolutionGrainWrapper(ISolutionGrain grainRef)
        {
            this.grainRef = grainRef;
        }
        public Task<IEnumerable<MethodDescriptor>> GetRoots()
        {
            return this.grainRef.GetRoots();
        }
        public Task<ISet<TypeDescriptor>>  InstantiatedTypes()
        {
            return this.grainRef.InstantiatedTypes();
        }
        public Task AddInstantiatedTypes(IEnumerable<TypeDescriptor> types)
        {
            return this.grainRef.AddInstantiatedTypes(types);
        }
    }

}
