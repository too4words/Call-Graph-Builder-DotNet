// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace ReachingTypeAnalysis
{
	internal abstract class AnalysisInvocationExpession
	{
		internal AnalysisInvocationExpession(MethodDescriptor caller, LocationDescriptor callNode, VariableDescriptor reciever, IList<VariableDescriptor> arguments, VariableDescriptor lhs)
		{
			Caller = caller;
			Arguments = arguments;
			LHS = lhs;
			Receiver = reciever;
			IsStatic = false;
			CallNode = callNode;
		}
		internal AnalysisInvocationExpession(MethodDescriptor caller, LocationDescriptor callNode, IList<VariableDescriptor> arguments, VariableDescriptor lhs)
		{
			Caller = caller;
			Arguments = arguments;
			LHS = lhs;
			IsStatic = true;
			CallNode = callNode;
		}

		internal abstract ISet<MethodDescriptor> ComputeCalleesForNode(PropagationGraph propGraph);

		internal ISet<TypeDescriptor> GetPotentialTypes(VariableDescriptor n, PropagationGraph propGraph)
		{
			var result = new HashSet<TypeDescriptor>();
			foreach (var type in propGraph.GetTypes(n))
			{
				// TO-DO fix by adding a where T: AnalysisType
				if (type.IsConcreteType)
				{
					result.Add(type);
				}
				else
				{
					result.UnionWith(this.InstatiatedTypes.Where(iType => iType.IsSubtype(type)));
				}
			}
			return result;
		}

		internal bool IsStatic { get; set; }
		internal bool IsConstructor { get; set; }
		// public M Callee;
		internal MethodDescriptor Caller { get; set; }
		internal IList<VariableDescriptor> Arguments { get; set; }
		internal VariableDescriptor Receiver { get; set; }
		internal VariableDescriptor LHS { get; set; }
		internal ISet<TypeDescriptor> InstatiatedTypes { get; set; }
		internal LocationDescriptor CallNode { get; set; }
	}

	internal class CallInfo : AnalysisInvocationExpession
	{
		internal CallInfo(MethodDescriptor caller, LocationDescriptor callNode, MethodDescriptor callee, VariableDescriptor reciever, IList<VariableDescriptor> arguments, VariableDescriptor lhs, bool isConstructor)
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

		internal CallInfo(MethodDescriptor caller, LocationDescriptor callNode, MethodDescriptor callee, IList<VariableDescriptor> arguments, VariableDescriptor lhs, bool isConstructor)
			: base(caller, callNode, arguments, lhs)
		{
			Caller = caller;
			Callee = callee;
			Arguments = arguments;
			LHS = lhs;
			Receiver = default(VariableDescriptor);
			IsStatic = true;
			IsConstructor = isConstructor;
		}

		internal override ISet<MethodDescriptor> ComputeCalleesForNode(PropagationGraph propGraph)
		{
			var calleesForNode = new HashSet<MethodDescriptor>();
			if (this.Receiver != null)
			{
				var callees = GetPotentialTypes(this.Receiver, propGraph)
					.Select(t => this.Callee.FindMethodImplementation(t));
				//var callees = PropGraph.GetTypes(cn.Receiver).Select(t => cn.Callee.FindMethodImplementation(t));
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

	internal class DelegateCallInfo : AnalysisInvocationExpession
	{
        internal DelegateCallInfo(MethodDescriptor caller, LocationDescriptor callNode, VariableDescriptor calleeDelegate, IList<VariableDescriptor> arguments, VariableDescriptor lhs)
			: base(caller, callNode, arguments, lhs)
		{
			Caller = caller;
			CalleeDelegate = calleeDelegate;
			Arguments = arguments;
			LHS = lhs;
		}

		internal DelegateCallInfo(MethodDescriptor caller, LocationDescriptor callNode, VariableDescriptor calleeDelegate, VariableDescriptor receiver, IList<VariableDescriptor> arguments, VariableDescriptor lhs)
			: base(caller, callNode, arguments, lhs)
		{
			Caller = caller;
			CalleeDelegate = calleeDelegate;
			Receiver = receiver;
			Arguments = arguments;
			LHS = lhs;
		}

		internal override ISet<MethodDescriptor> ComputeCalleesForNode(PropagationGraph propGraph)
		{
			return GetDelegateCallees(this.CalleeDelegate, propGraph);
		}

		private ISet<MethodDescriptor> GetDelegateCallees(VariableDescriptor delegateNode, PropagationGraph propGraph)
		{
			var callees = new HashSet<MethodDescriptor>();
			var types = propGraph.GetTypes(delegateNode);
			foreach (var delegateInstance in propGraph.GetDelegates(delegateNode))
			{
				if (types.Count() > 0)
				{
					foreach (var type in types)
					{
						// TO-DO!!!
						// Ugly: I'll fix it
						var aMethod = delegateInstance.FindMethodImplementation(type);
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

		internal VariableDescriptor CalleeDelegate { get; private set; }
	}
}
