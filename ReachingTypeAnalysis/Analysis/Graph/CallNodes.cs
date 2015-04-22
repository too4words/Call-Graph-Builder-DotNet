// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace ReachingTypeAnalysis
{
	internal abstract class AnalysisInvocationExpession
	{
		internal AnalysisInvocationExpession(AnalysisMethod caller, AnalysisNode callNode, AnalysisNode reciever, IList<AnalysisNode> arguments, AnalysisNode lhs)
		{
			Caller = caller;
			Arguments = arguments;
			LHS = lhs;
			Receiver = reciever;
			IsStatic = false;
			CallNode = callNode;
		}
		internal AnalysisInvocationExpession(AnalysisMethod caller, AnalysisNode callNode, IList<AnalysisNode> arguments, AnalysisNode lhs)
		{
			Caller = caller;
			Arguments = arguments;
			LHS = lhs;
			IsStatic = true;
			CallNode = callNode;
		}

		internal abstract ISet<AnalysisMethod> ComputeCalleesForNode(PropagationGraph propGraph);

		internal ISet<AnalysisType> GetPotentialTypes(AnalysisNode n, PropagationGraph propGraph)
		{
			var result = new HashSet<AnalysisType>();
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
		internal AnalysisMethod Caller { get; set; }
		internal IList<AnalysisNode> Arguments { get; set; }
		internal AnalysisNode Receiver { get; set; }
		internal AnalysisNode LHS { get; set; }
		internal ISet<AnalysisType> InstatiatedTypes { get; set; }

		internal AnalysisNode CallNode { get; set; }
	}

	internal class CallInfo : AnalysisInvocationExpession
	{
		internal CallInfo(AnalysisMethod caller, AnalysisNode callNode, AnalysisMethod callee, AnalysisNode reciever, IList<AnalysisNode> arguments, AnalysisNode lhs, bool isConstructor)
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

		internal CallInfo(AnalysisMethod caller, AnalysisNode callNode, AnalysisMethod callee, IList<AnalysisNode> arguments, AnalysisNode lhs, bool isConstructor)
			: base(caller, callNode, arguments, lhs)
		{
			Caller = caller;
			Callee = callee;
			Arguments = arguments;
			LHS = lhs;
			Receiver = default(AnalysisNode);
			IsStatic = true;
			IsConstructor = isConstructor;
		}

		internal override ISet<AnalysisMethod> ComputeCalleesForNode(PropagationGraph propGraph)
		{
			var calleesForNode = new HashSet<AnalysisMethod>();
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

		public AnalysisMethod Callee { get; private set; }
	}

	internal class DelegateCallInfo : AnalysisInvocationExpession
	{
        internal DelegateCallInfo(AnalysisMethod caller, AnalysisNode callNode, AnalysisNode calleeDelegate, IList<AnalysisNode> arguments, AnalysisNode lhs)
			: base(caller, callNode, arguments, lhs)
		{
			Caller = caller;
			CalleeDelegate = calleeDelegate;
			Arguments = arguments;
			LHS = lhs;
		}
		internal DelegateCallInfo(AnalysisMethod caller, AnalysisNode callNode, AnalysisNode calleeDelegate, AnalysisNode receiver, IList<AnalysisNode> arguments, AnalysisNode lhs)
			: base(caller, callNode, arguments, lhs)
		{
			Caller = caller;
			CalleeDelegate = calleeDelegate;
			Receiver = receiver;
			Arguments = arguments;
			LHS = lhs;
		}

		internal override ISet<AnalysisMethod> ComputeCalleesForNode(PropagationGraph propGraph)
		{
			return GetDelegateCallees(this.CalleeDelegate, propGraph);
		}
		private ISet<AnalysisMethod> GetDelegateCallees(AnalysisNode delegateNode, PropagationGraph propGraph)
		{
			var callees = new HashSet<AnalysisMethod>();
			var types = propGraph.GetTypes(delegateNode);
			foreach (var delegateInstance in propGraph.GetDelegates(delegateNode))
			{
				if (types.Count() > 0)
				{
					foreach (var t in types)
					{
						// TO-DO!!!
						// Ugly: I'll fix it
						var aMethod = delegateInstance.FindMethodImplementation((AnalysisType)t);
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

		internal AnalysisNode CalleeDelegate { get; private set; }
	}
}
