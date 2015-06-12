// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using ReachingTypeAnalysis.Roslyn;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace ReachingTypeAnalysis
{
	[Serializable]
	internal abstract class AnalysisInvocationExpession
	{
		internal AnalysisInvocationExpession(MethodDescriptor caller, AnalysisCallNode callNode, 
                        PropGraphNodeDescriptor reciever, 
                        IList<PropGraphNodeDescriptor> arguments, VariableNode lhs)
		{
			Caller = caller;
			Arguments = arguments;
			LHS = lhs;
			Receiver = reciever;
			IsStatic = false;
            CallNode = callNode;
            InstatiatedTypes = new HashSet<TypeDescriptor>();
        }
		internal AnalysisInvocationExpession(MethodDescriptor caller, 
                        AnalysisCallNode callNode, IList<PropGraphNodeDescriptor> arguments, VariableNode lhs)
		{
			Caller = caller;
			Arguments = arguments;
			LHS = lhs;
			IsStatic = true;
			CallNode = callNode;
            InstatiatedTypes = new HashSet<TypeDescriptor>();
		}

		internal abstract ISet<MethodDescriptor> ComputeCalleesForNode(PropagationGraph propGraph, ProjectCodeProvider codeProvider);

		internal ISet<TypeDescriptor> GetPotentialTypes(PropGraphNodeDescriptor n, PropagationGraph propGraph, ProjectCodeProvider codeProvider)
		{
			var result = new HashSet<TypeDescriptor>();
			foreach (var typeDescriptor in propGraph.GetTypes(n))
			{
				// TO-DO fix by adding a where T: AnalysisType
				if (typeDescriptor.IsConcreteType)
				{
					result.Add(typeDescriptor);
				}
				else
				{
                    // If it is a declaredTyped it means we were not able to compute a concrete type
                    // Therefore, we instantiate all compatible types for the set of instantiated types
					//result.UnionWith(this.InstatiatedTypes.Where(iType => iType.IsSubtype(typeDescriptor)));
                    Contract.Assert(this.InstatiatedTypes != null);
                    // Diego: This requires a Code Provider. Now it will simply fail.
                    result.UnionWith(this.InstatiatedTypes.Where(candidateTypeDescriptor
                                            => codeProvider.IsSubtype(candidateTypeDescriptor, typeDescriptor)));
				}
			}
			return result;
		}

		internal bool IsStatic { get; set; }
		internal bool IsConstructor { get; set; }
		// public M Callee;
		internal MethodDescriptor Caller { get; set; }
		internal IList<PropGraphNodeDescriptor> Arguments { get; set; }
		internal PropGraphNodeDescriptor Receiver { get; set; }
		internal VariableNode LHS { get; set; }
		internal ISet<TypeDescriptor> InstatiatedTypes { get; set; }
		internal AnalysisCallNode CallNode { get; set; }
	}

	[Serializable]
	internal class CallInfo : AnalysisInvocationExpession
	{
		internal CallInfo(MethodDescriptor caller, AnalysisCallNode callNode, MethodDescriptor callee, 
                    PropGraphNodeDescriptor reciever, IList<PropGraphNodeDescriptor> arguments, 
                    VariableNode lhs, bool isConstructor)
			: base(caller, callNode, reciever, arguments, lhs)
		{
			Caller = caller;
			Callee = callee;
			Arguments = arguments;
			LHS = lhs;
			Receiver = reciever;
			IsStatic = false;
			IsConstructor = isConstructor;
		}

		internal CallInfo(MethodDescriptor caller, AnalysisCallNode callNode, 
                    MethodDescriptor callee, IList<PropGraphNodeDescriptor> arguments, 
                    VariableNode lhs, bool isConstructor)
			: base(caller, callNode, arguments, lhs)
		{
			Caller = caller;
			Callee = callee;
			Arguments = arguments;
			LHS = lhs;
			Receiver = default(VariableNode);
			IsStatic = true;
			IsConstructor = isConstructor;
		}

		internal override ISet<MethodDescriptor> ComputeCalleesForNode(PropagationGraph propGraph, ProjectCodeProvider codeProvider)
		{
			var calleesForNode = new HashSet<MethodDescriptor>();
			if (this.Receiver != null)
			{
                // I replaced the invocation for a local call to mark that functionality is missing
                //var callees = GetPotentialTypes(this.Receiver, propGraph)
                //    .Select(t => this.Callee.FindMethodImplementation(t));
                var callees = GetPotentialTypes(this.Receiver, propGraph, codeProvider)
                        .Select(t => codeProvider.FindMethodImplementation(this.Callee,t));
				calleesForNode.UnionWith(callees);
			}
			else
			{
				calleesForNode.Add(this.Callee);
			}
			return calleesForNode;
		}


		public MethodDescriptor Callee { get; private set; }
	}

	[Serializable]
	internal class DelegateCallInfo : AnalysisInvocationExpession
	{
        internal DelegateCallInfo(MethodDescriptor caller, AnalysisCallNode callNode, 
                DelegateVariableNode calleeDelegate, IList<PropGraphNodeDescriptor> arguments, VariableNode lhs)
			: base(caller, callNode, arguments, lhs)
		{
			Caller = caller;
			CalleeDelegate = calleeDelegate;
			Arguments = arguments;
			LHS = lhs;
		}

		internal DelegateCallInfo(MethodDescriptor caller, AnalysisCallNode callNode, 
                    DelegateVariableNode calleeDelegate, PropGraphNodeDescriptor receiver, 
                    IList<PropGraphNodeDescriptor> arguments, VariableNode lhs)
			: base(caller, callNode, arguments, lhs)
		{
			Caller = caller;
			CalleeDelegate = calleeDelegate;
			Receiver = receiver;
			Arguments = arguments;
			LHS = lhs;
		}

		internal override ISet<MethodDescriptor> ComputeCalleesForNode(PropagationGraph propGraph, ProjectCodeProvider codeProvider)
		{
			return GetDelegateCallees(this.CalleeDelegate, propGraph, codeProvider);
		}

		private ISet<MethodDescriptor> GetDelegateCallees(VariableNode delegateNode, PropagationGraph propGraph, ProjectCodeProvider codeProvider)
		{
			var callees = new HashSet<MethodDescriptor>();
			var typeDescriptors = propGraph.GetTypes(delegateNode);
			foreach (var delegateInstance in propGraph.GetDelegates(delegateNode))
			{
				if (typeDescriptors.Count() > 0)
				{
					foreach (var typeDescriptor in typeDescriptors)
					{
						// TO-DO!!!
						// Ugly: I'll fix it
						//var aMethod = delegateInstance.FindMethodImplementation(type);
                        var aMethod = codeProvider.FindMethodImplementation(delegateInstance,typeDescriptor);
						callees.Add(aMethod);
					}
				}
				else
				{
					// if Count is 0, it is a delegate that do not came form an instance variable
					callees.Add(delegateInstance);
				}
			}

			return callees;
		}

		internal DelegateVariableNode CalleeDelegate { get; private set; }
	}
}
