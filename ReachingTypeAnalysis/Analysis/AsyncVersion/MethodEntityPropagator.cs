﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using ReachingTypeAnalysis.Communication;
using ReachingTypeAnalysis.Roslyn;
using System.Diagnostics.Contracts;

namespace ReachingTypeAnalysis.Analysis
{
    /// <summary>
    /// This Classs plays the role of the MethodEntityProcessor but it is used by the OndemandAnalysisAsync
    /// and OnDemandOrleans
    /// This should replace the MethodEntityProcessor when we get rid of the MethodEntityProccesor
    /// </summary>
    internal class MethodEntityWithPropagator : IMethodEntityWithPropagator
    {
        private MethodEntity methodEntity;
        private ICodeProvider codeProvider;
        //private Orleans.Runtime.Logger logger = GrainClient.Logger;

        /// <summary>
        /// This build a MethodEntityPropagator with a solution
        /// The solution provides its CodeProvicer
        /// </summary>
        /// <param name="methodDescriptor"></param>
        /// <param name="solution"></param>
        public MethodEntityWithPropagator(MethodDescriptor methodDescriptor, Solution solution)
        {
            //var providerAndSyntax = ProjectCodeProvider.GetProjectProviderAndSyntaxAsync(methodDescriptor,solution).Result;
            //this.codeProvider = providerAndSyntax.Item1;
            //this.methodEntity = (MethodEntity)codeProvider.CreateMethodEntityAsync(methodDescriptor).Result;

            var providerEntity = ProjectCodeProvider.FindCodeProviderAndEntity(methodDescriptor, solution).Result;
            this.methodEntity = providerEntity.Item2;
            this.codeProvider = providerEntity.Item1;
            SolutionManager.Instance.AddInstantiatedTypes(this.methodEntity.InstantiatedTypes);
        }

        /// <summary>
        /// Creates the Propagator using directly an entity and a provider
        /// This can be used by the MethodEntityGrain
        /// </summary>
        /// <param name="methodEntity"></param>
        /// <param name="provider"></param>
        public MethodEntityWithPropagator(MethodEntity methodEntity, ICodeProvider provider)
        {
            this.codeProvider = provider;
            this.methodEntity = methodEntity;
        }

        public async Task<PropagationEffects> PropagateAsync(PropagationKind propKind)
        {
            Logger.LogS("MethodEntityGrain", "PropagateAsync", "Propagation for {0} ", this.methodEntity.MethodDescriptor);

            // var codeProvider = await ProjectGrainWrapper.CreateProjectGrainWrapperAsync(this.methodEntity.MethodDescriptor);
            var propagationEffects = await this.methodEntity.PropGraph.PropagateAsync(codeProvider);

            foreach (var calleeInfo in propagationEffects.CalleesInfo)
            {
                //  Add instanciated types! 
                /// Diego: Ben. This may not work well in parallel... 
                /// We need a different way to update this info
                calleeInfo.InstantiatedTypes = this.methodEntity.InstantiatedTypes;

                // TODO: This is because of the refactor
                if (calleeInfo is MethodCallInfo)
                {
                    var methodCallInfo = calleeInfo as MethodCallInfo;
                    methodCallInfo.ReceiverPossibleTypes = GetTypes(methodCallInfo.Receiver);
                    methodCallInfo.PossibleCallees = await GetPossibleCalleesForMethodCallAsync(methodCallInfo, codeProvider);
                }
                else if (calleeInfo is DelegateCallInfo)
                {
                    var delegateCalleeInfo = calleeInfo as DelegateCallInfo;
                    delegateCalleeInfo.ReceiverPossibleTypes = GetTypes(delegateCalleeInfo.Delegate);
                    delegateCalleeInfo.PossibleCallees = await GetPossibleCalleesForDelegateCallAsync(delegateCalleeInfo, codeProvider);
                }

                for (int i = 0; i < calleeInfo.Arguments.Count; i++)
                {
                    var arg = calleeInfo.Arguments[i];
                    var potentialTypes = arg != null ? GetTypes(arg, propKind) : new HashSet<TypeDescriptor>();
                    calleeInfo.ArgumentsPossibleTypes.Add(potentialTypes);
                }
            }

            if (this.methodEntity.ReturnVariable != null)
            {
                foreach (var callerContext in this.methodEntity.Callers)
                {
                    var returnInfo = new ReturnInfo(this.methodEntity.MethodDescriptor, callerContext);
                    returnInfo.ResultPossibleTypes = GetTypes(this.methodEntity.ReturnVariable);
                    returnInfo.InstantiatedTypes = this.methodEntity.InstantiatedTypes;

                    propagationEffects.CallersInfo.Add(returnInfo);
                }
            }
            Logger.LogS("MethodEntityGrain", "PropagateAsync", "End Propagation for {0} ", this.methodEntity.MethodDescriptor);
            //this.methodEntity.Save(@"C:\Temp\"+this.methodEntity.MethodDescriptor.MethodName + @".dot");
            return propagationEffects;
        }

