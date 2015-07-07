// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using ReachingTypeAnalysis.Communication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Collections;
using ReachingTypeAnalysis.Roslyn;
using OrleansInterfaces;
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
    internal partial class MethodEntityProcessor: EntityProcessor
    {
        internal ICodeProvider codeProvider;
        [NonSerialized]
        private IDictionary<PropGraphNodeDescriptor, ISet<MethodDescriptor>> calleesMappingCache = new Dictionary<PropGraphNodeDescriptor, ISet<MethodDescriptor>>();
        //private SyntaxTree tree;
        internal MethodEntity MethodEntity { get; private set; }
        internal MethodEntityProcessor(MethodEntity methodEntity, 
            IDispatcher dispatcher, ICodeProvider codeProvider, IEntityDescriptor entityDescriptor = null, 
            bool verbose = false) :
            base(methodEntity, entityDescriptor, dispatcher)
        {
            Contract.Assert(methodEntity != null);

            this.MethodEntity = methodEntity;
            this.EntityDescriptor = entityDescriptor==null?methodEntity.EntityDescriptor
                                                          :entityDescriptor;
			this.Verbose = true; // verbose;
            // It gets a code provider for the method. 
            if (codeProvider!=null || dispatcher is OrleansDispatcher)
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
                                        bool verbose = false) :this (methodEntity, dispatcher, null, entityDescriptor, verbose)
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
            Debug.WriteLine("ProcessCallMessage: {0}", callerMesssage);
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
                Debug.WriteLine("Reached {0} via propagation", this.MethodEntity.MethodDescriptor);
            }
            // Propagation of concrete types

            var callsAndRets = this.MethodEntity.PropGraph.Propagate(this.codeProvider);
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
                this.InvalidateCaches();
            }
            /// I made a new list to avoid a concurrent modification exception we received in some tests
            var invocationsToProcess = new List<AnalysisInvocationExpession>(callInvocationInfoForCalls);
            /// Every invocation that was "touched" by a propagation is signaled to propagate the new data
            foreach (var invocationInfo in invocationsToProcess)
            {
                ///  Add instanciated types
                invocationInfo.InstantiatedTypes = this.MethodEntity.InstantiatedTypes;
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
                    var instantiatedTypes = new HashSet<TypeDescriptor>();
                    /// We get the instantiated type that are compatible with the receiver type
                    instantiatedTypes.UnionWith(
						this.MethodEntity.InstantiatedTypes
							.Where(type => codeProvider.IsSubtype(type,callInfo.Receiver.Type)));
                        // .Where(type => type.IsSubtype(callInfo.Receiver.Type)));
					foreach (var type in instantiatedTypes)
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
                        //var realCallee = callInfo.Callee.FindMethodImplementation(receiverType);
                        var realCallee = codeProvider.FindMethodImplementation(callInfo.Callee,receiverType);
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
                CreateAndSendCallMessage(callInfo, 
					callInfo.Callee, 
					callInfo.Callee.ContainerType, propKind);
            }
        }

        private void CreateAndSendCallMessage(AnalysisInvocationExpession callInfo, MethodDescriptor realCallee, 
                                            TypeDescriptor receiverType, PropagationKind propKind)
        {
            /// Here I have all the necessary info to update the callgraph
            /// 
            var callMessage = CreateCallMessage(callInfo, realCallee, receiverType, propKind);
            var callerMessage = new CallerMessage(this.EntityDescriptor, callMessage);
            var destination = EntityFactory.Create(realCallee, this.dispatcher);

            this.SendMessage(destination, callerMessage);
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
                        var methodDescriptor = codeProvider.FindMethodImplementation(delegateInstance, t);
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
            foreach (var callee in GetDelegateCallees(delegateCallInfo.CalleeDelegate))
            {
                CreateAndSendCallMessage(delegateCallInfo, callee, callee.ContainerType, propKind);
            }
        }

        private CallMessageInfo CreateCallMessage(AnalysisInvocationExpession callInfo,
                                                            MethodDescriptor actuallCallee,
                                                            TypeDescriptor computedReceiverType,
                                                            PropagationKind propKind)
        {
            // var calleType = (TypeDescriptor)actuallCallee.ContainerType;
            var calleType = computedReceiverType;
            ISet<TypeDescriptor> potentialReceivers = new HashSet<TypeDescriptor>();

            if (callInfo.Receiver != null)
            {
                // BUG!!!! I should use simply computedReceiverType
                // Instead of copying all types with use the type we use to compute the callee

                potentialReceivers.UnionWith(
				    	GetTypes(callInfo.Receiver, propKind)
                        .Where(t => codeProvider.IsSubtype(t,calleType)));
                        //.Where(t => t.IsSubtype(calleType)));

                // potentialReceivers.Add((AnalysisType)calleType);
            }

            var argumentValues = callInfo.Arguments
				.Select(a => a != null ? 
				GetTypes(a, propKind) : 
				new HashSet<TypeDescriptor>());

            Contract.Assert(argumentValues.Count() == callInfo.Arguments.Count());

            return new CallMessageInfo(callInfo.Caller, actuallCallee, callInfo.CallNode,
                                                potentialReceivers, new List<ISet<TypeDescriptor>>(argumentValues),
                                                callInfo.LHS, callInfo.InstantiatedTypes, propKind);
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
            var lhs = context.CallLHS;
            var types = returnVariable != null ? 
				GetTypes(returnVariable, propKind) : 
				new HashSet<TypeDescriptor>();

            // Diego TO-DO, different treatment for adding and removal
            if (propKind == PropagationKind.ADD_TYPES && types.Count() == 0 && returnVariable != null)
            {
                var instTypes = new HashSet<TypeDescriptor>();
                instTypes.UnionWith(this.MethodEntity.InstantiatedTypes
                          .Where(type => codeProvider.IsSubtype(type,returnVariable.Type)));
						//.Where(type => type.IsSubtype(returnVariable.Type)));
                foreach (var type in instTypes)
                {
                    types.Add(type);
                }
            }

            // Jump to caller
            var destination = EntityFactory.Create(caller, this.dispatcher);
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
        private void EndOfPropagationEvent(PropagationKind propKind, bool retValueChange)
        {
            // Should do something more clever
            ProcessReturnNode(propKind);
        }

		private void HandleCallEvent(CallMessageInfo callMessage)
        {
            if (MethodEntity.CanBeAnalized)
            {
                Contract.Assert(callMessage.ArgumentValues.Count() == this.MethodEntity.ParameterNodes.Count());
                if (this.Verbose)
                {
                    Debug.WriteLine("Reached {0} via call", this.MethodEntity.MethodDescriptor);
                }

                // This tries to check that the invocation is repeated (didn't work: need to check)
                //if (MethodEntity.Callers.Where(cs => cs.Invocation.Equals(callMessage.CallNode)).Count()>0)
                // This check if the method is already in caller list
                if (MethodEntity.Callers.Where(cs => cs.Caller.Equals(callMessage.Caller)).Count() > 0)
                {
                    if (this.Verbose)
                    {
                        Debug.WriteLine("Recursion loop {0} ", this.MethodEntity.MethodDescriptor);
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
                    this.MethodEntity.PropGraph.DiffProp(Demarshaler.Demarshal(callMessage.Receivers), this.MethodEntity.ThisRef, callMessage.PropagationKind);
                }

                var pairEnumerable = new PairIterator<PropGraphNodeDescriptor, ISet<TypeDescriptor>>(
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
            else
            {
                EndOfPropagationEvent(callMessage.PropagationKind, false);
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

        #region Methods that compute caller callees relation
        public void InvalidateCaches()
        {
            ValidateCache(); 

            calleesMappingCache.Clear();
        }
        /// <summary>
        /// Compute all the calless of this method entities
        /// </summary>
        /// <returns></returns>
        public IEnumerable<MethodDescriptor> Callees()
        {
            var result = new HashSet<MethodDescriptor>();
            foreach (var callNode in this.MethodEntity.PropGraph.CallNodes)
            {
                result.UnionWith(Callees(callNode));
            }
            return result;
        }

        /// <summary>
        /// Computes all the potential callees for a particular method invocation
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public ISet<MethodDescriptor> Callees(PropGraphNodeDescriptor node)
        {
            ISet<MethodDescriptor> result;
            ValidateCache(); 
            if (!calleesMappingCache.TryGetValue(node, out result))
            {
                var calleesForNode = new HashSet<MethodDescriptor>();
                var invExp = this.MethodEntity.PropGraph.GetInvocationInfo((AnalysisCallNode)node);
                Contract.Assert(invExp != null);

                calleesForNode.UnionWith(this.MethodEntity.PropGraph.ComputeCalleesForNode(invExp, this.codeProvider));
                //calleesForNode.UnionWith(invExp.ComputeCalleesForNode(this.MethodEntity.PropGraph,this.codeProvider));

                calleesMappingCache[node] = calleesForNode;
                result = calleesForNode;

            }
            return result;
        }

        private void ValidateCache()
        {
            if (calleesMappingCache == null)
            {
                calleesMappingCache = new Dictionary<PropGraphNodeDescriptor, ISet<MethodDescriptor>>();
            }

        }

        /// <summary>
        /// Generates a dictionary invocationNode -> potential callees
        /// This is used for example by the demo to get the caller / callee info
        /// </summary>
        /// <returns></returns>
        public IDictionary<AnalysisCallNode, ISet<MethodDescriptor>> GetCalleesInfo()
        {
            var calleesPerEntity = new Dictionary<AnalysisCallNode, ISet<MethodDescriptor>>();
            foreach (var calleeNode in this.MethodEntity.PropGraph.CallNodes)
            {
                calleesPerEntity[calleeNode] = Callees(calleeNode);
            }
            return calleesPerEntity;
        }

        //private void GetCallesForCalleeNode(ANode calleeNode)
        //{
        //    var res = new HashSet<AMethod>();
        //    var callInfo = this.PropGraph.GetInvocationInfo(calleeNode);
        //    res.UnionWith(callInfo.ComputeCalleesForNode(this.PropGraph));
        //    calleesPerEntityCache[calleeNode] = res;
        //}

        /// <summary>
        /// This methods register that there is a new caller for this method
        /// This is used in to register who calls this method
        /// </summary>
        /// <param name="context"></param>
            #endregion

    }
}
