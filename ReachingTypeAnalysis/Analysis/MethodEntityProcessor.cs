// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using ReachingTypeAnalysis.Communication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Collections;

namespace ReachingTypeAnalysis.Analysis
{
    /// <summary>
    /// This class is in charge of processing a method entity
    /// In particular, in performing the propagation of types 
    /// and communnicating with the callees and caller entities when the propagation 
    /// reaches the boundary of the method
    /// 
    /// THIS CLASS COULD BE MADE STATIC. 
    /// It just hold a reference to the method enitty being processed and modified 
    /// It can be passed as a parameter. T
    /// </summary>
    /// <typeparam name="E"></typeparam>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="M"></typeparam>
    internal partial class MethodEntityProcessor: EntityProcessor
    {
        public MethodEntity MethodEntity { get; private set; }
        public MethodEntityProcessor(MethodEntity methodEntity, IDispatcher dispatcher, bool verbose = false) :
            base(methodEntity, dispatcher)
        {
            Contract.Assert(methodEntity != null);

            this.MethodEntity = methodEntity;
            this.Verbose = verbose;
        }

        public bool Verbose { get; private set; }

        /// <summary>
        /// This is the main responsibility of the class. Essentially to anwser messagages sent to the entity
        /// </summary>
        /// <param name="source"></param>
        /// <param name="message"></param>
	    public override void ProcessMessage(IEntityDescriptor source, IMessage message)
        {
            lock (this.Entity)
            {
                if (message is CallerMessage)
                {
                    ProcessCallMessage(message as CallerMessage);
                }
                else if (message is ReturnMessage)
                {
                    ProcessReturnMessage(message as ReturnMessage);
                }
            }
        }

        /// <summary>
        /// This method handles a call message. A message sent from a caller to the callee
        /// </summary>
        /// <param name="callerMesssage"></param>
        private void ProcessCallMessage(CallerMessage callerMesssage)
        {
            Debug.WriteLine(string.Format("ProcessCallMessage: {0}", callerMesssage));
            // Propagate this to the callee (RTA)
            this.MethodEntity.InstantiatedTypes
				.UnionWith(
					Demarshaler.Demarshal(callerMesssage.CallMessageInfo.InstantiatedTypes));
            // "Event" handler: Do the propagation of caller info
            HandleCallEvent(callerMesssage.CallMessageInfo);
        }

        private void ProcessReturnMessage(ReturnMessage returnMessage)
        {
            var retMsgInfo = returnMessage.ReturnMessageInfo;
            // Propagate types from the callee (RTA)
            this.MethodEntity.InstantiatedTypes
				.UnionWith(Demarshaler.Demarshal(retMsgInfo.InstatiatedTypes));
            // "Event" handler: Do the propagation of callee info
            HandleReturnEvent(retMsgInfo);
        }

        #region Propagation of Constraints
        public override void DoAnalysis()
        {
            Propagate();
        }

        /// <summary>
        /// This method is invoked to propagate the concrete type info in the internal method graph
        /// As a result of the propagation several methods (calless, caller) may have to be updated to propagate the new information
        /// </summary>
		private void Propagate()
        {
            if (this.Verbose)
            {
                Debug.WriteLine(string.Format("Reached {0} via propagation", this.MethodEntity.Method.ToString()));
            }
            // Propagation of concrete types
            var callsAndRets = this.MethodEntity.PropGraph.Propagate();
            var callsThatHasChanged = callsAndRets.Calls;
            var retValueHasChanged = callsAndRets.RetValueChange;
            // This propagates the types in the affected calles
            ProcessCalleesAffectedByPropagation(callsThatHasChanged, PropagationKind.ADD_TYPES);
            // This progagate the output values of the method to the callers
            //ProcessOutputInfo(retValueHasChanged);
            ///This is a method we call to declara that we finish our propagation. 
            /// In the sync case it means everything was propagated
            /// In the sync case it can be the case that information is still flowing through other entities
            EndOfPropagationEvent(PropagationKind.ADD_TYPES, callsAndRets.RetValueChange);
        }

        private void ProcessOutputInfo(bool retValueHasChanged)
        {
            if (retValueHasChanged)
            {
                ProcessReturnNode(PropagationKind.ADD_TYPES);
            }
        }

        /// <summary>
        /// This is used to progapage a removal of a concrete type
        /// </summary>
        public void DoDelete()
        {
            PropagateDelete();
        }

        private void PropagateDelete()
        {
            var callsAndRets = this.MethodEntity.PropGraph.PropagateDeletionOfNodes();
            var callsThatHasChanged = callsAndRets.Calls;
            var retValueHasChanged = callsAndRets.RetValueChange;

            ProcessCalleesAffectedByPropagation(callsThatHasChanged, PropagationKind.REMOVE_TYPES);
            //if (retValueHasChanged)
            //{
            //    ProcessReturnNode(PropagationKind.REMOVE_TYPES);
            //}
            this.MethodEntity.PropGraph.RemoveDeletedTypes();

            EndOfPropagationEvent(PropagationKind.REMOVE_TYPES, callsAndRets.RetValueChange);
        }
        #endregion

