using ReachingTypeAnalysis;
using System.Threading.Tasks;
using Orleans;

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
        Task ProcessMessagge(IEntityDescriptor source, IMessage message, IDispatcher dispatcher);
        Task DoAnalysisAsync(IDispatcher dispatcher);
        //Task ReceiveMessageAsync(IEntityDescriptor source, IMessage message);
        Task<bool> IsInitialized();

        Task<IEntity> GetMethodEntity();

        Task SetMethodEntity(IEntity methodEntity, IEntityDescriptor descriptor);

        Task SetDescriptor(IEntityDescriptor orleansEntityDescriptor);
    }

    public interface IProjectCodeProviderGrain : IGrainWithStringKey
    {
        Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2);
        Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor);
    }
}
