// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ReachingTypeAnalysis
{
	internal class CallContext
	{
		internal AnalysisMethod Caller { get; private set; }
		internal AnalysisNode CallLHS { get; private set; }
		internal AnalysisNode Invocation { get; private set; }

		internal CallContext(AnalysisMethod caller, AnalysisNode lhs, AnalysisNode inv)
		{
			this.Caller = caller;
			this.CallLHS = lhs;
			this.Invocation = inv;
		}

		public override bool Equals(object obj)
		{
			var c2 = obj as CallContext;
			return this.Caller.Equals(c2.Caller) && (this.CallLHS == null || this.CallLHS.Equals(c2.CallLHS))
				&& (this.Invocation == null || c2.Invocation == null || this.Invocation.Equals(c2.Invocation));
		}
		public override int GetHashCode()
		{
			int lhsHashCode = this.CallLHS == null ? 1 : this.CallLHS.GetHashCode();
			return this.Caller.GetHashCode() + lhsHashCode;
		}
	}

	internal class StatementProcessor
	{
		// t-digarb: Should I include context information?
		private ISet<CallContext> callers = new HashSet<CallContext>();
		internal AnalysisMethod Method { get; private set; }
		internal AnalysisNode ThisRef { get; private set; }
		internal IEnumerable<AnalysisNode> ParameterNodes { get; private set; }
		internal AnalysisNode ReturnVariable { get; private set; }
		internal PropagationGraph PropagationGraph { get; private set; }
		internal ISet<AnalysisType> InstantiatedTypes { get; private set; }
		internal ISet<AnalysisType> RemovedTypes { get; private set; }

		//public StatementProcessor(M m, E rv, E thisRef, IList<E> parameters, MethodEntity<E, T, M> containerEntity)
		public StatementProcessor(AnalysisMethod m,
			AnalysisNode rv, AnalysisNode thisRef,
			IEnumerable<AnalysisNode> parameters)
		{
			// this.containerEntity = containerEntity;
			this.ReturnVariable = rv;
			this.Method = m;
			this.ParameterNodes = parameters;
			this.ThisRef = thisRef;

			this.PropagationGraph = new PropagationGraph();
			this.InstantiatedTypes = new HashSet<AnalysisType>();
			this.RemovedTypes = new HashSet<AnalysisType>();

			if (rv != null)
			{
				PropagationGraph.AddRet(rv);
			}
			if (thisRef != null)
			{
				PropagationGraph.Add(thisRef);
			}

			foreach (var parameter in this.ParameterNodes)
			{
				if (parameter != null)
				{
					PropagationGraph.Add(parameter);
				}
			}
			//this.dispatcher = dispatcher;            
		}

		/// <summary>
		///  In the incremental analysis we want to use this commands over an existing propagation graph
		/// </summary>
		/// <param name="propagationGraph"></param>
		public StatementProcessor(AnalysisMethod method, AnalysisNode rv,
			AnalysisNode thisRef, IList<AnalysisNode> parameters,
			PropagationGraph propagationGraph)
		{
			this.PropagationGraph = propagationGraph;
			this.ReturnVariable = rv;
			this.Method = method;
			this.ParameterNodes = parameters;
			this.ThisRef = thisRef;

			this.InstantiatedTypes = new HashSet<AnalysisType>();
			this.RemovedTypes = new HashSet<AnalysisType>();
		}

		public void RegisterAssignment(AnalysisNode lhs, AnalysisNode rhs)
		{
			if (lhs != null && rhs != null)
			{
				PropagationGraph.AddEdge(rhs, lhs);

				if (!rhs.AnalysisType.IsConcreteType)
				{
					PropagationGraph.Add(rhs, rhs.AnalysisType);
					PropagationGraph.AddToWorkList(rhs);
				}
			}
		}

		public void RegisterLocalVariable(AnalysisNode v)
		{
			PropagationGraph.Add(v);
		}

		public void RegisterNewExpressionAssignment(AnalysisNode lhs, AnalysisType t)
		{
			if (lhs != null)
			{
				this.PropagationGraph.Add(lhs, t);
				this.PropagationGraph.AddToWorkList(lhs);
			}
			this.InstantiatedTypes.Add(t);
		}

		#region Call management (calls, delegates, constructors, properties)
		public void RegisterConstructorCall(AnalysisMethod callee, IList<AnalysisNode> arguments, AnalysisNode lhs, AnalysisNode callNode)
		{
			Contract.Requires(callee != null);
			Contract.Requires(callNode != null);
			Contract.Requires(arguments != null);
			//var argumentValues = arguments.Select(a => a!=null?worker.GetTypes(a):new HashSet<Type>());

			var callExp = new CallInfo(this.Method, callNode, callee, arguments, lhs, true);

			PropagationGraph.AddCall(callExp, callNode);
			PropagationGraph.AddToWorkList(callNode);
			foreach (var a in arguments)
			{
				if (a != null)
				{
					PropagationGraph.AddEdge(a, callNode);
				}
			}
		}

		internal void RegisterDelegateAssignment(AnalysisNode lhs, AnalysisMethod m)
		{
			PropagationGraph.AddDelegate(lhs, m);
			PropagationGraph.AddToWorkList(lhs);
		}

		public void RegisterStaticCall(AnalysisMethod callee, IList<AnalysisNode> arguments,
			AnalysisNode lhs, AnalysisNode callNode)
		{
			Contract.Requires(callee != null);
			Contract.Requires(callNode != null);
			Contract.Requires(arguments != null);

			var callExp = new CallInfo(this.Method, callNode, callee, arguments, lhs, false);

			RegisterInvocation(arguments, callNode, callExp);

		}

		public void RegisterVirtualCall(AnalysisMethod callee, AnalysisNode receiver,
			IList<AnalysisNode> arguments, AnalysisNode lhs, AnalysisNode callNode)
		{
			Contract.Requires(receiver != null);
			Contract.Requires(callee != null);
			Contract.Requires(callNode != null);
			Contract.Requires(arguments != null);

			var callExp = new CallInfo(this.Method, callNode, callee, receiver, arguments, lhs, false);
			RegisterInvocation(arguments, callNode, callExp);
			if (receiver != null)
			{
				PropagationGraph.AddEdge(receiver, callNode);
			}

		}

		public void RegisterPropertyCall(AnalysisMethod callee, AnalysisNode receiver, IList<AnalysisNode> arguments, AnalysisNode lhs, AnalysisNode callNode)
		{
			Contract.Requires(callNode != null);
			Contract.Requires(callee != null);

			var callExp = new CallInfo(this.Method, callNode, callee, receiver, arguments, lhs, false);
			RegisterInvocation(arguments, callNode, callExp);
			if (receiver != null)
			{
				PropagationGraph.AddEdge(receiver, callNode);
			}
		}

		public void RegisterStaticDelegateCall(AnalysisMethod callee, IList<AnalysisNode> arguments, AnalysisNode lhs, AnalysisNode delegateNode, AnalysisNode callNode)
		{
			Contract.Requires(delegateNode != null);
			Contract.Requires(arguments != null);
			Contract.Requires(callee != null);

			var callExp = new DelegateCallInfo(this.Method, callNode, delegateNode, arguments, lhs);

			// RegisterInvocation(arguments, delegateNode, callExp);
			RegisterInvocation(arguments, callNode, callExp);
		}

		public void RegisterVirtualDelegateCall(AnalysisMethod callee, AnalysisNode receiver, IList<AnalysisNode> arguments, AnalysisNode lhs, AnalysisNode delegateNode)
		{
			Contract.Requires(receiver != null);
			Contract.Requires(arguments != null);
			Contract.Requires(lhs != null);
			Contract.Requires(callee != null);
			Contract.Requires(delegateNode != null);

			var callExp = new DelegateCallInfo(this.Method, delegateNode, receiver, arguments, lhs);
			this.PropagationGraph.AddEdge(receiver, delegateNode);

			RegisterInvocation(arguments, delegateNode, callExp);
		}


		private void RegisterInvocation(IList<AnalysisNode> arguments, AnalysisNode invocationNode, AnalysisInvocationExpession callExp)
		{
			Contract.Requires(callExp != null);
			Contract.Requires(arguments != null);
			Contract.Requires(invocationNode != null);

			this.PropagationGraph.AddCall(callExp, invocationNode);
			this.PropagationGraph.AddToWorkList(invocationNode);

			foreach (var a in arguments)
			{
				if (a != null)
				{
					PropagationGraph.AddEdge(a, invocationNode);
				}
			}
		}

		public void RegisterCallLHS(AnalysisNode callNode, AnalysisNode lhs)
		{
			var callExp = PropagationGraph.GetInvocationInfo(callNode);
			callExp.LHS = lhs;
		}
		#endregion

		#region Incremental analysis - Removal of types
		public void RegisterRemoveNewExpressionAssignment(AnalysisNode lhs)
		{
			if (lhs != null)
			{
				var types = PropagationGraph.GetTypes(lhs);

				this.RemovedTypes.UnionWith(types);

				PropagationGraph.RemoveTypes(lhs, types);
				PropagationGraph.AddToDeletionWorkList(lhs);

			}
		}
		public void RegisterRemoveType(AnalysisNode lhs, AnalysisType type)
		{
			if (lhs != null)
			{
				var types = new HashSet<AnalysisType>();
				types.Add(type);

				this.RemovedTypes.Add(type);

				PropagationGraph.RemoveTypes(lhs, types);
				PropagationGraph.AddToDeletionWorkList(lhs);

			}
		}
		public void RegisterRemoveTypes(AnalysisNode lhs, IEnumerable<AnalysisType> types)
		{
			if (lhs != null)
			{
				this.RemovedTypes.UnionWith(types);

				PropagationGraph.RemoveTypes(lhs, types);
				PropagationGraph.AddToDeletionWorkList(lhs);

			}
		}

		public void RegisterRemoveAssignment(AnalysisNode lhs, AnalysisNode rhs)
		{
			if (lhs != null && rhs != null)
			{
				var typesToDelete = PropagationGraph.TypesInEdge(rhs, lhs);
				PropagationGraph.RemoveTypes(lhs, typesToDelete);
				PropagationGraph.AddToDeletionWorkList(lhs);
			}
		}
		#endregion

		public void RegisterRet(AnalysisNode rn)
		{
			PropagationGraph.AddEdge(rn, this.ReturnVariable);
		}
	}
}