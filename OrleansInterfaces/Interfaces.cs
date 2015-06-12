using ReachingTypeAnalysis;
using System;
using System.Threading.Tasks;

namespace OrleansInterfaces
{
    /*
    public interface IOrleansEntityDescriptor : 
		Orleans.IGrainWithGuidKey, IEntityDescriptor
    {
        Task<Guid> GetGuid();
    }
     */

    public interface IMethodEntityGrain :
        Orleans.IGrainWithGuidKey, IEntity
    {
        Task<IEntityDescriptor> GetDescriptor();
        Task ReceiveMessageAsync(IEntityDescriptor source, IMessage message);
        Task<bool> IsInitialized();

        Task<IEntity> GetMethodEntity();

        Task SetMethodEntity(IEntity methodEntity, IEntityDescriptor descriptor);

        Task SetDescriptor(IEntityDescriptor orleansEntityDescriptor);
    }

    public interface IProjectCodeProviderGrain : Orleans.IGrainWithStringKey
    {
        Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2);
    }
}
