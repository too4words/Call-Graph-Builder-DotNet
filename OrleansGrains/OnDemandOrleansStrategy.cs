using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using OrleansInterfaces;
using Orleans;

namespace ReachingTypeAnalysis.Analysis
{
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
