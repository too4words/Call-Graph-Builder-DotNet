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
            var methodEntityGrain = GrainClient.GrainFactory.GetGrain<IMethodEntityGrain>(methodDescriptor.Marshall());

            //var methodEntityGrain = MethodEntityGrainFactory.GetGrain(methodDescriptor.Marshall());

            return await Task.FromResult(new MethodEntityGrainWrapper(methodEntityGrain));
		}

        //private Task<IMethodEntityGrain> GetMethodEntityGrainAsync(MethodDescriptor methodDescriptor)
        //{
        //    Logger.Instance.Log("AnalysisOrchestator", "CreateMethodEntityGrain", methodDescriptor);

        //    var methodEntityGrain = MethodEntityGrainFactory.GetGrain(methodDescriptor.Marshall());

        //    // Now on activation the method entity is created in the grain
        //    // We no longer need to create them externally
        //    //var isInitialized = await methodEntityGrain.IsInitialized();
        //    //// check if the result is initialized
        //    //// Now 
        //    //if (!isInitialized)
        //    //{
        //    //    Logger.Instance.Log("AnalysisOrchestator", "CreateMethodEntityGrain", "MethodEntityGrain for {0} does not exist", methodDescriptor);
        //    //    Contract.Assert(methodDescriptor != null);
        //    //    ////  methodEntity = await providerGrain.CreateMethodEntityAsync(grainDesc.MethodDescriptor);
        //    //    var methodEntity = await CreateMethodEntityUsingGrainsAsync(methodDescriptor);
        //    //    Contract.Assert(methodEntity != null);
        //    //    await methodEntityGrain.SetMethodEntityAsync(methodEntity, methodDescriptor);
        //    //    //await methodEntityGrain.SetDescriptor(entityDescriptor);
        //    //    return methodEntityGrain;
        //    //}
        //    //else
        //    //{
        //    //    Logger.Instance.Log("AnalysisOrchestator", "CreateMethodEntityGrain", "MethodEntityGrain for {0} already exists", methodDescriptor);
        //    //    return methodEntityGrain;
        //    //}
        //    return Task.FromResult(methodEntityGrain);
        //}

        //private async Task<MethodEntity> CreateMethodEntityUsingGrainsAsync(MethodDescriptor methodDescriptor)
        //{
        //    Logger.Instance.Log("AnalysisOrchestator", "CreateMethodEntityUsingGrainsAsync", "Creating new MethodEntity for {0}", methodDescriptor);

        //    MethodEntity methodEntity = null;
        //    var solutionGrain = SolutionGrainFactory.GetGrain("Solution");
        //    var providerGrain = await solutionGrain.GetCodeProviderAsync(methodDescriptor);
        //    Contract.Assert(providerGrain != null);
        //    methodEntity = (MethodEntity) await providerGrain.CreateMethodEntityAsync(methodDescriptor);
        //    return methodEntity;
        //}
	}
}
