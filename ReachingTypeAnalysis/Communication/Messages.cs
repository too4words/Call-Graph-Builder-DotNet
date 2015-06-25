// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ReachingTypeAnalysis.Communication
{
    [Serializable]
	public abstract class Message: IMessage
    {
        internal Message(IEntityDescriptor source)
        {
            this.Source = source;
        }
        public IEntityDescriptor Source { get; private set; }

        public abstract MessageHandler Handler();
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

	/// <summary>
	/// Information that is passed in a call message
	/// </summary>
	/// <typeparam name="M"></typeparam>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="E"></typeparam>
	[Serializable]
    internal class CallMessageInfo
    {
        public CallMessageInfo() { }
        internal CallMessageInfo(MethodDescriptor caller, MethodDescriptor callee, AnalysisCallNode callNode, 
            ISet<TypeDescriptor> receivers, IList<ISet<TypeDescriptor>> argumentValues,
            VariableNode lhs, ISet<TypeDescriptor> instantiatedTypes, PropagationKind propKind)
        {
            this.Caller = caller;
            this.Callee = callee;
            this.ArgumentValues = Marshal.ToTypeDescriptorList(argumentValues);
            this.Receivers = Marshal.ToTypeDescriptors(receivers);
            this.LHS = lhs;
            this.InstantiatedTypes = Marshal.ToTypeDescriptors(instantiatedTypes);
            this.PropagationKind = propKind;
            this.CallNode = callNode;
        }

		//private bool isStatic;
		internal readonly MethodDescriptor Callee;
		internal readonly MethodDescriptor Caller;
		internal readonly AnalysisCallNode CallNode;
		internal readonly IList<ISet<TypeDescriptor>> ArgumentValues;
		internal readonly ISet<TypeDescriptor> Receivers;
        internal readonly VariableNode LHS;
		internal readonly ISet<TypeDescriptor> InstantiatedTypes;
		internal readonly PropagationKind PropagationKind;

        public override string ToString()
        {
            return string.Format("CallMessage: {0}(...) -> {1}", this.Caller, this.Callee);
        }
    }

    /// <summary>
    /// Information that is passed in a return message
    /// </summary>
    /// <typeparam name="M"></typeparam>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="E"></typeparam>
    [Serializable]
    internal class ReturnMessageInfo 
    {
		internal ISet<TypeDescriptor> RVs { get; private set; }
        internal VariableNode LHS { get; private set; }
		internal ISet<TypeDescriptor> InstatiatedTypes { get; private set; }
		internal PropagationKind PropagationKind { get; private set; }
		internal AnalysisCallNode InvocationNode { get; private set; }

        internal ReturnMessageInfo(VariableNode lhs, 
			ISet<TypeDescriptor> rvs, PropagationKind propKind, 
			ISet<TypeDescriptor> instantiatedTypes, AnalysisCallNode invocationNode)
        {
            //ISet<Type> rvs = rv != null ? worker.GetTypes(rv, propKind) : new HashSet<Type>();
            this.RVs = rvs;
            this.LHS = lhs;
            this.PropagationKind = propKind;
			this.InstatiatedTypes = instantiatedTypes;
            this.InvocationNode = invocationNode;
        }

        public override string ToString()
        {
            return string.Format("{0} <- [{1}]", this.LHS, string.Join(",", this.RVs));
        }
    }
}
