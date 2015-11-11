using CodeGraphModel;
using ReachingTypeAnalysis;
using ReachingTypeAnalysis.Communication;
using System;
using System.Collections.Generic;
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
    }

	public interface IAnalysisStrategy
	{
		ISolutionManager SolutionManager { get; }
        Task<ISolutionManager> CreateFromSourceAsync(string source);
		Task<ISolutionManager> CreateFromSolutionAsync(string solutionPath);
		Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor);
	}

    public interface IMethodEntityWithPropagator : IEntity
    {
        Task<PropagationEffects> PropagateAsync(PropagationKind propKind);
		Task<PropagationEffects> PropagateAsync(PropagationKind propKind, IEnumerable<PropGraphNodeDescriptor> reWorkSet);
        Task<PropagationEffects> PropagateAsync(CallMessageInfo callMessageInfo);
        Task<PropagationEffects> PropagateAsync(ReturnMessageInfo returnMessageInfo);
        Task<bool> IsInitializedAsync();

        Task<IEnumerable<TypeDescriptor>> GetInstantiatedTypesAsync();
		Task<IEnumerable<CallContext>> GetCallersAsync();
        Task<ISet<MethodDescriptor>> GetCalleesAsync();
        Task<IDictionary<AnalysisCallNode, ISet<MethodDescriptor>>> GetCalleesInfoAsync();
        Task<ISet<MethodDescriptor>> GetCalleesAsync(int invocationPosition);
        Task<int> GetInvocationCountAsync();

		Task<SymbolReference> GetDeclarationInfoAsync();
        Task<IEnumerable<SymbolReference>> GetCallersDeclarationInfoAsync();
		Task<IEnumerable<Annotation>> GetAnnotationsAsync();
		Task<PropagationEffects> RemoveMethodAsync();

		Task UnregisterCallerAsync(CallContext callContext);
		Task UseDeclaredTypesForParameters();
		Task<MethodCalleesInfo> FixUnknownCalleesAsync();

		//Task UnregisterCalleeAsync(CallContext callContext);
		//Task<PropagationEffects> GetMoreEffects();
	}

    public interface IProjectCodeProvider
    {
        Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2);

        Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor);

        Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor);

		Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor);

		Task<IEnumerable<MethodDescriptor>> GetRootsAsync(AnalysisRootKind rootKind = AnalysisRootKind.Default);

		Task<IEnumerable<MethodDescriptor>> GetReachableMethodsAsync();

        Task<int> GetReachableMethodsCountAsync();

        Task<IEnumerable<FileResponse>> GetDocumentsAsync();

		Task<IEnumerable<FileResponse>> GetDocumentEntitiesAsync(string documentPath);

		Task<PropagationEffects> RemoveMethodAsync(MethodDescriptor methodToUpdate);
		
		Task<PropagationEffects> AddMethodAsync(MethodDescriptor methodToAdd);

		Task ReplaceDocumentSourceAsync(string source, string documentPath);

		Task ReplaceDocumentAsync(string documentPath, string newDocumentPath = null);

		Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments);

		Task ReloadAsync();

		Task<SymbolReference> GetDeclarationInfoAsync(MethodDescriptor methodDescriptor);

		Task<SymbolReference> GetInvocationInfoAsync(CallContext callContext);

		Task<IEnumerable<TypeDescriptor>> GetCompatibleInstantiatedTypesAsync(TypeDescriptor type);
	}

    public interface ISolutionManager
    {
        Task<IEnumerable<MethodDescriptor>> GetRootsAsync(AnalysisRootKind rootKind = AnalysisRootKind.Default);

		Task<IEnumerable<MethodDescriptor>> GetReachableMethodsAsync();

        Task<int> GetReachableMethodsCountAsync();

        Task<IEnumerable<IProjectCodeProvider>> GetProjectCodeProvidersAsync();

		Task<IProjectCodeProvider> GetProjectCodeProviderAsync(string assemblyName);

		Task<IProjectCodeProvider> GetProjectCodeProviderAsync(MethodDescriptor methodDescriptor);

		Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor);

		//The next 2 methods are for RTA: Not currently used
		//Task AddInstantiatedTypesAsync(IEnumerable<TypeDescriptor> types);
		//Task<ISet<TypeDescriptor>> GetInstantiatedTypesAsync();

		Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments);

		Task ReloadAsync();
	}

    public interface IRtaManager
    {
        Task AddInstantiatedTypesAsync(IEnumerable<TypeDescriptor> types);
        Task<ISet<TypeDescriptor>> GetInstantiatedTypesAsync();
    }

    public delegate void MessageHandler(IMessage message);

    public interface IMessage
    {
        IEntityDescriptor Source { get; }
        MessageHandler Handler();
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
        REMOVE_ASSIGNMENT
    }

    [Serializable]
    public class CallContext
    {
        public MethodDescriptor Caller { get; private set; }
        public VariableNode LHS { get; private set; }
        public AnalysisCallNode CallNode { get; private set; }

        public CallContext(MethodDescriptor caller, VariableNode lhs, AnalysisCallNode callNode)
        {
            this.Caller = caller;
            this.LHS = lhs;
            this.CallNode = callNode;
        }

        public override bool Equals(object obj)
        {
            var c2 = obj as CallContext;
            return this.Caller.Equals(c2.Caller) && (this.LHS == null || this.LHS.Equals(c2.LHS))
                && (this.CallNode == null || c2.CallNode == null || this.CallNode.Equals(c2.CallNode));
        }

        public override int GetHashCode()
        {
            int lhsHashCode = this.LHS == null ? 1 : this.LHS.GetHashCode();
            return this.Caller.GetHashCode() + lhsHashCode;
        }
    }
}