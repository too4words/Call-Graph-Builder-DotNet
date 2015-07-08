using System;
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
        //IEntityProcessor GetEntityProcessor(IDispatcher dispacther);
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

    public interface ICodeProvider
    {
        Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2);
        Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor);
        bool IsSubtype(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2);
        MethodDescriptor FindMethodImplementation(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor);
    
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
        ImmutableHashSet<IEntityDescriptor> GetAllEntitiesDescriptors();

        void DeliverMessage(IEntityDescriptor destination, IMessage message);
        Task DeliverMessageAsync(IEntityDescriptor destination, IMessage message);
        //Task<IEntity> GetEntityAsync(IEntityDescriptor entityDesc);
        //IEntity GetEntity(IEntityDescriptor entityDesc);
        void RegisterEntity(IEntityDescriptor entityDesc, IEntity entity);
        Task<IEntityProcessor> GetEntityWithProcessorAsync(IEntityDescriptor entityDesc);
        IEntityProcessor GetEntityWithProcessor(IEntityDescriptor entityDesc);
    }

    /// <summary>
    /// This type represent the type of operation we want to process
    /// </summary>
    [Serializable]
    public enum PropagationKind
    {
        ADD_TYPES,
        REMOVE_TYPES,
        ADD_ASSIGNMENT,
        REMOVE_ASSIGNMENT,
    }
    [Serializable]
    public class CallContext
    {
        public MethodDescriptor Caller { get; private set; }
        public VariableNode CallLHS { get; private set; }
        public AnalysisCallNode Invocation { get; private set; }

        public CallContext(MethodDescriptor caller, VariableNode lhs, AnalysisCallNode inv)
        {
            this.Caller = caller;
            this.CallLHS = lhs;
            this.Invocation = inv;
        }

        public override bool Equals(object obj)
        {
            var c2 = obj as CallContext;
            return this.Caller.Equals(c2.Caller) && (this.CallLHS == null || this.CallLHS.Equals(c2.CallLHS))
                && (this.Invocation == null || c2.Invocation == null || this.Invocation.Equals(c2.Invocation));
        }
        public override int GetHashCode()
        {
            int lhsHashCode = this.CallLHS == null ? 1 : this.CallLHS.GetHashCode();
            return this.Caller.GetHashCode() + lhsHashCode;
        }
    }

}