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
		//public ISet<TypeDescriptor> InstantiatedTypes { get; set; }
		public AnalysisCallNode CallNode { get; set; }
		public ISet<TypeDescriptor> ReceiverPossibleTypes { get; set; }
		public IList<ISet<TypeDescriptor>> ArgumentsPossibleTypes { get; set; }
		public ISet<MethodDescriptor> PossibleCallees { get; set; }

		public CallInfo(MethodDescriptor caller, AnalysisCallNode callNode,
			PropGraphNodeDescriptor receiver,
			IList<PropGraphNodeDescriptor> arguments, VariableNode lhs)
		{
			this.Caller = caller;
			this.Arguments = arguments;
			this.LHS = lhs;
			this.Receiver = receiver;
			this.CallNode = callNode;
			//this.InstantiatedTypes = new HashSet<TypeDescriptor>();
			this.ReceiverPossibleTypes = new HashSet<TypeDescriptor>();
			this.ArgumentsPossibleTypes = new List<ISet<TypeDescriptor>>();
			this.PossibleCallees = new HashSet<MethodDescriptor>();
		}

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
	}

	[Serializable]
	public class ReturnInfo
	{
		public ISet<TypeDescriptor> ResultPossibleTypes { get; set; }
		//public ISet<TypeDescriptor> InstantiatedTypes { get; set; }
		public CallContext CallerContext { get; private set; }
		public MethodDescriptor Callee { get; set; }

		public ReturnInfo(MethodDescriptor callee, CallContext callerContext)
		{
			//this.InstantiatedTypes = new HashSet<TypeDescriptor>();
			this.ResultPossibleTypes = new HashSet<TypeDescriptor>();
			this.Callee = callee;
			this.CallerContext = callerContext;
		}
	}
}
