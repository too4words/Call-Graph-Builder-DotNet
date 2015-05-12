using System.Collections.Immutable;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis
{
    public interface IEntityDescriptor
    {
        //MethodDescriptor MethodDescriptor { get; }
    }

    public interface IEntity
    {
        IEntityProcessor GetEntityProcessor(IDispatcher dispacther);
    }

    public interface IEntityProcessor
    {
        IEntity Entity { get; }
        void SendMessage(IEntityDescriptor destination, IMessage message);
        void ReceiveMessage(IEntityDescriptor source, IMessage message);
        Task SendMessageAsync(IEntityDescriptor destination, IMessage message);
        Task ReceiveMessageAsync(IEntityDescriptor source, IMessage message);
        void DoAnalysis();
        Task DoAnalysisAsync();
    }

    public delegate void MessageHandler(IMessage message);

    public interface IMessage
    {
        IEntityDescriptor Source { get; }
        MessageHandler Handler();
    }

    public interface IDispatcher
    {
        ImmutableHashSet<IEntity> GetAllEntites();
        void DeliverMessage(IEntityDescriptor destination, IMessage message);
        Task DeliverMessageAsync(IEntityDescriptor destination, IMessage message);
        Task<IEntity> GetEntity(IEntityDescriptor entityDesc);
        void RegisterEntity(IEntityDescriptor entityDesc, IEntity entity);
        Task<IEntityProcessor> GetEntityWithProcessorAsync(IEntityDescriptor entityDesc);
    }
}