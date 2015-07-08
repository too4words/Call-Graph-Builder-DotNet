// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis
{
	[Serializable]
	public abstract class AnalysisInvocationExpession
	{
		public AnalysisInvocationExpession(MethodDescriptor caller, AnalysisCallNode callNode, 
                        PropGraphNodeDescriptor reciever, 
                        IList<PropGraphNodeDescriptor> arguments, VariableNode lhs)
		{
			Caller = caller;
			Arguments = arguments;
			LHS = lhs;
			Receiver = reciever;
			IsStatic = false;
            CallNode = callNode;
            InstantiatedTypes = new HashSet<TypeDescriptor>();
            ReceiverPotentialTypes = new HashSet<TypeDescriptor>();
            ArgumentsPotentialTypes = new List<ISet<TypeDescriptor>>();
        }
		public AnalysisInvocationExpession(MethodDescriptor caller, 
                        AnalysisCallNode callNode, IList<PropGraphNodeDescriptor> arguments, VariableNode lhs)
            : this(caller,callNode, null, arguments, lhs)
		{
			IsStatic = true;
		}

        //internal abstract ISet<MethodDescriptor> ComputeCalleesForNode(PropagationGraph propGraph, ICodeProvider codeProvider);
        //internal virtual Task<ISet<MethodDescriptor>> ComputeCalleesForNodeAsync(PropagationGraph propGraph, ICodeProvider codeProvider)
        //{
        //    throw new NotImplementedException();
        //}

        //internal ISet<TypeDescriptor> GetPotentialTypes(PropGraphNodeDescriptor n, PropagationGraph propGraph, ICodeProvider codeProvider)
        //{
        //    var result = new HashSet<TypeDescriptor>();
        //    foreach (var typeDescriptor in propGraph.GetTypes(n))
        //    {
        //        // TO-DO fix by adding a where T: AnalysisType
        //        if (typeDescriptor.IsConcreteType)
        //        {
        //            result.Add(typeDescriptor);
        //        }
        //        else
        //        {
        //            // If it is a declaredTyped it means we were not able to compute a concrete type
        //            // Therefore, we instantiate all compatible types for the set of instantiated types
        //            //result.UnionWith(this.InstatiatedTypes.Where(iType => iType.IsSubtype(typeDescriptor)));
        //            Contract.Assert(this.InstatiatedTypes != null);
        //            // Diego: This requires a Code Provider. Now it will simply fail.
        //            result.UnionWith(this.InstatiatedTypes.Where(candidateTypeDescriptor
        //                                    => codeProvider.IsSubtype(candidateTypeDescriptor, typeDescriptor)));
        //        }
        //    }
        //    return result;
        //}
        //internal async Task<ISet<TypeDescriptor>> GetPotentialTypesAsync(PropGraphNodeDescriptor n, PropagationGraph propGraph, ICodeProvider codeProvider)
        //{
        //    var result = new HashSet<TypeDescriptor>();
        //    foreach (var typeDescriptor in propGraph.GetTypes(n))
        //    {
        //        // TO-DO fix by adding a where T: AnalysisType
        //        if (typeDescriptor.IsConcreteType)
        //        {
        //            result.Add(typeDescriptor);
        //        }
        //        else
        //        {
        //            Contract.Assert(this.InstatiatedTypes != null);
        //            foreach(var candidateType in this.InstatiatedTypes)
        //            {
        //                var isSubtype = await codeProvider.IsSubtypeAsync(candidateType, typeDescriptor);
        //                if(isSubtype)
        //                {
        //                    result.Add(candidateType);
        //                }
        //            }
        //        }
        //    }
        //    return result;
        //}


		public bool IsStatic { get; set; }
		public bool IsConstructor { get; set; }
		// public M Callee;
		public MethodDescriptor Caller { get; set; }
		public IList<PropGraphNodeDescriptor> Arguments { get; set; }
		public PropGraphNodeDescriptor Receiver { get; set; }
		public VariableNode LHS { get; set; }
		public ISet<TypeDescriptor> InstantiatedTypes { get; set; }
		public AnalysisCallNode CallNode { get; set; }
        public ISet<TypeDescriptor> ReceiverPotentialTypes { get; set; }
        public IList<ISet<TypeDescriptor>> ArgumentsPotentialTypes { get; set; }
	}

	[Serializable]
	public class CallInfo : AnalysisInvocationExpession
	{
		public CallInfo(MethodDescriptor caller, AnalysisCallNode callNode, MethodDescriptor callee, 
                    PropGraphNodeDescriptor reciever, IList<PropGraphNodeDescriptor> arguments,
                    VariableNode lhs, bool isConstructor)
			: base(caller, callNode, reciever, arguments, lhs)
		{
			//Caller = caller;
			Callee = callee;
			//Arguments = arguments;
			//LHS = lhs;
			//Receiver = reciever;
			//IsStatic = false;
			IsConstructor = isConstructor;
            
		}

		public CallInfo(MethodDescriptor caller, AnalysisCallNode callNode, 
                    MethodDescriptor callee, IList<PropGraphNodeDescriptor> arguments,
                    VariableNode lhs, bool isConstructor)
            : this(caller, callNode, callee, null, arguments, lhs, isConstructor)
		{
			//IsStatic = true;
		}

        //internal override ISet<MethodDescriptor> ComputeCalleesForNode(PropagationGraph propGraph, ICodeProvider codeProvider)
        //{
        //    var calleesForNode = new HashSet<MethodDescriptor>();
        //    if (this.Receiver != null)
        //    {
        //        // I replaced the invocation for a local call to mark that functionality is missing
        //        //var callees = GetPotentialTypes(this.Receiver, propGraph)
        //        //    .Select(t => this.Callee.FindMethodImplementation(t));
        //        var callees = GetPotentialTypes(this.Receiver, propGraph, codeProvider)
        //                .Select(t => codeProvider.FindMethodImplementation(this.Callee,t));
        //        calleesForNode.UnionWith(callees);
        //    }
        //    else
        //    {
        //        calleesForNode.Add(this.Callee);
        //    }
        //    return calleesForNode;
        //}

        //internal async override Task<ISet<MethodDescriptor>> ComputeCalleesForNodeAsync(PropagationGraph propGraph, ICodeProvider codeProvider)
        //{
        //    var calleesForNode = new HashSet<MethodDescriptor>();
        //    if (this.Receiver != null)
        //    {
        //        // I replaced the invocation for a local call to mark that functionality is missing
        //        //var callees = GetPotentialTypes(this.Receiver, propGraph)
        //        //    .Select(t => this.Callee.FindMethodImplementation(t));
        //        foreach(var type in propGraph.GetPotentialTypes(this.Receiver, this.InstatiatedTypes, codeProvider))
        //        {
        //            var realCallee = await codeProvider.FindMethodImplementationAsync(this.Callee, type);
        //            calleesForNode.Add(realCallee);
        //        }
        //    }
        //    else
        //    {
        //        calleesForNode.Add(this.Callee);
        //    }
        //    return calleesForNode;
        //}

		public MethodDescriptor Callee { get; private set; }
	}

	[Serializable]
	public class DelegateCallInfo : AnalysisInvocationExpession
	{
        public DelegateCallInfo(MethodDescriptor caller, AnalysisCallNode callNode,
                DelegateVariableNode calleeDelegate, IList<PropGraphNodeDescriptor> arguments, VariableNode lhs)
			: this(caller, callNode, calleeDelegate,null, arguments, lhs)
		{
            //Caller = caller;
            //CalleeDelegate = calleeDelegate;
            //Arguments = arguments;
            //LHS = lhs;
		}

		public DelegateCallInfo(MethodDescriptor caller, AnalysisCallNode callNode, 
                    DelegateVariableNode calleeDelegate, PropGraphNodeDescriptor receiver,
                    IList<PropGraphNodeDescriptor> arguments, VariableNode lhs)
			: base(caller, callNode,receiver, arguments, lhs)
		{
			//Caller = caller;
			CalleeDelegate = calleeDelegate;
			//Receiver = receiver;
			//Arguments = arguments;
			//LHS = lhs;
            ResolvedCallees = new HashSet<MethodDescriptor>();
		}

        //internal override ISet<MethodDescriptor> ComputeCalleesForNode(PropagationGraph propGraph, ICodeProvider codeProvider)
        //{
        //    return GetDelegateCallees(this.CalleeDelegate, propGraph, codeProvider);
        //}

        //internal override async Task<ISet<MethodDescriptor>> ComputeCalleesForNodeAsync(PropagationGraph propGraph, ICodeProvider codeProvider)
        //{
        //    return await GetDelegateCalleesAsync(this.CalleeDelegate, propGraph, codeProvider);
        //}

        //private ISet<MethodDescriptor> GetDelegateCallees(VariableNode delegateNode, PropagationGraph propGraph, ICodeProvider codeProvider)
        //{
        //    var callees = new HashSet<MethodDescriptor>();
        //    var typeDescriptors = propGraph.GetTypes(delegateNode);
        //    foreach (var delegateInstance in propGraph.GetDelegates(delegateNode))
        //    {
        //        if (typeDescriptors.Count() > 0)
        //        {
        //            foreach (var typeDescriptor in typeDescriptors)
        //            {
        //                // TO-DO!!!
        //                // Ugly: I'll fix it
        //                //var aMethod = delegateInstance.FindMethodImplementation(type);
        //                var aMethod = codeProvider.FindMethodImplementation(delegateInstance,typeDescriptor);
        //                callees.Add(aMethod);
        //            }
        //        }
        //        else
        //        {
        //            // if Count is 0, it is a delegate that do not came form an instance variable
        //            callees.Add(delegateInstance);
        //        }
        //    }

        //    return callees;
        //}

        //private async Task<ISet<MethodDescriptor>> GetDelegateCalleesAsync(VariableNode delegateNode, PropagationGraph propGraph, ICodeProvider codeProvider)
        //{
        //    var callees = new HashSet<MethodDescriptor>();
        //    var typeDescriptors = propGraph.GetTypes(delegateNode);
        //    foreach (var delegateInstance in propGraph.GetDelegates(delegateNode))
        //    {
        //        if (typeDescriptors.Count() > 0)
        //        {
        //            foreach (var typeDescriptor in typeDescriptors)
        //            {
        //                // TO-DO!!!
        //                // Ugly: I'll fix it
        //                //var aMethod = delegateInstance.FindMethodImplementation(type);
        //                var aMethod = await codeProvider.FindMethodImplementationAsync(delegateInstance, typeDescriptor);
        //                callees.Add(aMethod);
        //            }
        //        }
        //        else
        //        {
        //            // if Count is 0, it is a delegate that do not came form an instance variable
        //            callees.Add(delegateInstance);
        //        }
        //    }

        //    return callees;
        //}


		public DelegateVariableNode CalleeDelegate { get; private set; }
        public ISet<MethodDescriptor> ResolvedCallees { get; set; } 
	}

    public class ReturnInfo
    {
        public ISet<TypeDescriptor> ReturnPotentialTypes { get;  set; }
        public ISet<TypeDescriptor> InstantiatedTypes { get;  set; }
        public CallContext CallerContext { get; private set; }
       

        public ReturnInfo(CallContext caller)
        {
            this.InstantiatedTypes = new HashSet<TypeDescriptor>();
            this.ReturnPotentialTypes = new HashSet<TypeDescriptor>();
            this.CallerContext = caller;
        }
    }
}
