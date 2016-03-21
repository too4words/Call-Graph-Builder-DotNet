// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis
{
	[Serializable]
	public abstract class CallInfo
	{
		public bool IsConstructor { get; set; }
		public MethodDescriptor Caller { get; set; }
		public IList<PropGraphNodeDescriptor> Arguments { get; set; }
		public PropGraphNodeDescriptor Receiver { get; set; }
		public VariableNode LHS { get; set; }
		public AnalysisCallNode CallNode { get; set; }
		public IList<ISet<TypeDescriptor>> ArgumentsPossibleTypes { get; set; }
		public ISet<ResolvedCallee> PossibleCallees { get; set; }

		public CallInfo(MethodDescriptor caller, AnalysisCallNode callNode,
			PropGraphNodeDescriptor receiver, IList<PropGraphNodeDescriptor> arguments, VariableNode lhs)
		{
			this.Caller = caller;
			this.Arguments = arguments;
			this.LHS = lhs;
			this.Receiver = receiver;
			this.CallNode = callNode;
			this.ArgumentsPossibleTypes = new List<ISet<TypeDescriptor>>();
			this.PossibleCallees = new HashSet<ResolvedCallee>();
		}

		public CallInfo(CallInfo other, IEnumerable<ResolvedCallee> possibleCallees)
		{
			this.IsConstructor = other.IsConstructor;
			this.Caller = other.Caller;
			this.LHS = other.LHS;
			this.Receiver = other.Receiver;
			this.CallNode = other.CallNode;
			this.Arguments = new List<PropGraphNodeDescriptor>(other.Arguments);
			this.ArgumentsPossibleTypes = new List<ISet<TypeDescriptor>>(other.ArgumentsPossibleTypes);
			this.PossibleCallees = new HashSet<ResolvedCallee>(possibleCallees);
		}

		public abstract CallInfo Clone(IEnumerable<ResolvedCallee> possibleCallees);

		public bool IsStatic
		{
			get { return this.Receiver == null; }
		}
	}

	[Serializable]
	public class MethodCallInfo : CallInfo
	{
		public MethodDescriptor Method { get; private set; }

		public MethodCallInfo(MethodDescriptor caller, AnalysisCallNode callNode, MethodDescriptor method,
			PropGraphNodeDescriptor receiver, IList<PropGraphNodeDescriptor> arguments,
			VariableNode lhs, bool isConstructor)
			: base(caller, callNode, receiver, arguments, lhs)
		{
			this.IsConstructor = isConstructor;
			this.Method = method;
		}

		public MethodCallInfo(MethodDescriptor caller, AnalysisCallNode callNode,
			MethodDescriptor method, IList<PropGraphNodeDescriptor> arguments,
			VariableNode lhs, bool isConstructor)
			: this(caller, callNode, method, null, arguments, lhs, isConstructor)
		{
		}

		public MethodCallInfo(MethodCallInfo other, IEnumerable<ResolvedCallee> possibleCallees)
			: base(other, possibleCallees)
		{
			this.Method = other.Method;
		}

		public override CallInfo Clone(IEnumerable<ResolvedCallee> possibleCallees)
		{
			var result = new MethodCallInfo(this, possibleCallees);
			return result;
		}
	}

	[Serializable]
	public class DelegateCallInfo : CallInfo
	{
		public DelegateVariableNode Delegate { get; private set; }

		public DelegateCallInfo(MethodDescriptor caller, AnalysisCallNode callNode,
			DelegateVariableNode calleeDelegate, PropGraphNodeDescriptor receiver,
			IList<PropGraphNodeDescriptor> arguments, VariableNode lhs)
			: base(caller, callNode, receiver, arguments, lhs)
		{
			this.Delegate = calleeDelegate;
		}

		public DelegateCallInfo(MethodDescriptor caller, AnalysisCallNode callNode,
			DelegateVariableNode @delegate, IList<PropGraphNodeDescriptor> arguments, VariableNode lhs)
			: this(caller, callNode, @delegate, null, arguments, lhs)
		{
		}

		public DelegateCallInfo(DelegateCallInfo other, IEnumerable<ResolvedCallee> possibleCallees)
			: base(other, possibleCallees)
		{
			this.Delegate = other.Delegate;
		}

		public override CallInfo Clone(IEnumerable<ResolvedCallee> possibleCallees)
		{
			var result = new DelegateCallInfo(this, possibleCallees);
			return result;
		}
	}

	[Serializable]
	public class ReturnInfo
	{
		public ISet<TypeDescriptor> ResultPossibleTypes { get; set; }
		public CallContext CallerContext { get; private set; }
		public MethodDescriptor Callee { get; set; }

		public ReturnInfo(MethodDescriptor callee, CallContext callerContext)
		{
			this.ResultPossibleTypes = new HashSet<TypeDescriptor>();
			this.Callee = callee;
			this.CallerContext = callerContext;
		}
	}
}
