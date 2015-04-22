// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ReachingTypeAnalysis.Communication
{
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
	internal class CallMessageInfo
    {
        public CallMessageInfo() { }
        internal CallMessageInfo(AnalysisMethod caller, AnalysisMethod callee, AnalysisNode callNode, ISet<AnalysisType> receivers, IEnumerable<ISet<AnalysisType>> argumentValues,
            AnalysisNode lhs, ISet<AnalysisType> instantiatedTypes, PropagationKind propKind)
        {
            this.Caller = caller.MethodDescriptor;
            this.Callee = callee.MethodDescriptor;
            this.ArgumentValues = Marshal.ToTypeDescriptorList(argumentValues);
            this.Receivers = Marshal.ToTypeDescriptors(receivers);
            this.LHS = new VariableDescriptor(lhs);
            this.InstantiatedTypes = Marshal.ToTypeDescriptors(instantiatedTypes);
            this.PropagationKind = propKind;
            this.CallNode = new LocationDescriptor((callNode as AnalysisNode).LocationReference.Location);
        }

		//private bool isStatic;
		internal readonly MethodDescriptor Callee;
		internal readonly MethodDescriptor Caller;
		internal readonly LocationDescriptor CallNode;
		internal readonly IEnumerable<ISet<TypeDescriptor>> ArgumentValues;
		internal readonly ISet<TypeDescriptor> Receivers;
		internal readonly VariableDescriptor LHS;
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
    internal class ReturnMessageInfo 
    {
		internal ISet<TypeDescriptor> RVs { get; private set; }
		internal VariableDescriptor LHS { get; private set; }
		internal ISet<TypeDescriptor> InstatiatedTypes { get; private set; }
		internal PropagationKind PropagationKind { get; private set; }

		internal VariableDescriptor InvocationNode { get; private set; }

        internal ReturnMessageInfo(AnalysisNode lhs, 
			ISet<AnalysisType> rvs, PropagationKind propKind, 
			ISet<AnalysisType> instantiatedTypes, AnalysisNode invocatioNode)
        {
            //ISet<Type> rvs = rv != null ? worker.GetTypes(rv, propKind) : new HashSet<Type>();
            this.RVs = Marshal.ToTypeDescriptors(rvs);
            this.LHS = new VariableDescriptor(lhs);
            this.PropagationKind = propKind;
			this.InstatiatedTypes = Marshal.ToTypeDescriptors(instantiatedTypes);
            this.InvocationNode = new VariableDescriptor(invocatioNode);
        }

        public override string ToString()
        {
            return string.Format("{0} <- [{1}]", this.LHS, string.Join(",", this.RVs));
        }
    }
}
