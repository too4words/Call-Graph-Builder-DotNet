using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OrleansInterfaces;
using System.Diagnostics.Contracts;
using Microsoft.CodeAnalysis;
using Orleans;
using ReachingTypeAnalysis.Roslyn;


namespace ReachingTypeAnalysis.Analysis
{
    //internal abstract class AnalysisStrategy: IAnalysisStrategy
    //{
    //    public abstract  Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor);

    //}
	internal class OnDemandAsyncStrategy : IAnalysisStrategy
	{
		private IDictionary<MethodDescriptor, IMethodEntityWithPropagator> methodEntities;
        private ISolutionManager solutionManager;

		public OnDemandAsyncStrategy()
		{
			this.methodEntities = new Dictionary<MethodDescriptor, IMethodEntityWithPropagator>();
            
		}

		public async Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			IMethodEntityWithPropagator methodEntityPropagator = null;

			lock (methodEntities)
			{
				if (!methodEntities.TryGetValue(methodDescriptor, out methodEntityPropagator))
				{
                    var codeProvider = solutionManager.GetProjectCodeProviderAsync(methodDescriptor.BaseDescriptor).Result;
					methodEntityPropagator = new MethodEntityWithPropagator(methodDescriptor, codeProvider);
					methodEntities.Add(methodDescriptor, methodEntityPropagator);
				}
			}
            await solutionManager.AddInstantiatedTypesAsync(await methodEntityPropagator.GetInstantiatedTypesAsync());
			return methodEntityPropagator;
		}


        public async Task<ISolutionManager> CreateSolutionAsync(string filePath)
        {
            this.solutionManager = await SolutionManager.CreateFromSolution(this, filePath);
            return this.solutionManager;
        }

        public async Task<ISolutionManager> CreateSolutionFromSourceAsync(string source)
        {
            this.solutionManager = await SolutionManager.CreateFromSourceCode(this, source);
            return this.solutionManager;
        }

        public Task<IProjectCodeProvider> CreateProjectCodeProviderAsync(string projectFilePath, string projectName)
        {
            return ProjectCodeProvider.ProjectCodeProviderAsync(projectFilePath);
        }

        public async Task<IProjectCodeProvider> CreateProjectCodeFromSourceAsync(string source, string projectName)
        {
            var solution = Utils.CreateSolution(source);
            return await ProjectCodeProvider.ProjectCodeProviderByNameAsync(solution, projectName);
        }


        public Task<IProjectCodeProvider> GetDummyProjectCodeProviderAsync()
        {
            return Task.FromResult<IProjectCodeProvider>(new DummyCodeProvider());
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


		public async Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{           
            var methodEntityGrain = /*GrainClient.GrainFactory*/ grainFactory.GetGrain<IMethodEntityGrain>(methodDescriptor.Marshall());
            return await Task.FromResult(methodEntityGrain);	
        }


        public Task<ISolutionManager> CreateSolutionAsync(string filePath)
        {
            var solutionGrain = grainFactory.GetGrain<ISolutionGrain>("Solution");
            solutionGrain.SetSolutionPath(filePath);
            this.solutionManager = solutionGrain;
            return Task.FromResult(solutionManager);
        }

        public Task<ISolutionManager> CreateSolutionFromSourceAsync(string source)
        {
            var solutionGrain = grainFactory.GetGrain<ISolutionGrain>("Solution");
            solutionGrain.SetSolutionSource(source);
            this.solutionManager = solutionGrain;
            return Task.FromResult(solutionManager);
        }

        public async Task<IProjectCodeProvider> CreateProjectCodeProviderAsync(string projectFilePath, string projectName)
        {
            var projectGrain = grainFactory.GetGrain<IProjectCodeProviderGrain>(projectName);
            await projectGrain.SetProjectPath(projectFilePath);
            return projectGrain;
        }

        public async Task<IProjectCodeProvider> CreateProjectCodeFromSourceAsync(string source, string projectName)
        {
            var projectGrain = grainFactory.GetGrain<IProjectCodeProviderGrain>(projectName);
            await projectGrain.SetProjectSourceCode(source);
            return projectGrain;
        }


        public Task<IProjectCodeProvider> GetDummyProjectCodeProviderAsync()
        {
            var grain = grainFactory.GetGrain<IProjectCodeProviderGrain>("DUMMY");
            return Task.FromResult<IProjectCodeProvider>(grain);
        }
    }
}
