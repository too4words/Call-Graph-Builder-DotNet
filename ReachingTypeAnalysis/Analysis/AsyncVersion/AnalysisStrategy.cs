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
    }

	internal class OnDemandOrleansStrategy : IAnalysisStrategy
	{
        private ISolutionManager solutionManager;
        private IGrainFactory grainFactory;

        public OnDemandOrleansStrategy(IGrainFactory grainFactory)
        {
            this.grainFactory = grainFactory;
        }

		public Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{           
            var methodEntityGrain = grainFactory.GetGrain<IMethodEntityGrain>(methodDescriptor.Marshall());
			return Task.FromResult<IMethodEntityWithPropagator>(methodEntityGrain);	
        }
    }
}
