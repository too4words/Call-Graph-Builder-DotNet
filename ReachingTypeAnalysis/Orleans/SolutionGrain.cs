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
    public interface ISolutionState : IGrainState
    {
        string SolutionFullPath { get; set; }
        string SourceCode { get; set; }
    }

    [StorageProvider(ProviderName = "TestStore")]
    public class SolutionGrain : Grain<ISolutionState>, ISolutionGrain
    {
        [NonSerialized]
        private Dictionary<MethodDescriptor, IProjectCodeProviderGrain> MethodDescriptors;
        [NonSerialized]
        private Microsoft.CodeAnalysis.Solution solution;

        public override Task OnActivateAsync()
        {
            MethodDescriptors = new Dictionary<MethodDescriptor, IProjectCodeProviderGrain>();

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
            return this.State.WriteStateAsync();
        }

        public Task SetSolutionSource(string solutionSource)
        {
            this.State.SourceCode = solutionSource;
            this.solution = Utils.CreateSolution(solutionSource);
            return this.State.WriteStateAsync();
        }

        public async Task<IProjectCodeProviderGrain> GetCodeProviderAsync(MethodDescriptor methodDescriptor)
        {

            IProjectCodeProviderGrain projectCodeProviderGrain;
            if (this.MethodDescriptors.TryGetValue(methodDescriptor, out projectCodeProviderGrain))
            {
                return projectCodeProviderGrain;
            }
            else
            {
                projectCodeProviderGrain = await ProjectCodeProvider.GetCodeProviderGrainAsync(methodDescriptor, this.solution);
                this.MethodDescriptors.Add(methodDescriptor, projectCodeProviderGrain);
                await this.State.WriteStateAsync();
            }
			
            return projectCodeProviderGrain;
        }

        public Task<IEnumerable<MethodDescriptor>> GetRoots()
        {
            return ProjectCodeProvider.GetMainMethodsAsync(this.solution);
        }
    }
}
