using ReachingTypeAnalysis;
using System;
using System.Threading.Tasks;

namespace OrleansInterfaces
{
    public interface IOrleansEntityDescriptor : 
		Orleans.IGrainWithGuidKey, IEntityDescriptor
    {
        Task<Guid> GetGuid();
    }

    public interface IMethodEntityGrain: 
		Orleans.IGrainWithGuidKey, IEntity
    {		
		Task<IOrleansEntityDescriptor> GetDescriptor();
		Task ReceiveMessageAsync(IOrleansEntityDescriptor source, IMessage message);
        Task<bool> IsInitialized();
	}
}
