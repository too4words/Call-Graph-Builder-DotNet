// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ReachingTypeAnalysis.Communication
{
    /// <summary>
	/// Information that is passed in a call message
	/// </summary>
	[Serializable]
    public class CallMessageInfo
    {
		public MethodDescriptor Caller { get; private set; }
		public MethodDescriptor Callee { get; private set; }
		public AnalysisCallNode CallNode { get; private set; }
		public IList<ISet<TypeDescriptor>> ArgumentsPossibleTypes { get; private set; }
		public TypeDescriptor ReceiverType { get; private set; }
		public VariableNode LHS { get; private set; }
		public PropagationKind PropagationKind { get; private set; }

		public CallMessageInfo() { }

        public CallMessageInfo(MethodDescriptor caller, MethodDescriptor callee, TypeDescriptor receiverType,
			IList<ISet<TypeDescriptor>> argumentsPossibleTypes, AnalysisCallNode callNode, VariableNode lhs,
			PropagationKind propKind)
		{
            this.Caller = caller;
            this.Callee = callee;
            this.ArgumentsPossibleTypes = argumentsPossibleTypes;
            this.ReceiverType = receiverType;
            this.LHS = lhs;
            this.PropagationKind = propKind;
            this.CallNode = callNode;
        }

		public override string ToString()
        {
            return string.Format("CallMessageInfo: {0}(...) -> {1}", this.Caller, this.Callee);
        }

        //public override bool Equals(object obj)
        //{
        //    var other = (CallMessageInfo)obj;
        //    return base.Equals(other);
        //    /// TODO: Complete EQUALS AND HASH
        //    //return this.ArgumentsPossibleTypes.Equals(other.ArgumentsPossibleTypes) 
        //    //    && this.Callee.Equals(oth;
        //}

        //public override int GetHashCode()
        //{
        //    return base.GetHashCode();
        //}
    }

    /// <summary>
    /// Information that is passed in a return message
    /// </summary>
    [Serializable]
    public class ReturnMessageInfo 
    {
		public MethodDescriptor Callee { get; private set; }
		public MethodDescriptor Caller { get; private set; }
		public ISet<TypeDescriptor> ResultPossibleTypes { get; private set; }
        public VariableNode LHS { get; private set; }
		public PropagationKind PropagationKind { get; private set; }
		public AnalysisCallNode CallNode { get; private set; }

		public ReturnMessageInfo() { }

		public ReturnMessageInfo(MethodDescriptor caller, MethodDescriptor callee, ISet<TypeDescriptor> resultPossibleTypes,
			AnalysisCallNode callNode, VariableNode lhs, PropagationKind propKind)
		{
			this.Caller = caller;
			this.Callee = callee;
			this.LHS = lhs;
			this.ResultPossibleTypes = resultPossibleTypes;
            this.PropagationKind = propKind;
            this.CallNode = callNode;
        }

        public override string ToString()
        {
            return string.Format("ReturnMessageInfo: {0} <- [{1}]", this.LHS, string.Join(", ", this.ResultPossibleTypes));
        }
    }
}
