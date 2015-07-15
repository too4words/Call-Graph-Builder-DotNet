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

    public interface IMethodEntityGrain :  IGrainWithStringKey //, IMethodEntity
    {
        //Task<IEntityDescriptor> GetDescriptor();
        //Task<MethodDescriptor> GetMethodDescriptor();
        //Task SetDescriptor(IEntityDescriptor orleansEntityDescriptor);

		Task SetMethodEntityAsync(IEntity methodEntity, MethodDescriptor methodDescriptor);
        Task<PropagationEffects> PropagateAsync(PropagationKind propKind);
        Task<PropagationEffects> PropagateAsync(CallMessageInfo callMessageInfo);
        Task<PropagationEffects> PropagateAsync(ReturnMessageInfo returnMessageInfo);
        Task<bool> IsInitialized();
        Task<IEntity> GetMethodEntity();
        Task<ISet<MethodDescriptor>> GetCalleesAsync();
        Task<IDictionary<AnalysisCallNode, ISet<MethodDescriptor>>> GetCalleesInfoAsync();
    }

    public interface IProjectCodeProviderGrain : IGrainWithStringKey
    {
        Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2);
        Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor);
		Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor);
        Task SetProjectPath(string fullPath);
        Task SetProjectSourceCode(string source);
    }
}
