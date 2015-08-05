using ReachingTypeAnalysis;
using ReachingTypeAnalysis.Communication;
using System.Threading.Tasks;
using Orleans;
using System.Collections.Generic;

namespace OrleansInterfaces
{
    /*
    public interface IOrleansEntityDescriptor : 
		Orleans.IGrainWithGuidKey, IEntityDescriptor
    {
        Task<Guid> GetGuid();
    }
     */

    public interface IMethodEntityGrain :  IGrainWithStringKey, IMethodEntityWithPropagator
    {
        //Task<IEntityDescriptor> GetDescriptor();
        //Task<MethodDescriptor> GetMethodDescriptor();
        //Task SetDescriptor(IEntityDescriptor orleansEntityDescriptor);

		Task SetMethodEntityAsync(IEntity methodEntity, MethodDescriptor methodDescriptor);
        //Task<PropagationEffects> PropagateAsync(PropagationKind propKind);
        //Task<PropagationEffects> PropagateAsync(CallMessageInfo callMessageInfo);
        //Task<PropagationEffects> PropagateAsync(ReturnMessageInfo returnMessageInfo);
        //Task<bool> IsInitializedAsync();
        //Task<IEntity> GetMethodEntityAsync();
        //Task<ISet<MethodDescriptor>> GetCalleesAsync();
        //Task<IDictionary<AnalysisCallNode, ISet<MethodDescriptor>>> GetCalleesInfoAsync();
        //Task<ISet<MethodDescriptor>> GetCalleesAsync(int invocationPosition);
        //Task<int> GetInvocationCountAsync();
    }

}
