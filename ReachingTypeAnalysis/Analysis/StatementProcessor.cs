using ReachingTypeAnalysis.Roslyn;
using System;
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ReachingTypeAnalysis.Analysis
{
 
	internal class StatementProcessor
	{
		// t-digarb: Should I include context information?
		private ISet<CallContext> callers = new HashSet<CallContext>();
		internal MethodDescriptor Method { get; private set; }
		internal PropGraphNodeDescriptor ThisRef { get; private set; }
		internal IEnumerable<PropGraphNodeDescriptor> ParameterNodes { get; private set; }
		internal PropGraphNodeDescriptor ReturnVariable { get; private set; }
		internal PropagationGraph PropagationGraph { get; private set; }
		internal ISet<TypeDescriptor> InstantiatedTypes { get; private set; }
		internal ISet<TypeDescriptor> RemovedTypes { get; private set; }

        private IDictionary<AnonymousMethodDescriptor, MethodEntity> anonymousMethods = new Dictionary<AnonymousMethodDescriptor, MethodEntity>();

        internal IDictionary<AnonymousMethodDescriptor, MethodEntity> AnonymousMethods
        {
            get { return anonymousMethods; }
        }

		public StatementProcessor(MethodDescriptor m,
			PropGraphNodeDescriptor rv, PropGraphNodeDescriptor thisRef,
			IEnumerable<PropGraphNodeDescriptor> parameters) 
        //    ,
        //    ICodeProvider codeProvider)
        {
			// this.containerEntity = containerEntity;
			this.ReturnVariable = rv;
			this.Method = m;
			this.ParameterNodes = parameters;
			this.ThisRef = thisRef;
			this.PropagationGraph = new PropagationGraph();
			this.InstantiatedTypes = new HashSet<TypeDescriptor>();
			this.RemovedTypes = new HashSet<TypeDescriptor>();

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
		public StatementProcessor(MethodDescriptor method, VariableNode rv,
                			VariableNode thisRef, IList<PropGraphNodeDescriptor> parameters,
			PropagationGraph propagationGraph)
		{
			this.PropagationGraph = propagationGraph;
			this.ReturnVariable = rv;
			this.Method = method;
			this.ParameterNodes = parameters;
			this.ThisRef = thisRef;

			this.InstantiatedTypes = new HashSet<TypeDescriptor>();
			this.RemovedTypes = new HashSet<TypeDescriptor>();
		}

		public void RegisterAssignment(PropGraphNodeDescriptor lhs, PropGraphNodeDescriptor rhs)
		{
			if (lhs != null && rhs != null)
			{
				PropagationGraph.AddEdge(rhs, lhs);

				if (!rhs.Type.IsConcreteType)
				{
					PropagationGraph.Add(rhs, rhs.Type);
					PropagationGraph.AddToWorkList(rhs);
				}
			}
		}

		public void RegisterLocalVariable(PropGraphNodeDescriptor v)
		{
			PropagationGraph.Add(v);
		}

		public void RegisterNewExpressionAssignment(PropGraphNodeDescriptor lhs, TypeDescriptor t)
		{
			if (lhs != null)
			{
				this.PropagationGraph.Add(lhs, t);
				this.PropagationGraph.AddToWorkList(lhs);
			}
			this.InstantiatedTypes.Add(t);
		}

		#region Call management (calls, delegates, constructors, properties)
		public void RegisterConstructorCall(MethodDescriptor callee, IList<PropGraphNodeDescriptor> arguments, 
                                            VariableNode lhs, AnalysisCallNode callNode)
		{
			Contract.Requires(callee != null);
			Contract.Requires(callNode != null);
			Contract.Requires(arguments != null);
			//var argumentValues = arguments.Select(a => a!=null?worker.GetTypes(a):new HashSet<Type>());

			var callExp = new MethodCallInfo(this.Method, callNode, callee, arguments, lhs, true);

			PropagationGraph.AddCall(callExp, callNode);
			PropagationGraph.AddToWorkList(callNode);
			foreach (var argument in arguments)
			{
				if (argument != null)
				{
					PropagationGraph.AddEdge(argument, callNode);
				}
			}
		}

		internal void RegisterDelegateAssignment(DelegateVariableNode lhs, MethodDescriptor m)
		{
			PropagationGraph.AddDelegate(lhs, m);
			PropagationGraph.AddToWorkList(lhs);
		}

        internal void RegisterAnonymousMethod(AnonymousMethodDescriptor methodDescriptor, MethodEntity methodEntity)
        {
            this.anonymousMethods[methodDescriptor] = methodEntity;
        }

		public void RegisterStaticCall(MethodDescriptor callee, IList<PropGraphNodeDescriptor> arguments,
			VariableNode lhs, AnalysisCallNode callNode)
		{
			Contract.Requires(callee != null);
			Contract.Requires(callNode != null);
			Contract.Requires(arguments != null);

			var callExp = new MethodCallInfo(this.Method, callNode, callee, arguments, lhs, false);

			RegisterInvocation(arguments, callNode, callExp);

		}

		public void RegisterVirtualCall(MethodDescriptor callee, VariableNode receiver,
			IList<PropGraphNodeDescriptor> arguments, VariableNode lhs, AnalysisCallNode callNode)
		{
			Contract.Requires(receiver != null);
			Contract.Requires(callee != null);
			Contract.Requires(callNode != null);
			Contract.Requires(arguments != null);

			var callExp = new MethodCallInfo(this.Method, callNode, callee, receiver, arguments, lhs, false);
			RegisterInvocation(arguments, callNode, callExp);
			if (receiver != null)
			{
				PropagationGraph.AddEdge(receiver, callNode);
			}

		}

		public void RegisterPropertyCall(MethodDescriptor callee, PropGraphNodeDescriptor receiver, IList<PropGraphNodeDescriptor> arguments, 
                                        VariableNode lhs, AnalysisCallNode callNode)
		{
			Contract.Requires(callNode != null);
			Contract.Requires(callee != null);

			var callExp = new MethodCallInfo(this.Method, callNode, callee, receiver, arguments, lhs, false);
			RegisterInvocation(arguments, callNode, callExp);
			if (receiver != null)
			{
				PropagationGraph.AddEdge(receiver, callNode);
			}
		}

		public void RegisterStaticDelegateCall(MethodDescriptor callee, IList<PropGraphNodeDescriptor> arguments, 
                        VariableNode lhs, DelegateVariableNode delegateNode, AnalysisCallNode callNode)
		{
			Contract.Requires(delegateNode != null);
			Contract.Requires(arguments != null);
			Contract.Requires(callee != null);

			var callExp = new DelegateCallInfo(this.Method, callNode, delegateNode, arguments, lhs);

			// RegisterInvocation(arguments, delegateNode, callExp);
			RegisterInvocation(arguments, callNode, callExp);
		}

        //public void RegisterVirtualDelegateCall(MethodDescriptor callee, VariableNode receiver,
        //                                    IList<AnalysisNode> arguments, 
        //                                    VariableNode lhs, DelegateVariableNode delegateVariableNode)
        //{
        //    Contract.Requires(receiver != null);
        //    Contract.Requires(arguments != null);
        //    Contract.Requires(lhs != null);
        //    Contract.Requires(callee != null);
        //    Contract.Requires(delegateVariableNode != null);

        //    var callExp = new DelegateCallInfo(this.Method, delegateVariableNode, receiver, arguments, lhs);
        //    this.PropagationGraph.AddEdge(receiver, delegateVariableNode);

        //    RegisterInvocation(arguments, delegateVariableNode, callExp);
        //}


		private void RegisterInvocation(IList<PropGraphNodeDescriptor> arguments, 
                                        AnalysisCallNode invocationNode, 
                                        CallInfo callExp)
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

		public void RegisterCallLHS(AnalysisCallNode callNode, VariableNode lhs)
		{
			var callExp = PropagationGraph.GetInvocationInfo(callNode);
			callExp.LHS = lhs;
		}
		#endregion

		#region Incremental analysis - Removal of types
		public void RegisterRemoveNewExpressionAssignment(PropGraphNodeDescriptor lhs)
		{
			if (lhs != null)
			{
				var types = PropagationGraph.GetTypes(lhs);

				this.RemovedTypes.UnionWith(types);

				PropagationGraph.RemoveTypes(lhs, types);
				PropagationGraph.AddToDeletionWorkList(lhs);

			}
		}
		public void RegisterRemoveType(PropGraphNodeDescriptor lhs, TypeDescriptor type)
		{
			if (lhs != null)
			{
				var types = new HashSet<TypeDescriptor>();
				types.Add(type);

				this.RemovedTypes.Add(type);

				PropagationGraph.RemoveTypes(lhs, types);
				PropagationGraph.AddToDeletionWorkList(lhs);

			}
		}
		public void RegisterRemoveTypes(PropGraphNodeDescriptor lhs, IEnumerable<TypeDescriptor> types)
		{
			if (lhs != null)
			{
				this.RemovedTypes.UnionWith(types);

				PropagationGraph.RemoveTypes(lhs, types);
				PropagationGraph.AddToDeletionWorkList(lhs);

			}
		}

		public void RegisterRemoveAssignment(PropGraphNodeDescriptor lhs, PropGraphNodeDescriptor rhs)
		{
			if (lhs != null && rhs != null)
			{
				var typesToDelete = PropagationGraph.TypesInEdge(rhs, lhs);
				PropagationGraph.RemoveTypes(lhs, typesToDelete);
				PropagationGraph.AddToDeletionWorkList(lhs);
			}
		}
		#endregion

		public void RegisterRet(PropGraphNodeDescriptor rn)
		{
			PropagationGraph.AddEdge(rn, this.ReturnVariable);
		}
	}
}