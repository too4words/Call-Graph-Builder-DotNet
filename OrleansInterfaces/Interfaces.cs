using ReachingTypeAnalysis;
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

    public interface IMethodEntityGrain :  IGrainWithStringKey, IEntity
    {
        Task<IEntityDescriptor> GetDescriptor();

        Task<PropagationEffects> PropagateAsync();

        Task UpdateMethodArgumentsAsync(ISet<TypeDescriptor> receiverTypes,
            IList<ISet<TypeDescriptor>> argumentsPotentialTypes, PropagationKind propKind);
        Task UpdateMethodReturnAsync(ISet<TypeDescriptor> returnValues, VariableNode lhs, PropagationKind propKind);
 
        Task ProcessMessaggeAsync(IEntityDescriptor source, IMessage message);
        Task DoAnalysisAsync();
        //Task ReceiveMessageAsync(IEntityDescriptor source, IMessage message);
        Task<IEntityProcessor> GetEntityWithProcessorAsync();

        Task<bool> IsInitialized();

        Task<IEntity> GetMethodEntity();

        Task SetMethodEntity(IEntity methodEntity, IEntityDescriptor descriptor);

        Task SetDescriptor(IEntityDescriptor orleansEntityDescriptor);
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
