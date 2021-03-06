﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using ReachingTypeAnalysis.Communication;

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
	/// 
	/*
    internal partial class MethodEntityProcessor : EntityProcessor
	{
		/// <summary>
		/// Asynv version of ProcessMessage
		/// </summary>
		/// <param name="source"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public async override Task ProcessMessageAsync(IEntityDescriptor source, IMessage message)
		{
			//lock (this.Entity)
			{
				if (message is CallerMessage)
				{
					await ProcessCallMessageAsync(message as CallerMessage);
				}
				else
				if (message is CalleeMessage)
				{
					await ProcessReturnMessageAsync(message as CalleeMessage);
				}
				else
				{
					Contract.Assert(false, "Unexpected message: " + message);
				}
			}
		}

		private async Task ProcessCallMessageAsync(CallerMessage callerMesssage)
		{
			Logger.Instance.Log("MethodEntityProcessor", "ProcessCallMessageAsync", callerMesssage);
			// Propagate this to the callee (RTA)
            //this.MethodEntity.InstantiatedTypes.UnionWith(
            //    Demarshaler.Demarshal(callerMesssage.CallMessageInfo.InstantiatedTypes));
			// "Event" handler: Do the propagation of caller info
			await HandleCallEventAsync(callerMesssage.CallMessageInfo);
		}

		private async Task ProcessReturnMessageAsync(CalleeMessage returnMesssage)
		{
            Logger.Instance.Log("MethodEntityProcessor", "ProcessReturnMessageAsync", "{0} in method {1}", returnMesssage, this.MethodEntity.MethodDescriptor);

			var retMsgInfo = returnMesssage.ReturnMessageInfo;
			// Propagate types from the callee (RTA)
            //this.MethodEntity.InstantiatedTypes
            //    .UnionWith(Demarshaler.Demarshal(retMsgInfo.InstatiatedTypes));
			// "Event" handler: Do the propagation of callee info
			await HandleReturnEventAsync(retMsgInfo);
		}

		#region Propagation of Constraints
		public override Task DoAnalysisAsync()
		{
			return PropagateAsync();
		}

		private async Task ProcessOutputInfoAsync(bool retValueHasChanged)
		{
			if (retValueHasChanged)
			{
				await ProcessReturnNodeAsync(PropagationKind.ADD_TYPES);
			}
		}



		private async Task PropagateAsync()
		{
			if (this.Verbose)
			{
				Logger.Instance.Log("MethodEntityProcessor", "PropagateAsync", "Reached {0} via propagation", this.MethodEntity.MethodDescriptor);
			}
            var continuations = new List<Task>();
			var callsAndRets = await this.MethodEntity.PropGraph.PropagateAsync(this.codeProvider);
			await ProcessCalleesAffectedByPropagationAsync(callsAndRets.CalleesInfo, PropagationKind.ADD_TYPES);
			var retValueHasChanged = callsAndRets.ResultChanged;
			//ProcessOutputInfoAsync(retValueHasChanged).Start();
			await EndOfPropagationEventAsync(PropagationKind.ADD_TYPES, retValueHasChanged);
		}

		private async Task PropagateDeleteAsync()
		{
			var callsAndRets = await this.MethodEntity.PropGraph.PropagateDeletionOfNodesAsync(this.codeProvider);
			await ProcessCalleesAffectedByPropagationAsync(callsAndRets.CalleesInfo, PropagationKind.REMOVE_TYPES);
			this.MethodEntity.PropGraph.RemoveDeletedTypes();
			await EndOfPropagationEventAsync(PropagationKind.REMOVE_TYPES, callsAndRets.ResultChanged);
		}
		#endregion

		#region Event Triggering and Handling
		private async Task ProcessCalleesAffectedByPropagationAsync(IEnumerable<CallInfo> callInvocationInfoForCalls, PropagationKind propKind)
		{
			/// Diego: I did this for the demo because I query directly the entities instead of querying the callgraph 
            //if (callInvocationInfoForCalls.Count() > 0)
            //{
            //    this.InvalidateCaches();
            //}
			// I made a new list to avoid a concurrent modification exception we received in some tests
			var invocationsToProcess = new List<CallInfo>(callInvocationInfoForCalls);
			var continuations = new List<Task>();

			foreach (var invocationInfo in invocationsToProcess)
			{
				//  Add instanciated types! 
				/// Diego: Ben. This may not work well in parallel... 
				/// We need a different way to update this info
				invocationInfo.InstantiatedTypes = this.MethodEntity.InstantiatedTypes;
				// I hate to do this. Need to Refactor!
				if (invocationInfo is MethodCallInfo)
				{
					var t = DispatchCallMessageAsync(invocationInfo as MethodCallInfo, propKind);
					//await t;
					continuations.Add(t);
				}
				if (invocationInfo is DelegateCallInfo)
				{
					var t = DispatchCallMessageForDelegateAsync(invocationInfo as DelegateCallInfo, propKind);
					//await t;
					continuations.Add(t);
				}
			}
			Contract.Assert(invocationsToProcess.Count == invocationsToProcess.Count);
			await Task.WhenAll(continuations);
		}

		/// <summary>
		/// Async veprsion
		/// </summary>
		/// <param name="callInfo"></param>
		/// <param name="propKind"></param>
		/// <returns></returns>
		private async Task DispatchCallMessageAsync(MethodCallInfo callInfo, PropagationKind propKind)
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
                        var isSubType = await codeProvider.IsSubtypeAsync(candidateTypeDescriptor, callInfo.Receiver.Type);

                        if (isSubType)
                        {
                            instTypes.Add(candidateTypeDescriptor);
                        }
                    }
                    //instTypes.UnionWith(this.MethodEntity.InstantiatedTypes
                    //    .Where(candidateTypeDescriptor => codeProvider.IsSubtype(candidateTypeDescriptor,callInfo.Receiver.Type)));
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
					foreach(var receiverType in types)
					{
						// Given a method m and T find the most accurate implementation wrt to T
						// it can be T.m or the first super class implementing m
						//var realCallee = callInfo.Callee.FindMethodImplementation(receiverType);
						var realCallee = await codeProvider.FindMethodImplementationAsync(callInfo.Method, receiverType);
						var task = CreateAndSendCallMessageAsync(callInfo, realCallee, receiverType, propKind);
                        await task;
						//continuations.Add(task);
						//CreateAndSendCallMessage(callInfo, realCallee, receiverType, propKind);
					};//);
					await Task.WhenAll(continuations);
				}
				// This is not good: One reason is that loads like b = this.f are not working
				// in a meth m a after  call r.m() because only the value of r is passed and not all its structure
				else
				{
					await CreateAndSendCallMessageAsync(callInfo, 
						callInfo.Method, callInfo.Method.ContainerType, propKind);
				}
			}
			else
			{
				await CreateAndSendCallMessageAsync(callInfo, callInfo.Method, callInfo.Method.ContainerType, propKind);
			}
		}

        private async Task<CallMessageInfo> CreateCallMessageAsync(CallInfo callInfo,
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
                foreach (var type in GetTypes(callInfo.Receiver, propKind))
                {
                    var isSubType = await codeProvider.IsSubtypeAsync(type, calleType);

                    if(isSubType)
                    {
                        potentialReceivers.Add(type);
                    }
                }
                //potentialReceivers.UnionWith(
                //        GetTypes(callInfo.Receiver, propKind)
                //        .Where(t => codeProvider.IsSubtype(t, calleType)));
                //.Where(t => t.IsSubtype(calleType)));

                // potentialReceivers.Add((AnalysisType)calleType);
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

		private async Task CreateAndSendCallMessageAsync(CallInfo callInfo, 
                            MethodDescriptor realCallee, TypeDescriptor receiverType, PropagationKind propKind)
		{
			var callMessage = await CreateCallMessageAsync(callInfo, realCallee, receiverType, propKind);
			var callerMessage = new CallerMessage(this.EntityDescriptor, callMessage);
            var destination = new MethodEntityDescriptor(realCallee);

			await this.SendMessageAsync(destination, callerMessage);
		}

		private async Task DispatchCallMessageForDelegateAsync(DelegateCallInfo delegateCallInfo, PropagationKind propKind)
		{
			var continuations = new List<Task>();
			foreach (var callee in await GetDelegateCalleesAsync(delegateCallInfo.Delegate))
			{
				var continuation = CreateAndSendCallMessageAsync(delegateCallInfo, 
					callee, callee.ContainerType, propKind);
                await continuation;
                //continuations.Add(continuation);
			}
			await Task.WhenAll(continuations);
		}

		/// <summary>
		/// When the method returns I should inform the computed return value to all its caller
		/// It may also propagate other info from out parameters and escaping objects (not implemented)
		/// </summary>
		private async Task ProcessReturnNodeAsync(PropagationKind propKind)
		{
            if(this.Verbose)
            {
                Logger.Instance.Log("MethodEntityProcessor", "ProcessReturnNodeAsync", this.MethodEntity.MethodDescriptor);
            }
			if (this.MethodEntity.ReturnVariable != null)
			{
				// A test to try to force only one simultaneous entity 
				// We should use a Queue instead
				//while (this.MethodEntity.CurrentContext == null)
				//{
				//    Logger.Instance.Log(string.Format("Waiting: {0} to start before return...", this.Method));
				//    Thread.Sleep(10);
				//}
				//CallConext<M, E> callContext=null;
				//lock(this.MethodEntity)
				//{
				//    callContext = this.MethodEntity.CurrentContext;
				//    this.MethodEntity.CurrentContext = null;
				//}
				//var ret = this.MethodEntity.ReturnVariable;
				//if (callContext.Invocation != null)
				//{
				//    var task = DispachReturnMessageAsync(callContext, ret, propKind);
				//    if (task != null) task.RunSynchronously();
				//}

				var continuations = new List<Task>();
				foreach (var callerContex in this.MethodEntity.Callers)
				{
					var ret = this.MethodEntity.ReturnVariable;
					var t = DispachReturnMessageAsync(callerContex, ret, propKind);
                    await t;
                    //continuations.Add(t);
				}

				await Task.WhenAll(continuations);
			}
		}

        /// <summary>
        /// Async versions of DispachReturnMessage
        /// </summary>
        /// <param name="context"></param>
        /// <param name="returnVariable"></param>
        /// <param name="propKind"></param>
        /// <returns></returns>
        internal async Task DispachReturnMessageAsync(CallContext context, VariableNode returnVariable, PropagationKind propKind)
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
					var isSubtype = await codeProvider.IsSubtypeAsync(iType,returnVariable.Type);
					
					if (isSubtype)
					{
						instTypes.Add(iType);
					}
				}

				//instTypes.UnionWith(
				//	this.MethodEntity.InstantiatedTypes
				//		.Where(iType => codeProvider.IsSubtypeAsync(iType,returnVariable.Type).Result));
                ////    .Where(iType => iType.IsSubtype(returnVariable.Type)));
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
			//if (lhs != null)
			{
				await this.SendMessageAsync(destination, returnMessage);
			}
			//else
			//{
			//    return new Task(() => { });
			//}
		}

		public async Task EndOfPropagationEventAsync(PropagationKind propKind, bool retValueChange)
		{
			if (this.Verbose)
			{
				Logger.Instance.Log("MethodEntityProcessor", "EndOfPropagationEventAsync", this.MethodEntity.MethodDescriptor);
			}

			// Should do something more clever
			if (retValueChange)
			{
				await ProcessReturnNodeAsync(propKind);
			}
		}

		private async Task HandleCallEventAsync(CallMessageInfo callMessage)
		{
            if (MethodEntity.CanBeAnalized)
            {
                Contract.Assert(callMessage.ArgumentsPossibleTypes.Count() == this.MethodEntity.ParameterNodes.Count());

                if (this.Verbose)
                {
                    Logger.Instance.Log("MethodEntityProcessor", "HandleCallEventAsync", "Reached {0} via call {1}", this.MethodEntity.MethodDescriptor, callMessage);
                }
                // This is the node in the caller where info of ret-value should go
                var lhs = callMessage.LHS;
                // Save caller info
                var callContext = new CallContext(callMessage.Caller, callMessage.LHS, callMessage.CallNode);
                /// Loop detected due to recursion
                //if (MethodEntity.NodesProcessing.Contains(callMessage.CallNode))
                //if (MethodEntity.NodesProcessing.Contains(callContext))
                //{
                //    if (this.Verbose)
                //    {
                //        Logger.Instance.Log(string.Format("Recursion loop {0} ", this.Method.ToString()));
                //    }
                //    //lock (this.MethodEntity)
                //    //{
                //    //    MethodEntity.NodesProcessing.Remove(callContext);
                //    //    //MethodEntity.NodesProcessing.Remove(callMessage.CallNode);
                //    //}
                //    //EndOfPropagationEventAsync(callMessage.PropagationKind);
                //    return new Task(() => { });
                //}

                // Just a test to try to block the Entity to a single simultaneous caller 
                //while (this.MethodEntity.CurrentContext != null)
                //{
                //    Logger.Instance.Log(string.Format("Waiting: {0} to finish", this.Method));
                //    Thread.Sleep(10);
                //}

                //lock (this.MethodEntity)
                {
                    //this.MethodEntity.CurrentContext = callContext;

                    // Here is when I register the caller
                    this.MethodEntity.AddToCallers(callContext);

                    if (this.MethodEntity.ThisRef != null)
                    {
                        await this.MethodEntity.PropGraph.DiffPropAsync(Demarshaler.Demarshal(callMessage.ReceiverPossibleTypes), this.MethodEntity.ThisRef, callMessage.PropagationKind);
                    }
                    var pairIterator = new PairIterator<PropGraphNodeDescriptor, ISet<TypeDescriptor>>
                        (this.MethodEntity.ParameterNodes, callMessage.ArgumentsPossibleTypes);
                    foreach (var pair in pairIterator)
                    {
                        var parameterNode = pair.Item1;
                        //PropGraph.Add(pn, argumentValues[i]);
                        if (parameterNode != null)
                        {
                            await this.MethodEntity.PropGraph.DiffPropAsync(
                                Demarshaler.Demarshal(pair.Item2),
                                parameterNode, callMessage.PropagationKind);
                        }
                    }
                }

                switch (callMessage.PropagationKind)
                {
                    case PropagationKind.ADD_TYPES:
                        await PropagateAsync();
                        break;
                    case PropagationKind.REMOVE_TYPES:
                        await PropagateDeleteAsync();
                        break;
                }
            }
            else 
            {
                await EndOfPropagationEventAsync(callMessage.PropagationKind,false);
            }
		}


		private async Task HandleReturnEventAsync(ReturnMessageInfo retMessageInfo)
		{
			if (retMessageInfo.LHS != null)
			{
				//lock (this.MethodEntity)
				{
					await this.MethodEntity.PropGraph.DiffPropAsync(
						Demarshaler.Demarshal(retMessageInfo.ResultPossibleTypes),
						Demarshaler.Demarshal(retMessageInfo.LHS),
						retMessageInfo.PropagationKind);
					// This should be Async
				}

				switch (retMessageInfo.PropagationKind)
				{
					case PropagationKind.ADD_TYPES:
						await PropagateAsync();
						break;
					case PropagationKind.REMOVE_TYPES:
						await PropagateDeleteAsync();
						break;
				}
			}
		}

        private async Task<IEnumerable<MethodDescriptor>> GetDelegateCalleesAsync(DelegateVariableNode delegateVariableNode)
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
                        var methodDescriptor = await codeProvider.FindMethodImplementationAsync(delegateInstance, t);
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

		#endregion
        public async Task<IEnumerable<MethodDescriptor>> CalleesAsync()
        {
            var result = new HashSet<MethodDescriptor>();
            foreach (var callNode in this.MethodEntity.PropGraph.CallNodes)
            {
                result.UnionWith(await CalleesAsync(callNode));
            }
            return result;
        }

        public async Task<ISet<MethodDescriptor>> CalleesAsync(PropGraphNodeDescriptor node)
        {
            ISet<MethodDescriptor> result;
            //ValidateCache();
            if (!calleesMappingCache.TryGetValue(node, out result))
            {
                var calleesForNode = new HashSet<MethodDescriptor>();
                var invExp = this.MethodEntity.PropGraph.GetInvocationInfo((AnalysisCallNode)node);
                Contract.Assert(invExp != null);

                //calleesForNode.UnionWith(await invExp.ComputeCalleesForNodeAsync(this.MethodEntity.PropGraph, this.codeProvider));
                calleesForNode.UnionWith(await this.MethodEntity.PropGraph.ComputeCalleesForNodeAsync(invExp,this.codeProvider));
                calleesMappingCache[node] = calleesForNode;
                result = calleesForNode;

            }
            return result;
        }
	}
	 */ 
}