        #region Event Triggering and Handling
        private void ProcessCalleesAffectedByPropagation(IEnumerable<AnalysisInvocationExpession> callInvocationInfoForCalls, PropagationKind propKind)
        {
            /// This to to remove any information about calls that we cached
            /// This is for the incremental reasoning
            /// Diego: I did this for the demo because I query directly the entities instead of querying the callgraph 
            if (callInvocationInfoForCalls.Count() > 0)
            {
                this.MethodEntity.InvalidateCaches();
            }
            /// I made a new list to avoid a concurrent modification exception we received in some tests
            var invocationsToProcess = new List<AnalysisInvocationExpession>(callInvocationInfoForCalls);
            /// Every invocation that was "touched" by a propagation is signaled to propagate the new data
            foreach (var invocationInfo in invocationsToProcess)
            {
                ///  Add instanciated types
                invocationInfo.InstatiatedTypes = this.MethodEntity.InstantiatedTypes;
                // I hate to do this. Need to Refactor!
                if (invocationInfo is CallInfo)
                {
                    DispatchCallMessage(invocationInfo as CallInfo, propKind);
                }
                if (invocationInfo is DelegateCallInfo)
                {
                    DispatchCallMessageForDelegate(invocationInfo as DelegateCallInfo, propKind);
                }
            }
        }

        /// <summary>
        /// Given an invocations this method dispatch a message to every potential callee to propagate the info in the arguments
        /// </summary>
        /// <param name="callInfo"></param>
        /// <param name="propKind"></param>
        private void DispatchCallMessage(CallInfo callInfo, PropagationKind propKind)
        {
            if (!callInfo.IsStatic && callInfo.Receiver != null)
            {
                // I need to computes all the calless
                // In case of a deletion we can discar the deleted callee
                //var types = GetTypes(callNode.Receiver, propKind); 
                var types = GetTypes(callInfo.Receiver);

                /// If types=={} it means that some info ins missing => we should be conservative and use the instantiated types (RTA) 
                if (types.Count() == 0 && propKind != PropagationKind.REMOVE_TYPES)
                {
                    var instTypes = new HashSet<AnalysisType>();
                    /// We get the instantiated type that are compatible with the receiver type
                    instTypes.UnionWith(
						this.MethodEntity.InstantiatedTypes
							.Where(type => type.IsSubtype(callInfo.Receiver.AnalysisType)));
					foreach (var type in instTypes)
					{
						types.Add(type);
					}
                    // TO-DO: SHould I fix the node in the receiver to show that is not loaded? Ideally I should use the declared type. 
                    // Here I will use the already instantiated types
                    this.MethodEntity.PropGraph.Add(callInfo.Receiver, types);
                    //var declaredType = callInfo.Receiver.AType;
                    //this.PropGraph.Add(callInfo.Receiver, declaredType);
                }
                // Now we compute the potential callees we are going to send the messsages
                // I need to propagate the change to all potential callees, in particular the change in arguments
                if (types.Count() > 0)
                {
                    foreach (var receiverType in types)
                    {
                        // Given a method m and T find the most accurate implementation wrt to T
                        // it can be T.m or the first super class implementing m
                        var realCallee = callInfo.Callee.FindMethodImplementation(receiverType);
                        CreateAndSendCallMessage(
							callInfo, realCallee, receiverType, propKind);
                    }
                }
                // This is not good: One reason is that loads like b = this.f are not working
                // in a meth m a after  call r.m() because only the value of r is passed and not all its structure
                else
                {
                    CreateAndSendCallMessage(callInfo, callInfo.Callee, callInfo.Callee.ContainerType, propKind);
                }
            }
            else
            {
                CreateAndSendCallMessage(callInfo, callInfo.Callee, callInfo.Callee.ContainerType, propKind);
            }
        }

        private void CreateAndSendCallMessage(AnalysisInvocationExpession callInfo, AnalysisMethod realCallee, AnalysisType receiverType, PropagationKind propKind)
        {
            /// Here I have all the necessary info to update the callgraph
            /// 
            var callMessage = CreateCallMessage(callInfo, realCallee, receiverType, propKind);
            var callerMessage = new CallerMessage(this.MethodEntity.EntityDescriptor, callMessage);
            var destination = EntityFactory.Create(realCallee);

            this.SendMessage(destination, callerMessage);
        }

