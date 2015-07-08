﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using ReachingTypeAnalysis.Communication;
using System.Diagnostics.Contracts;


namespace ReachingTypeAnalysis.Analysis
{
    class AnalysisOrchestator
    {

        public static async Task AnalyzeAsync(MethodDescriptor methodDescriptor)
        {
            var codeProvider = await ProjectGrainWrapper.CreateProjectGrainWrapperAsync(methodDescriptor);

            Debug.WriteLine("Analyzing {0} ", methodDescriptor);
            var orleansEnityDesc = new OrleansEntityDescriptor(methodDescriptor);
            var methodEntityGrain = await OrleansDispatcher.CreateMethodEntityGrain(orleansEnityDesc);  
            var callsAndRets = await methodEntityGrain.PropagateAsync();
            await ProcessCalleesAffectedByPropagationAsync(callsAndRets.Calls, PropagationKind.ADD_TYPES, codeProvider);
            //var retValueHasChanged = callsAndRets.RetValueChange;
           // await EndOfPropagationEventAsync(PropagationKind.ADD_TYPES, retValueHasChanged);
        }
        private static async Task ProcessCalleesAffectedByPropagationAsync(IEnumerable<AnalysisInvocationExpession> callInvocationInfoForCalls,
                                                                            PropagationKind propKind, ICodeProvider codeProvider)
        {

            // I made a new list to avoid a concurrent modification exception we received in some tests
            var invocationsToProcess = new List<AnalysisInvocationExpession>(callInvocationInfoForCalls);
            var continuations = new List<Task>();

            foreach (var invocationInfo in invocationsToProcess)
            {
                //TODO: I hate to do this. Need to Refactor!
                if (invocationInfo is CallInfo)
                {
                    var t = DispatchCallMessageAsync(invocationInfo as CallInfo, propKind, codeProvider);
                    await t;
                    //continuations.Add(t);
                }
                if (invocationInfo is DelegateCallInfo)
                {
                    var t = DispatchCallMessageForDelegateAsync(invocationInfo as DelegateCallInfo, propKind);
                    await t;
                    //continuations.Add(t);
                }
            }
            Contract.Assert(invocationsToProcess.Count == invocationsToProcess.Count);
            await Task.WhenAll(continuations);
        }
        private static async Task DispatchCallMessageAsync(CallInfo callInfo, PropagationKind propKind, ICodeProvider codeProvider)
        {
            if (!callInfo.IsStatic && callInfo.Receiver != null)
            {
                // I need to computes all the calless
                // In case of a deletion we can discar the deleted callee
                //var types = GetTypes(callNode.Receiver, propKind); 
                var types = callInfo.ReceiverPotentialTypes;

                // If types=={} it means that some info ins missing => we should be conservative and use the instantiated types (RTA) 
                if (types.Count() == 0 && propKind != PropagationKind.REMOVE_TYPES)	//  && propKind==PropagationKind.ADD_TYPES) 
                {
                    var instTypes = new HashSet<TypeDescriptor>();
                    foreach (var candidateTypeDescriptor in callInfo.InstantiatedTypes)
                    {
                        var isSubType = await codeProvider.IsSubtypeAsync(candidateTypeDescriptor, callInfo.Receiver.Type);

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
                    //this.MethodEntity.PropGraph.Add(callInfo.Receiver, types);
                    callInfo.ReceiverPotentialTypes = types;
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
                        var realCallee = await codeProvider.FindMethodImplementationAsync(callInfo.Callee, receiverType);
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
                        callInfo.Callee, callInfo.Callee.ContainerType, propKind);
                }
            }
            else
            {
                await CreateAndSendCallMessageAsync(callInfo, callInfo.Callee, callInfo.Callee.ContainerType, propKind);
            }
        }

        private static async Task DispatchCallMessageForDelegateAsync(DelegateCallInfo delegateCallInfo, PropagationKind propKind)
        {
            var continuations = new List<Task>();
            foreach (var callee in delegateCallInfo.ResolvedCallees)
            {
                var continuation = CreateAndSendCallMessageAsync(delegateCallInfo,
                    callee, callee.ContainerType, propKind);
                await continuation;
                //continuations.Add(continuation);
            }
            await Task.WhenAll(continuations);
        }

