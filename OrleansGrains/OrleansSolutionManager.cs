using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReachingTypeAnalysis.Roslyn;
using System.Linq;
using Orleans;
using ReachingTypeAnalysis;

using AssemblyName = System.String;
using System.Threading;
using ReachingTypeAnalysis.Analysis;
using OrleansInterfaces;

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ReachingTypeAnalysis")]
namespace ReachingTypeAnalysis.Analysis
{
    internal class OrleansSolutionManager : SolutionManager
    {
        private IGrainFactory grainFactory;

        private OrleansSolutionManager(IGrainFactory grainFactory)
        {
            this.grainFactory = grainFactory;
        }

        public static async Task<OrleansSolutionManager> CreateFromSolutionAsync(IGrainFactory grainFactory, string solutionPath)
        {
            var manager = new OrleansSolutionManager(grainFactory);
            await manager.LoadSolutionAsync(solutionPath);
            return manager;
        }

        public static async Task<OrleansSolutionManager> CreateFromSourceAsync(IGrainFactory grainFactory, string source)
        {
            var manager = new OrleansSolutionManager(grainFactory);
            await manager.LoadSourceAsync(source);
            return manager;
        }

        protected override Task CreateProjectCodeProviderAsync(string projectFilePath, string assemblyName)
        {
            var projectGrain = grainFactory.GetGrain<IProjectCodeProviderGrain>(assemblyName);
            return projectGrain.SetProjectPath(projectFilePath);
        }

        protected override Task CreateProjectCodeProviderFromSourceAsync(string source, string assemblyName)
        {
            var projectGrain = grainFactory.GetGrain<IProjectCodeProviderGrain>(assemblyName);
            return projectGrain.SetProjectSourceCode(source);
        }

        protected override IProjectCodeProvider GetProjectCodeProvider(string assemblyName)
        {
            foreach (var project in this.solution.Projects)
            {
                if (project.AssemblyName.Equals(assemblyName))
                {
                    var provider = grainFactory.GetGrain<IProjectCodeProviderGrain>(assemblyName);
                    return provider;
                }
            }
            return GetDummyProjectCodeProvider();
        }

        protected override IProjectCodeProvider GetDummyProjectCodeProvider()
        {
            var provider = grainFactory.GetGrain<IProjectCodeProviderGrain>("DUMMY");
            return provider;
        }
    }

    internal class OnDemandOrleansStrategy : IAnalysisStrategy
    {
        private ISolutionManager solutionManager;
        private IGrainFactory grainFactory;

        public OnDemandOrleansStrategy(IGrainFactory grainFactory)
        {
            this.grainFactory = grainFactory;
        }

        public async Task<ISolutionManager> CreateFromSourceAsync(string source)
        {
            this.solutionManager = await OrleansSolutionManager.CreateFromSourceAsync(grainFactory, source);
            return this.solutionManager;
        }
        public async Task<ISolutionManager> CreateFromSolutionAsync(string solutionPath)
        {
            this.solutionManager = await OrleansSolutionManager.CreateFromSolutionAsync(grainFactory, solutionPath);
            return this.solutionManager;
        }

        public Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
        {
            var methodEntityGrain = grainFactory.GetGrain<IMethodEntityGrain>(methodDescriptor.Marshall());
            return Task.FromResult<IMethodEntityWithPropagator>(methodEntityGrain);
        }
    }
}