        private IEnumerable<AnalysisMethod> GetDelegateCallees(AnalysisNode delegateNode)
        {
            var callees = new HashSet<AnalysisMethod>();
            var types = GetTypes(delegateNode);
            foreach (var delegateInstance in GetDelegates(delegateNode))
            {
                if (types.Count() > 0)
                {
                    foreach (var t in types)
                    {
                        var aMethod = delegateInstance.FindMethodImplementation(t);
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

        private void DispatchCallMessageForDelegate(DelegateCallInfo delegateCallInfo, PropagationKind propKind)
        {
            foreach (var callee in GetDelegateCallees(delegateCallInfo.CalleeDelegate))
            {
                CreateAndSendCallMessage(delegateCallInfo, (AnalysisMethod)callee, callee.ContainerType, propKind);
            }
        }

        private CallMessageInfo CreateCallMessage(AnalysisInvocationExpession callInfo,
                                                            AnalysisMethod actuallCallee,
                                                            AnalysisType computedReceiverType,
                                                            PropagationKind propKind)
        {
            var calleType = (AnalysisType)actuallCallee.ContainerType;
            ISet<AnalysisType> potentialReceivers = new HashSet<AnalysisType>();

            if (callInfo.Receiver != null)
            {
                // Instead of copying all types with use the type we use to compute the callee
                //potentialReceivers.UnionWith(GetTypes(callInfo.Receiver, propKind).Where(t => t.Equals(computedReceiverType)));
                potentialReceivers.UnionWith(
					GetTypes(callInfo.Receiver, propKind).Where(t => t.IsSubtype(calleType)));
                // potentialReceivers.Add((AnalysisType)calleType);
            }

            var argumentValues = callInfo.Arguments
				.Select(a => a != null ? 
				GetTypes(a, propKind) : new HashSet<AnalysisType>());
            Contract.Assert(argumentValues.Count() == callInfo.Arguments.Count());
            AnalysisMethod am = (AnalysisMethod)actuallCallee;
            //Contract.Assert(argumentValues.Count() == ((AMethod)am).RoslynMethod.Parameters.Count() );

            return new CallMessageInfo(callInfo.Caller, actuallCallee, callInfo.CallNode,
                                                potentialReceivers, new List<ISet<AnalysisType>>(argumentValues),
                                                callInfo.LHS, callInfo.InstatiatedTypes, propKind);
        }

        /// <summary>
        /// When the method returns I should inform the computed return value to all its caller
        /// It may also propagate other info from out parameters and escaping objects (not implemented)
        /// </summary>
        private void ProcessReturnNode(PropagationKind propKind)
        {
            if (this.MethodEntity.ReturnVariable != null)
            {
                // Should do the same with out and escaping info
                foreach (var callerContex in this.MethodEntity.Callers)
                {
                    DispachReturnMessage(callerContex, this.MethodEntity.ReturnVariable, propKind);
                }
            }
        }

        /// <summary>
        /// This is use to send a message to the caller with the output information generated by the callee
        /// </summary>
        /// <param name="context"></param>
        /// <param name="returnVariable"></param>
        /// <param name="propKind"></param>
        public void DispachReturnMessage(CallContext context, AnalysisNode returnVariable, PropagationKind propKind)
        {
            var caller = context.Caller;
            var lhs = context.CallLHS;
            var types = returnVariable != null ? 
				GetTypes(returnVariable, propKind) : new HashSet<AnalysisType>();
            // Diego TO-DO, different treatment for adding and removal
            if (propKind == PropagationKind.ADD_TYPES && types.Count() == 0 && returnVariable != null)
            {
                var instTypes = new HashSet<AnalysisType>();
                instTypes.UnionWith(this.MethodEntity.InstantiatedTypes
						.Where(type => type.IsSubtype(returnVariable.AnalysisType)));
                foreach (var type in instTypes)
                {
                    types.Add(type);
                }
            }

            // Jump to caller
            var destination = EntityFactory.Create(caller);
            var retMessageInfo = new ReturnMessageInfo(
				lhs, 
				types, 
				propKind, 
				this.MethodEntity.InstantiatedTypes, 
				context.Invocation); 
            var returnMessage = new ReturnMessage(
				this.MethodEntity.EntityDescriptor, retMessageInfo);
            if (lhs != null)
            {
                this.SendMessage(destination, returnMessage);
            }
        }

        /// <summary>
        /// This method is executed when a method finishes its analysis
        /// If some output info was generated it will generate return messahe
        /// </summary>
        /// <param name="propKind"></param>
        public void EndOfPropagationEvent(PropagationKind propKind, bool retValueChange)
        {
            // Should do something more clever
            ProcessReturnNode(propKind);
        }

		private void HandleCallEvent(CallMessageInfo callMessage)
        {
            Contract.Assert(callMessage.ArgumentValues.Count() == this.MethodEntity.ParameterNodes.Count());
            if (this.Verbose)
            {
                Debug.WriteLine(string.Format("Reached {0} via call", this.MethodEntity.Method.ToString()));
            }
			var caller = Demarshaler.Demarshal(callMessage.Caller);
            // This is the node in the caller where info of ret-value should go
            var lhs = Demarshaler.Demarshal(callMessage.LHS);

			// This tries to check that the invocation is repeated (didn't work: need to check)
			//if (MethodEntity.Callers.Where(cs => cs.Invocation.Equals(callMessage.CallNode)).Count()>0)
			// This check if the method is already in caller list
			if (MethodEntity.Callers.Where(cs => cs.Caller.Equals(callMessage.Caller)).Count() > 0)
			{
				if (this.Verbose)
				{
					Debug.WriteLine(string.Format("Recursion loop {0} ", this.MethodEntity.Method.ToString()));
				}
				EndOfPropagationEvent(callMessage.PropagationKind, false);

				return;
			}

            // Save caller info
            var context = new CallContext(caller, lhs, Demarshaler.Demarshal(callMessage.CallNode));
            this.MethodEntity.AddToCallers(context);

            // Propagate type info in method 
            //PropGraph.Add(thisRef, receivers);
            if (this.MethodEntity.ThisRef != null)
            {
                this.MethodEntity.PropGraph.DiffProp(Demarshaler.Demarshal(callMessage.Receivers), this.MethodEntity.ThisRef, callMessage.PropagationKind);
            }

			var pairEnumerable = new PairIterator<AnalysisNode, ISet<TypeDescriptor>>(
				this.MethodEntity.ParameterNodes, callMessage.ArgumentValues);
            foreach (var pair in pairEnumerable)
            {
                var parameterNode = pair.Item1;
                //PropGraph.Add(pn, argumentValues[i]);
                if (parameterNode != null)
                {
                    this.MethodEntity.PropGraph.DiffProp(
						Demarshaler.Demarshal(pair.Item2), 
						parameterNode, callMessage.PropagationKind);
                }
            }

            if (callMessage.PropagationKind == PropagationKind.ADD_TYPES)
            {
                Propagate();
            }
            if (callMessage.PropagationKind == PropagationKind.REMOVE_TYPES)
            {
                PropagateDelete();
            }
        }

        private void HandleReturnEvent(ReturnMessageInfo retMesssageInfo)
        {

            //PropGraph.Add(lhs, retValues);
            this.MethodEntity.PropGraph.DiffProp(
				Demarshaler.Demarshal(retMesssageInfo.RVs),
				Demarshaler.Demarshal(retMesssageInfo.LHS), 
				retMesssageInfo.PropagationKind);
            // This should be Async
            switch (retMesssageInfo.PropagationKind)
            {
                case PropagationKind.ADD_TYPES:
                    Propagate();
                    break;
                case PropagationKind.REMOVE_TYPES:
                    PropagateDelete();
                    break;
                default:
                    throw new ArgumentException();
            }
        }

		internal ISet<AnalysisType> GetTypes(AnalysisNode node, PropagationKind prop)
        {
            switch (prop)
            {
                case PropagationKind.ADD_TYPES:
                    return GetTypes(node);
                case PropagationKind.REMOVE_TYPES:
                    return GetDeletedTypes(node);
                default:
                    return GetTypes(node);
            }
        }

        internal ISet<AnalysisType> GetTypes(AnalysisNode node)
        {
            if (node != null)
            {
                return this.MethodEntity.PropGraph.GetTypes(node);
            }
            else
            {
                return new HashSet<AnalysisType>();
            }
        }

        internal ISet<AnalysisType> GetDeletedTypes(AnalysisNode node)
        {
            if (node != null)
            {
                return this.MethodEntity.PropGraph.GetDeletedTypes(node);
            }
            else
            {
                return new HashSet<AnalysisType>();
            }
        }

        /// <summary>
        /// Return the set of methods that a delegate node refers to 
        /// It should include as precondition that the node is a delegateNode but
        /// we return and empty set in other cases
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        internal ISet<AnalysisMethod> GetDelegates(AnalysisNode node)
        {
            return this.MethodEntity.PropGraph.GetDelegates(node);
        }

      //  internal ISet<T> GetPotentialTypes(E node)
      //  {
      //      var res = new HashSet<T>();
      //      foreach (var type in this.MethodEntity.PropGraph.GetTypes(node))
      //      {
      //          if (type.IsConcreteType)
      //          {
      //              res.Add(type);
      //          }
      //          else
      //          {
      //              res.UnionWith(
						//InstantiatedTypes
						//	.Where(type => type.IsSubtype(type)));
      //          }
      //      }
      //      return res;
      //  }
        #endregion
    }
}
