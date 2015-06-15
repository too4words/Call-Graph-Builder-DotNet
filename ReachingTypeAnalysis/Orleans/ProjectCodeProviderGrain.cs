using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using System;
using Orleans;
using Orleans.Providers;
using OrleansInterfaces;
using ReachingTypeAnalysis.Roslyn;

namespace ReachingTypeAnalysis.Analysis
{
    public interface IProjectState : IGrainState
    {
        string FullPath { get; set; }
    }

    [StorageProvider(ProviderName = "TestStore")]
    public class ProjectCodeProviderGrain : Grain<IProjectState>, IProjectCodeProviderGrain
    {
        [NonSerialized]
        private ProjectCodeProvider provider;
        public override Task OnActivateAsync()
        {
            //Contract.Assert(this.State != null);
            //var fullPath = this.State.FullPath;
            //Contract.Assert(fullPath != null);
            //this.provider = await ProjectCodeProvider.ProjectCodeProviderAsync(fullPath);
            return TaskDone.Done;
        }

        public Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        {
            Contract.Assert(this.provider != null);
            return this.provider.IsSubtypeAsync(typeDescriptor1, typeDescriptor2);
        }
        public Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
        {
            var methodImplementationDescriptor = this.provider.FindMethodImplementation(methodDescriptor, typeDescriptor);
            if(methodImplementationDescriptor==null)
            {
                // That means is not 
            }
            return Task.FromResult<MethodDescriptor>(methodImplementationDescriptor);
        }


    }
}
