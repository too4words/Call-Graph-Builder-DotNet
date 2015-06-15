﻿using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.CodeGeneration;
using Orleans.Providers;
using OrleansInterfaces;
using ReachingTypeAnalysis.Roslyn;

namespace ReachingTypeAnalysis.Analysis
{
    public interface ISolutionState : IGrainState
    {
        string SolutionFullPath { get; set; }
    }

    [StorageProvider(ProviderName = "TestStore")]
    public class SolutionGrain : Grain<ISolutionState>, ISolutionGrain
    {
        [NonSerialized]
        private Microsoft.CodeAnalysis.Solution solution;

        public override Task OnActivateAsync()
        {
            if (this.State.SolutionFullPath != null)
            {
                this.solution = Utils.CreateSolution(this.State.SolutionFullPath);
            }

            return TaskDone.Done;
        }

        public Task SetSolution(string solutionPath)
        {
            this.State.SolutionFullPath = solutionPath;
            this.solution = Utils.CreateSolution(solutionPath);
            return this.State.WriteStateAsync();
        }

        public async Task<IProjectCodeProviderGrain> GetCodeProviderAsync(MethodDescriptor methodDescriptor)
        {
            var projectCodeProviderGrain = await ProjectCodeProvider.GetCodeProviderGrainAsync(methodDescriptor, this.solution);
            return projectCodeProviderGrain;
        }
    }
}