        public async Task<PropagationEffects> PropagateAsync(CallMessageInfo callMessageInfo)
        {
            Logger.LogS("MethodEntityGrain", "PropagateAsync-call", "Propagation for {0} ", callMessageInfo.Callee);
            if (!this.methodEntity.CanBeAnalized) return new PropagationEffects(new HashSet<CallInfo>(), false);

            if (this.methodEntity.ThisRef != null)
            {
                await this.methodEntity.PropGraph.DiffPropAsync(callMessageInfo.ReceiverPossibleTypes, this.methodEntity.ThisRef, callMessageInfo.PropagationKind);
            }

            for (var i = 0; i < this.methodEntity.ParameterNodes.Count; i++)
            {
                var parameterNode = this.methodEntity.ParameterNodes[i];

                if (parameterNode != null)
                {
                    await this.methodEntity.PropGraph.DiffPropAsync(callMessageInfo.ArgumentsPossibleTypes[i], parameterNode, callMessageInfo.PropagationKind);
                }
            }

            var context = new CallContext(callMessageInfo.Caller, callMessageInfo.LHS, callMessageInfo.CallNode);
            this.methodEntity.AddToCallers(context);


            var effects = await PropagateAsync(callMessageInfo.PropagationKind);
            Logger.LogS("MethodEntityGrain", "PropagateAsync-call", "End Propagation for {0} ", callMessageInfo.Callee);
            return effects;
        }

        public async Task<PropagationEffects> PropagateAsync(ReturnMessageInfo returnMessageInfo)
        {
            Logger.LogS("MethodEntityGrain", "PropagateAsync-return", "Propagation for {0} ", returnMessageInfo.Caller);
            //PropGraph.Add(lhs, retValues);
            await this.methodEntity.PropGraph.DiffPropAsync(returnMessageInfo.ResultPossibleTypes, returnMessageInfo.LHS, returnMessageInfo.PropagationKind);
            var effects = await PropagateAsync(returnMessageInfo.PropagationKind);
            Logger.LogS("MethodEntityGrain", "PropagateAsync-return", "End Propagation for {0} ", returnMessageInfo.Caller);
            return effects;
        }
        public async Task<ISet<MethodDescriptor>> GetCalleesAsync(int invocationPosition)
        {
            var invocationNode = methodEntity.GetCallSiteByOrdinal(invocationPosition);
            ISet<MethodDescriptor> result;
            var calleesForNode = new HashSet<MethodDescriptor>();
            var invExp = methodEntity.PropGraph.GetInvocationInfo((AnalysisCallNode)invocationNode);
            Contract.Assert(invExp != null);
            Contract.Assert(codeProvider != null);
            var calleeResult = await methodEntity.PropGraph.ComputeCalleesForNodeAsync(invExp, codeProvider);
            calleesForNode.UnionWith(calleeResult);
            result = calleesForNode;
            return result;
            // return await CallGraphQueryInterface.GetCalleesAsync(methodEntity, invocationNode, this.codeProvider);
        }
        public Task<int> GetInvocationCountAsync()
        {
            return Task.FromResult(methodEntity.PropGraph.CallNodes.Count());
        }

