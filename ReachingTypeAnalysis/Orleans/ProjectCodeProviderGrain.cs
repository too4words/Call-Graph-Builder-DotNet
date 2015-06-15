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
        private ProjectCodeProvider projectCodeProvider;

        public override async Task OnActivateAsync()
        {
            //Contract.Assert(this.State != null);
			if (this.State.FullPath != null)
			{
				this.projectCodeProvider = await ProjectCodeProvider.ProjectCodeProviderAsync(this.State.FullPath);
			}
        }

        public Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        {
            Contract.Assert(this.projectCodeProvider != null);
            return this.projectCodeProvider.IsSubtypeAsync(typeDescriptor1, typeDescriptor2);
        }

        public Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
        {
			Contract.Assert(this.projectCodeProvider != null);
            var methodImplementationDescriptor = this.projectCodeProvider.FindMethodImplementation(methodDescriptor, typeDescriptor);
            return Task.FromResult<MethodDescriptor>(methodImplementationDescriptor);
        }

		public async Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			Contract.Assert(this.projectCodeProvider != null);
			var methodEntity = await ProjectCodeProvider.CreateMethodEntityAsync(methodDescriptor);
			return methodEntity;
		}
    }
}
