using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Orleans.Providers;
using OrleansInterfaces;
using ReachingTypeAnalysis.Roslyn;

namespace ReachingTypeAnalysis
{
    public interface ProjectState : Orleans.IGrainState
    {
        string FullPath { get; set; }        
    }

    [StorageProvider(ProviderName = "TestStore")]
    public class ProjectCodeProviderGrain : Orleans.Grain<ProjectState>, IProjectCodeProviderGrain
    {
        private ProjectCodeProvider provider;

        public override async Task OnActivateAsync()
        {
            if (provider == null)
            {
                Contract.Assert(this.State != null);
                var fullPath = this.State.FullPath;
                Contract.Assert(fullPath != null);
                this.provider = await ProjectCodeProvider.ProjectCodeProviderAsync(fullPath);
            }
        }

        public Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
        {
            Contract.Assert(this.provider != null);

            return this.provider.IsSubtypeAsync(typeDescriptor1, typeDescriptor2);
        }
    }
}
