using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OrleansInterfaces;
using System.Diagnostics.Contracts;
using Microsoft.CodeAnalysis;
using Orleans;


namespace ReachingTypeAnalysis.Analysis
{
    //internal abstract class AnalysisStrategy: IAnalysisStrategy
    //{
    //    public abstract  Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor);

    //}
	internal class OndemandAsyncStrategy : IAnalysisStrategy
	{
		private IDictionary<MethodDescriptor, IMethodEntityWithPropagator> methodEntities;
        private Solution solution;
		public OndemandAsyncStrategy(Solution solution)
		{
			this.methodEntities = new Dictionary<MethodDescriptor, IMethodEntityWithPropagator>();
            this.solution = solution;
		}

		public Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			IMethodEntityWithPropagator methodEntityPropagator = null;
			lock (methodEntities)
			{
				if (!methodEntities.TryGetValue(methodDescriptor, out methodEntityPropagator))
				{
					methodEntityPropagator = new MethodEntityWithPropagator(methodDescriptor,solution);
					methodEntities.Add(methodDescriptor, methodEntityPropagator);
				}
			}

			return Task.FromResult(methodEntityPropagator);
		}
	}

	internal class OnDemandOrleansStrategy : IAnalysisStrategy
	{
		public async Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
            var methodDescriptorToSearch = methodDescriptor;
            
            var methodEntityGrain = GrainClient.GrainFactory.GetGrain<IMethodEntityGrain>(methodDescriptorToSearch.Marshall());
            return await Task.FromResult(methodEntityGrain);
            //return await Task.FromResult(new MethodEntityGrainWrapper(methodEntityGrain));		
        }
	}
}