        //private static async Task<IEnumerable<MethodDescriptor>> GetDelegateCalleesAsync(DelegateCallInfo delegateCallInfo)
        //{
        //    var callees = new HashSet<MethodDescriptor>();
        //    var types = GetTypes( delegateVariableNode);
        //    delegateCallInfo.ReceiverPotentialTypes;
        //    foreach (var delegateInstance in GetDelegates(delegateVariableNode))
        //    {
        //        if (types.Count() > 0)
        //        {
        //            foreach (var t in types)
        //            {
        //                //var aMethod = delegateInstance.FindMethodImplementation(t);
        //                // Diego: SHould I use : codeProvider.FindImplementation(delegateInstance, t);
        //                var methodDescriptor = await codeProvider.FindMethodImplementationAsync(delegateInstance, t);
        //                callees.Add(methodDescriptor);
        //            }
        //        }
        //        else
        //        {
        //            // if Count is 0, it is a delegate that do not came form an instance variable
        //            callees.Add(delegateInstance);
        //        }
        //    }
        //    return callees;
        //}



        private static Task CreateAndSendCallMessageAsync(
                            AnalysisInvocationExpession callInfo,
                            MethodDescriptor realCallee, TypeDescriptor receiverType, PropagationKind propKind)
        {
            var callMessage = CreateCallMessage(callInfo, realCallee, receiverType, propKind);
            var callerMessage = new CallerMessage(new OrleansEntityDescriptor(callInfo.Caller), callMessage);
            var destination = new OrleansEntityDescriptor(realCallee);

            //await this.SendMessageAsync(destination, callerMessage);
            return AnalyzeCalleeAsync(realCallee,callerMessage,propKind);
            //var destinationEntity = await GetEntityAsync(destination);
            //var destinationGrain = (IMethodEntityGrain)destinationEntity;
            //await destinationGrain.ProcessMessaggeAsync(message.Source, message);

        }

        private static async Task AnalyzeCalleeAsync(MethodDescriptor calleeDescriptor, CallerMessage callerMessage,PropagationKind propKind)
        {

            var codeProvider = await ProjectGrainWrapper.CreateProjectGrainWrapperAsync(calleeDescriptor);
            Debug.WriteLine("Analyzing {0} ", calleeDescriptor);
            var orleansEnityDesc = new OrleansEntityDescriptor(calleeDescriptor);
            var methodEntityGrain = await OrleansDispatcher.CreateMethodEntityGrain(orleansEnityDesc);

            await methodEntityGrain.UpdateMethodArgumentsAsync(callerMessage.CallMessageInfo.Receivers, callerMessage.CallMessageInfo.ArgumentValues, propKind);

            var callsAndRets = await methodEntityGrain.PropagateAsync();
            await ProcessCalleesAffectedByPropagationAsync(callsAndRets.Calls, PropagationKind.ADD_TYPES, codeProvider);
            if(callsAndRets.RetValueChange)
            {
                await ProcessReturnAsync(callsAndRets.ReturnInfo);
            }
        }

        private static async Task ProcessReturnAsync(ISet<ReturnInfo> returnInfos)
        {
                var continuations = new List<Task>();
                foreach (var returnInfo in returnInfos)
                {
                    var retPotentialTypes = returnInfo.ReturnPotentialTypes;
                    //var t = DispachReturnMessageAsync(returnInfo.Caller, ret, propKind);
                    await t;
                    //continuations.Add(t);
                }

                await Task.WhenAll(continuations);

            }



    internal static async Task DispachReturnMessageAsync(CallContext context, VariableNode returnVariable, PropagationKind propKind)
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
			var destination = EntityFactory.Create(caller,this.dispatcher);
			var retMessageInfo = new ReturnMessageInfo(
				lhs,
				types,
				propKind, this.MethodEntity.InstantiatedTypes,
				context.Invocation);
			var returnMessage = new ReturnMessage(this.MethodEntity.EntityDescriptor, retMessageInfo);
			//if (lhs != null)
			{
				await this.SendMessageAsync(destination, returnMessage);
			}
			//else
			//{
			//    return new Task(() => { });
			//}
		}


        private static  CallMessageInfo CreateCallMessage(AnalysisInvocationExpession callInfo,
                                                            MethodDescriptor actuallCallee,
                                                            TypeDescriptor computedReceiverType,
                                                            PropagationKind propKind)
        {
            var calleType = computedReceiverType;
            ISet<TypeDescriptor> potentialReceivers = new HashSet<TypeDescriptor>();

            if (callInfo.Receiver != null)
            {
                potentialReceivers.Add(computedReceiverType);
            }
            return new CallMessageInfo(callInfo.Caller, actuallCallee, callInfo.CallNode,
                                                potentialReceivers, callInfo.ArgumentsPotentialTypes,
                                                callInfo.LHS, callInfo.InstantiatedTypes, propKind);
        }

    }
}