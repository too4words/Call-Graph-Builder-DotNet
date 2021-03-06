﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.CodeAnalysis;
using ReachingTypeAnalysis.Communication;
using ReachingTypeAnalysis.Roslyn;
using System.Threading.Tasks;

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
    [Serializable]
    internal partial class MethodEntityProcessor : EntityProcessor
    {
        internal IProjectCodeProvider codeProvider;
        [NonSerialized]
        private IDictionary<PropGraphNodeDescriptor, ISet<MethodDescriptor>> calleesMappingCache = new Dictionary<PropGraphNodeDescriptor, ISet<MethodDescriptor>>();
        //private SyntaxTree tree;
        internal MethodEntity MethodEntity { get; private set; }
        internal MethodEntityProcessor(MethodEntity methodEntity,
            IDispatcher dispatcher,
            IProjectCodeProvider codeProvider, IEntityDescriptor entityDescriptor = null,
            bool verbose = false) :
            base(methodEntity, entityDescriptor, dispatcher)
        {
            Contract.Assert(methodEntity != null);

            this.MethodEntity = methodEntity;
            this.EntityDescriptor = entityDescriptor == null ? methodEntity.EntityDescriptor
                                                          : entityDescriptor;
            this.Verbose = true; // verbose;
            // It gets a code provider for the method. 
            if (codeProvider != null)
            {
                this.codeProvider = codeProvider;
                //this.codeProvider = ProjectGrainWrapper.CreateProjectGrainWrapperAsync(methodEntity.MethodDescriptor).Result;
                //SetCodeProviderAsync(methodEntity.MethodDescriptor);
            }
            else
            {
                var pair = ProjectCodeProvider.GetProjectProviderAndSyntaxAsync(methodEntity.MethodDescriptor).Result;
                if (pair != null)
                {
                    this.codeProvider = pair.Item1;
                }
            }
            // We use the codeProvider for Propagation and HandleCall and ReturnEvents (in the method DiffProp that uses IsAssignable)
            // We can get rid of this by passing codeProvider as parameter in this 3 methods
            this.MethodEntity.PropGraph.SetCodeProvider(this.codeProvider);
        }

        internal MethodEntityProcessor(MethodEntity methodEntity,
                                        IDispatcher dispatcher, IEntityDescriptor entityDescriptor = null,
                                        bool verbose = false) : 
            this(methodEntity, dispatcher, null, entityDescriptor, verbose)
        { }


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
                else if (message is CalleeMessage)
                {
                    ProcessReturnMessage(message as CalleeMessage);
                }
            }
        }

        /// <summary>
        /// This method handles a call message. A message sent from a caller to the callee
        /// </summary>
        /// <param name="callerMesssage"></param>
        private void ProcessCallMessage(CallerMessage callerMesssage)
        {
            Logger.Instance.Log("MethodEntityProcessor", "ProcessCallMessage", callerMesssage);
            // Propagate this to the callee (RTA)
            //this.MethodEntity.InstantiatedTypes
            //    .UnionWith(
            //        Demarshaler.Demarshal(callerMesssage.CallMessageInfo.InstantiatedTypes));
            // "Event" handler: Do the propagation of caller info
            HandleCallEvent(callerMesssage.CallMessageInfo);
        }

        private void ProcessReturnMessage(CalleeMessage returnMessage)
        {
            var retMsgInfo = returnMessage.ReturnMessageInfo;
            // Propagate types from the callee (RTA)
            //this.MethodEntity.InstantiatedTypes
            //    .UnionWith(Demarshaler.Demarshal(retMsgInfo.InstatiatedTypes));
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
                Logger.Instance.Log("MethodEntityProcessor", "Propagate", "Reached {0} via propagation", this.MethodEntity.MethodDescriptor);
            }
            // Propagation of concrete types

            var callsAndRets = this.MethodEntity.PropGraph.Propagate(this.codeProvider);
            var callsThatHasChanged = callsAndRets.CalleesInfo;
            var retValueHasChanged = callsAndRets.ResultChanged;
            // This propagates the types in the affected calles
            ProcessCalleesAffectedByPropagation(callsThatHasChanged, PropagationKind.ADD_TYPES);
            // This progagate the output values of the method to the callers
            //ProcessOutputInfo(retValueHasChanged);
            ///This is a method we call to declara that we finish our propagation. 
            /// In the sync case it means everything was propagated
            /// In the sync case it can be the case that information is still flowing through other entities
            EndOfPropagationEvent(PropagationKind.ADD_TYPES, callsAndRets.ResultChanged);
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
            var callsThatHasChanged = callsAndRets.CalleesInfo;
            var retValueHasChanged = callsAndRets.ResultChanged;

            ProcessCalleesAffectedByPropagation(callsThatHasChanged, PropagationKind.REMOVE_TYPES);
            //if (retValueHasChanged)
            //{
            //    ProcessReturnNode(PropagationKind.REMOVE_TYPES);
            //}
            this.MethodEntity.PropGraph.RemoveDeletedTypes();

            EndOfPropagationEvent(PropagationKind.REMOVE_TYPES, callsAndRets.ResultChanged);
        }
        #endregion

        #region Event Triggering and Handling
        private void ProcessCalleesAffectedByPropagation(IEnumerable<CallInfo> callInvocationInfoForCalls, PropagationKind propKind)
        {
            /// This to to remove any information about calls that we cached
            /// This is for the incremental reasoning
            /// Diego: I did this for the demo because I query directly the entities instead of querying the callgraph 
            //if (callInvocationInfoForCalls.Count() > 0)
            //{
            //    this.InvalidateCaches();
            //}
            /// I made a new list to avoid a concurrent modification exception we received in some tests
            var invocationsToProcess = new List<CallInfo>(callInvocationInfoForCalls);
            /// Every invocation that was "touched" by a propagation is signaled to propagate the new data
            foreach (var invocationInfo in invocationsToProcess)
            {
                ///  Add instanciated types. 
                ///  Removed: we no longer send RTA info in the messages!
                //invocationInfo.InstantiatedTypes = this.MethodEntity.InstantiatedTypes;
                // I hate to do this. Need to Refactor!
                if (invocationInfo is MethodCallInfo)
                {
                    DispatchCallMessage(invocationInfo as MethodCallInfo, propKind);
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
        private void DispatchCallMessage(MethodCallInfo callInfo, PropagationKind propKind)
        {
			if (!callInfo.IsStatic && callInfo.Receiver != null)
			{
				// I need to computes all the calless
				// In case of a deletion we can discar the deleted callee
				//var types = GetTypes(callNode.Receiver, propKind); 
				var types = GetTypes(callInfo.Receiver);

				// If types=={} it means that some info ins missing => we should be conservative and use the instantiated types (RTA) 
				if (types.Count() == 0 && propKind != PropagationKind.REMOVE_TYPES)	//  && propKind==PropagationKind.ADD_TYPES) 
				{
					var instTypes = new HashSet<TypeDescriptor>();
					foreach (var candidateTypeDescriptor in this.MethodEntity.InstantiatedTypes)
					{
						var isSubType = codeProvider.IsSubtypeAsync(candidateTypeDescriptor, callInfo.Receiver.Type).Result;

						if (isSubType)
						{
							instTypes.Add(candidateTypeDescriptor);
						}
					}
					foreach (var t in instTypes)
					{
						types.Add(t);
					}
					// TO-DO: SHould I fix the node in the receiver to show that is not loaded. Ideally I should use the declared type. 
					// Here I will use the already instantiated types
					this.MethodEntity.PropGraph.Add(callInfo.Receiver, types);
					//var declaredType = callInfo.Receiver.AType;
					//this.PropGraph.Add(callInfo.Receiver, declaredType);
				}
				if (types.Count() > 0)
				{
					var continuations = new List<Task>();
					// Hack
					// Parallel.ForEach(types, async (receiverType) =>
					foreach (var receiverType in types)
					{
						// Given a method m and T find the most accurate implementation wrt to T
						// it can be T.m or the first super class implementing m
						//var realCallee = callInfo.Callee.FindMethodImplementation(receiverType);
						var realCallee = codeProvider.FindMethodImplementationAsync(callInfo.Method, receiverType).Result;
						CreateAndSendCallMessage(callInfo, realCallee, receiverType, propKind);
					};//);
				}
				// This is not good: One reason is that loads like b = this.f are not working
				// in a meth m a after  call r.m() because only the value of r is passed and not all its structure
				else
				{
					CreateAndSendCallMessage(callInfo, callInfo.Method, callInfo.Method.ContainerType, propKind);
				}
			}
			else
			{
				CreateAndSendCallMessage(callInfo, callInfo.Method, callInfo.Method.ContainerType, propKind);
			}
        }
        private IEnumerable<MethodDescriptor> GetDelegateCallees(DelegateVariableNode delegateVariableNode)
        {
			var callees = new HashSet<MethodDescriptor>();
			var types = GetTypes(delegateVariableNode);
			foreach (var delegateInstance in GetDelegates(delegateVariableNode))
			{
				if (types.Count() > 0)
				{
					foreach (var t in types)
					{
						//var aMethod = delegateInstance.FindMethodImplementation(t);
						// Diego: SHould I use : codeProvider.FindImplementation(delegateInstance, t);
						var methodDescriptor = codeProvider.FindMethodImplementationAsync(delegateInstance, t).Result;
						callees.Add(methodDescriptor);
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
            foreach (var callee in GetDelegateCallees(delegateCallInfo.Delegate))
            {
                CreateAndSendCallMessage(delegateCallInfo, callee, callee.ContainerType, propKind);
            }
        }

        private CallMessageInfo CreateCallMessage(CallInfo callInfo,
                                                            MethodDescriptor actuallCallee,
                                                            TypeDescriptor computedReceiverType,
                                                            PropagationKind propKind)
        {
            var calleType = computedReceiverType;
            ISet<TypeDescriptor> potentialReceivers = new HashSet<TypeDescriptor>();

            if (callInfo.Receiver != null)
            {
                // BUG!!!! I should use simply computedReceiverType
                // Instead of copying all types with use the type we use to compute the callee
                foreach (var type in GetTypes(callInfo.Receiver, propKind))
                {
                    var isSubType = codeProvider.IsSubtypeAsync(type, calleType).Result;

                    if(isSubType)
                    {
                        potentialReceivers.Add(type);
                    }
                }
            }

            var argumentValues = callInfo.Arguments
                .Select(a => a != null ?
                GetTypes(a, propKind) :
                new HashSet<TypeDescriptor>());

            Contract.Assert(argumentValues.Count() == callInfo.Arguments.Count());

            return new CallMessageInfo(callInfo.Caller, actuallCallee, potentialReceivers,
				new List<ISet<TypeDescriptor>>(argumentValues), callInfo.InstantiatedTypes,
				callInfo.CallNode, callInfo.LHS, propKind);
        }

		private void CreateAndSendCallMessage(CallInfo callInfo, 
                            MethodDescriptor realCallee, TypeDescriptor receiverType, PropagationKind propKind)
		{
			var callMessage = CreateCallMessage(callInfo, realCallee, receiverType, propKind);
			var callerMessage = new CallerMessage(this.EntityDescriptor, callMessage);
            var destination = new MethodEntityDescriptor(realCallee);

			this.SendMessage(destination, callerMessage);
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
		internal void DispachReturnMessage(CallContext context, VariableNode returnVariable, PropagationKind propKind)
		{
			var caller = context.Caller;
			var lhs = context.LHS;
			var types = returnVariable != null ?
				GetTypes(returnVariable, propKind) :
				new HashSet<TypeDescriptor>();

			// Diego TO-DO, different treatment for adding and removal
			if (propKind == PropagationKind.ADD_TYPES && types.Count() == 0 && returnVariable != null)
			{
				var instTypes = new HashSet<TypeDescriptor>();

				foreach (var iType in this.MethodEntity.InstantiatedTypes)
				{
					var isSubtype = codeProvider.IsSubtypeAsync(iType, returnVariable.Type).Result;

					if (isSubtype)
					{
						instTypes.Add(iType);
					}
				}

				foreach (var t in instTypes)
				{
					types.Add(t);
				}
			}

			// Jump to caller
			var destination = new MethodEntityDescriptor(caller);
			var retMessageInfo = new ReturnMessageInfo(
				context.Caller,
				this.MethodEntity.MethodDescriptor,
				types,
				this.MethodEntity.InstantiatedTypes,
				context.CallNode, lhs,
				propKind);
			var returnMessage = new CalleeMessage(this.MethodEntity.EntityDescriptor, retMessageInfo);
			this.SendMessage(destination, returnMessage);
		}

        /// <summary>
        /// This method is executed when a method finishes its analysis
        /// If some output info was generated it will generate return messahe
        /// </summary>
        /// <param name="propKind"></param>
        private void EndOfPropagationEvent(PropagationKind propKind, bool retValueChange)
        {
            // Should do something more clever
            ProcessReturnNode(propKind);
        }

        private void HandleCallEvent(CallMessageInfo callMessage)
        {
            if (MethodEntity.CanBeAnalized)
            {
                Contract.Assert(callMessage.ArgumentsPossibleTypes.Count() == this.MethodEntity.ParameterNodes.Count());
                if (this.Verbose)
                {
                    Logger.Instance.Log("MethodEntityProcessor", "HandleCallEvent", "Reached {0} via call", this.MethodEntity.MethodDescriptor);
                }

                // This tries to check that the invocation is repeated (didn't work: need to check)
                //if (MethodEntity.Callers.Where(cs => cs.Invocation.Equals(callMessage.CallNode)).Count()>0)
                // This check if the method is already in caller list
                if (MethodEntity.Callers.Where(cs => cs.Caller.Equals(callMessage.Caller)).Count() > 0)
                {
                    if (this.Verbose)
                    {
                        Logger.Instance.Log("MethodEntityProcessor", "HandleCallEvent", "Recursion loop {0} ", this.MethodEntity.MethodDescriptor);
                    }
                    EndOfPropagationEvent(callMessage.PropagationKind, false);
                    return;
                }

                // Save caller info
                var context = new CallContext(callMessage.Caller, callMessage.LHS, callMessage.CallNode);
                this.MethodEntity.AddToCallers(context);

                // Propagate type info in method 
                //PropGraph.Add(thisRef, receivers);
                if (this.MethodEntity.ThisRef != null)
                {
                    this.MethodEntity.PropGraph.DiffProp(Demarshaler.Demarshal(callMessage.ReceiverPossibleTypes), this.MethodEntity.ThisRef, callMessage.PropagationKind);
                }

                var pairEnumerable = new PairIterator<PropGraphNodeDescriptor, ISet<TypeDescriptor>>(
                    this.MethodEntity.ParameterNodes, callMessage.ArgumentsPossibleTypes);

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
            else
            {
                EndOfPropagationEvent(callMessage.PropagationKind, false);
            }
        }

        private void HandleReturnEvent(ReturnMessageInfo retMesssageInfo)
        {

            //PropGraph.Add(lhs, retValues);
            this.MethodEntity.PropGraph.DiffProp(
                Demarshaler.Demarshal(retMesssageInfo.ResultPossibleTypes),
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

        internal ISet<TypeDescriptor> GetTypes(PropGraphNodeDescriptor node, PropagationKind prop)
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

        internal ISet<TypeDescriptor> GetTypes(PropGraphNodeDescriptor analysisNode)
        {
            if (analysisNode != null)
            {
                return this.MethodEntity.PropGraph.GetTypes(analysisNode);
            }
            else
            {
                return new HashSet<TypeDescriptor>();
            }
        }

        internal ISet<TypeDescriptor> GetDeletedTypes(PropGraphNodeDescriptor node)
        {
            if (node != null)
            {
                return this.MethodEntity.PropGraph.GetDeletedTypes(node);
            }
            else
            {
                return new HashSet<TypeDescriptor>();
            }
        }

        /// <summary>
        /// Return the set of methods that a delegate node refers to 
        /// It should include as precondition that the node is a delegateNode but
        /// we return and empty set in other cases
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        internal ISet<MethodDescriptor> GetDelegates(DelegateVariableNode node)
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
