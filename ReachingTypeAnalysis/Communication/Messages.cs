// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ReachingTypeAnalysis.Communication
{
    [Serializable]
	public abstract class Message: IMessage
    {
		public IEntityDescriptor Source { get; private set; }

		internal Message(IEntityDescriptor source)
        {
            this.Source = source;
        }

        public abstract MessageHandler Handler();
    }

	/// <summary>
	/// Information that is passed in a call message
	/// </summary>
	[Serializable]
    internal class CallMessageInfo
    {
		internal MethodDescriptor Callee { get; private set; }
		internal MethodDescriptor Caller { get; private set; }
		internal AnalysisCallNode CallNode { get; private set; }
		internal IList<ISet<TypeDescriptor>> ArgumentsPossibleTypes { get; private set; }
		internal ISet<TypeDescriptor> ReceiverPossibleTypes { get; private set; }
		internal VariableNode LHS { get; private set; }
		internal ISet<TypeDescriptor> InstantiatedTypes { get; private set; }
		internal PropagationKind PropagationKind { get; private set; }

		public CallMessageInfo() { }

        internal CallMessageInfo(MethodDescriptor caller, MethodDescriptor callee, ISet<TypeDescriptor> receiverPossibleTypes,
			IList<ISet<TypeDescriptor>> argumentsPossibleTypes, ISet<TypeDescriptor> instantiatedTypes,
			AnalysisCallNode callNode, VariableNode lhs, PropagationKind propKind)
		{
            this.Caller = caller;
            this.Callee = callee;
            this.ArgumentsPossibleTypes = argumentsPossibleTypes;
            this.ReceiverPossibleTypes = receiverPossibleTypes;
            this.LHS = lhs;
            this.InstantiatedTypes = instantiatedTypes;
            this.PropagationKind = propKind;
            this.CallNode = callNode;
        }

		public override string ToString()
        {
            return string.Format("CallMessageInfo: {0}(...) -> {1}", this.Caller, this.Callee);
        }
    }

    /// <summary>
    /// Information that is passed in a return message
    /// </summary>
    [Serializable]
    internal class ReturnMessageInfo 
    {
		internal MethodDescriptor Callee { get; private set; }
		internal MethodDescriptor Caller { get; private set; }
		internal ISet<TypeDescriptor> ResultPossibleTypes { get; private set; }
        internal VariableNode LHS { get; private set; }
		internal ISet<TypeDescriptor> InstatiatedTypes { get; private set; }
		internal PropagationKind PropagationKind { get; private set; }
		internal AnalysisCallNode CallNode { get; private set; }

		public ReturnMessageInfo() { }

		internal ReturnMessageInfo(MethodDescriptor caller, MethodDescriptor callee, ISet<TypeDescriptor> resultPossibleTypes,
			ISet<TypeDescriptor> instantiatedTypes, AnalysisCallNode callNode, VariableNode lhs, PropagationKind propKind)
		{
			this.Caller = caller;
			this.Callee = callee;
			this.LHS = lhs;
			this.ResultPossibleTypes = resultPossibleTypes;
            this.PropagationKind = propKind;
			this.InstatiatedTypes = instantiatedTypes;
            this.CallNode = callNode;
        }

        public override string ToString()
        {
            return string.Format("ReturnMessageInfo: {0} <- [{1}]", this.LHS, string.Join(", ", this.ResultPossibleTypes));
        }
    }
}
