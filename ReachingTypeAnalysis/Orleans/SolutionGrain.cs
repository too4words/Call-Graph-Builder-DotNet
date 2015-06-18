using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.CodeGeneration;
using Orleans.Providers;
using OrleansInterfaces;
using ReachingTypeAnalysis.Roslyn;
using System.Collections.Generic;

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
		List<MethodDescriptor> MethodDescriptors { get; set; }
        [NonSerialized]
        private Microsoft.CodeAnalysis.Solution solution;

        public override Task OnActivateAsync()
        {
			MethodDescriptors = new List<MethodDescriptor>();		

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
            var projectCodeProviderGrain = await ProjectCodeProvider.GetCodeProviderGrainAsync(methodDescriptor, this.solution);

			if (!MethodDescriptors.Contains(methodDescriptor))
			{
				MethodDescriptors.Add(methodDescriptor);
			}

			await this.State.WriteStateAsync();
            return projectCodeProviderGrain;
        }
		public async Task<IList<MethodDescriptor>> GetMethodDescriptors()
		{
			return MethodDescriptors;
		}
    }
}