        private async Task<ISet<MethodDescriptor>> GetPossibleCalleesForMethodCallAsync(MethodCallInfo methodCallInfo, ICodeProvider codeProvider)
        {
            var possibleCallees = new HashSet<MethodDescriptor>();

            // TODO: This is not good: one reason is that loads like b = this.f are not working
            // in a method m after call r.m() because only the value of r is passed and not all its structure (fields)

            if (methodCallInfo.Method.IsStatic)
            {
                // Static method call
                possibleCallees.Add(methodCallInfo.Method);
            }
            else
            {
                // Instance method call

                //// I need to compute all the callees
                //// In case of a deletion we can discard the deleted callee

                //// If callInfo.ReceiverPossibleTypes == {} it means that some info in missing => we should be conservative and use the instantiated types (RTA) 
                //// TODO: I make this False for testing what happens if we remove this

                //if (conservativeWithTypes && methodCallInfo.ReceiverPossibleTypes.Count == 0)
                //{
                //	// TO-DO: Should I fix the node in the receiver to show that is not loaded. Ideally I should use the declared type. 
                //	// Here I will use the already instantiated types

                //	foreach (var candidateTypeDescriptor in methodCallInfo.InstantiatedTypes)
                //	{
                //		var isSubtype = await codeProvider.IsSubtypeAsync(candidateTypeDescriptor, methodCallInfo.Receiver.Type);

                //		if (isSubtype)
                //		{
                //			methodCallInfo.ReceiverPossibleTypes.Add(candidateTypeDescriptor);
                //		}
                //	}
                //}

                if (methodCallInfo.ReceiverPossibleTypes.Count > 0)
                {
                    foreach (var receiverType in methodCallInfo.ReceiverPossibleTypes)
                    {
                        // Given a method m and T find the most accurate implementation wrt to T
                        // it can be T.m or the first super class implementing m
                        var methodDescriptor = await codeProvider.FindMethodImplementationAsync(methodCallInfo.Method, receiverType);
                        possibleCallees.Add(methodDescriptor);
                    }
                }
                //else
                //{
                //    // We don't have any possibleType for the receiver,
                //    // so we just use the receiver's declared type to
                //    // identify the calle method implementation
                //    possibleCallees.Add(methodCallInfo.Method);
                //}
            }

            return possibleCallees;
        }

        private async Task<ISet<MethodDescriptor>> GetPossibleCalleesForDelegateCallAsync(DelegateCallInfo delegateCallInfo, ICodeProvider codeProvider)
        {
            var possibleCallees = new HashSet<MethodDescriptor>();
            var possibleDelegateMethods = GetPossibleMethodsForDelegate(delegateCallInfo.Delegate);

            foreach (var method in possibleDelegateMethods)
            {
                if (method.IsStatic)
                {
                    // Static method call
                    possibleCallees.Add(method);
                }
                else
                {
                    // Instance method call

                    if (delegateCallInfo.ReceiverPossibleTypes.Count > 0)
                    {
                        foreach (var receiverType in delegateCallInfo.ReceiverPossibleTypes)
                        {
                            //var aMethod = delegateInstance.FindMethodImplementation(t);
                            // Diego: Should I use : codeProvider.FindImplementation(delegateInstance, t);
                            var callee = await codeProvider.FindMethodImplementationAsync(method, receiverType);
                            possibleCallees.Add(callee);
                        }
                    }
                    else
                    {
                        // We don't have any possibleType for the receiver,
                        // so we just use the receiver's declared type to
                        // identify the calle method implementation

                        // if Count is 0, it is a delegate that do not came form an instance variable
                        possibleCallees.Add(method);
                    }
                }
            }

            return possibleCallees;
        }

        private ISet<TypeDescriptor> GetTypes(PropGraphNodeDescriptor analysisNode)
        {
            if (analysisNode != null)
            {
                return this.methodEntity.PropGraph.GetTypes(analysisNode);
            }
            else
            {
                return new HashSet<TypeDescriptor>();
            }
        }
        private ISet<TypeDescriptor> GetTypes(PropGraphNodeDescriptor node, PropagationKind prop)
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
        internal ISet<TypeDescriptor> GetDeletedTypes(PropGraphNodeDescriptor node)
        {
            if (node != null)
            {
                return this.methodEntity.PropGraph.GetDeletedTypes(node);
            }
            else
            {
                return new HashSet<TypeDescriptor>();
            }
        }
        internal ISet<MethodDescriptor> GetPossibleMethodsForDelegate(DelegateVariableNode node)
        {
            return this.methodEntity.PropGraph.GetDelegates(node);
        }

        public Task<IEntity> GetMethodEntityAsync()
        {
            // Contract.Assert(this.methodEntity != null);
            return Task.FromResult<IEntity>(this.methodEntity);
        }

        public Task<ISet<MethodDescriptor>> GetCalleesAsync()
        {
            var codeProvider = this.codeProvider;
            Contract.Assert(codeProvider != null);
            return CallGraphQueryInterface.GetCalleesAsync(this.methodEntity, codeProvider);
        }

        public Task<IDictionary<AnalysisCallNode, ISet<MethodDescriptor>>> GetCalleesInfoAsync()
        {
            return CallGraphQueryInterface.GetCalleesInfo(this.methodEntity, this.codeProvider);
        }
        public Task<bool> IsInitializedAsync()
        {
            return Task.FromResult(this.methodEntity != null);
        }
    }
}