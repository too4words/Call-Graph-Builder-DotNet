﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Orleans;
using Orleans.Concurrency;
using Orleans.Providers;
using OrleansInterfaces;
using ReachingTypeAnalysis.Communication;
using ReachingTypeAnalysis.Roslyn;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Analysis
{
    public interface IOrleansEntityState: IGrainState
    {
        MethodDescriptor MethodDescriptor { get; set; }
    }

    [StorageProvider(ProviderName = "TestStore")]
    //[Reentrant]
    internal class MethodEntityGrain : Grain<IOrleansEntityState>, IMethodEntityGrain
    {
		private const bool conservativeWithTypes = false;

		private OrleansEntityDescriptor orleansEntityDescriptor;
        /// <summary>
        /// Each grain will use its own dispatcher
        /// </summary>
		private OrleansDispatcher dispatcher;
        [NonSerialized]
        private MethodEntity methodEntity;
        [NonSerialized]
        private MethodEntityProcessor methodEntityProcessor;
        [NonSerialized]
        private IProjectCodeProviderGrain codeProviderGrain;
        public override async Task OnActivateAsync()
        {
	        // Shold not be null..
            if (this.State.Etag!= null)
            {
                var solutionGrain = SolutionGrainFactory.GetGrain("Solution");
				codeProviderGrain = await solutionGrain.GetCodeProviderAsync(this.State.MethodDescriptor);
                var orleansEntityDesc = new OrleansEntityDescriptor(this.State.MethodDescriptor);
                // TODO: do we need to check and restore methodEntity
                // To restore the full entity state we need to save propagation data
                // or repropagate
				this.methodEntity = (MethodEntity)await codeProviderGrain.CreateMethodEntityAsync(this.State.MethodDescriptor);
                dispatcher = new OrleansDispatcher(orleansEntityDesc, this.AsReference<IMethodEntityGrain>());
            }
        }

        public override Task OnDeactivateAsync()
        {
            this.methodEntity = null;
            return TaskDone.Done;
        }

        public Task SetMethodEntity(IEntity methodEntity, IEntityDescriptor descriptor)
        {
            Contract.Assert(methodEntity != null);
            this.orleansEntityDescriptor = (OrleansEntityDescriptor)descriptor;
            this.methodEntity = (MethodEntity) methodEntity;
			dispatcher = new OrleansDispatcher(descriptor, this.AsReference<IMethodEntityGrain>());
            Contract.Assert(this.State != null);
            this.State.MethodDescriptor = this.orleansEntityDescriptor.MethodDescriptor;
            return State.WriteStateAsync();
        }

        public Task<IEntityDescriptor> GetDescriptor()
        {
            return Task.FromResult<IEntityDescriptor>(this.orleansEntityDescriptor);
        }

        public Task SetDescriptor(IEntityDescriptor descriptor)
        {
            var orleansEntityDescriptor = (OrleansEntityDescriptor)descriptor;

            Contract.Assert(this.State != null);
            this.State.MethodDescriptor = orleansEntityDescriptor.MethodDescriptor;
            return State.WriteStateAsync();
        }

        public  Task<IEntity> GetMethodEntity()
        {
            // Contract.Assert(this.methodEntity != null);
            return Task.FromResult<IEntity>(this.methodEntity);
        }
        /// <summary>
        /// Ideally I would prefer to have a method GetEntityWithProcessor and 
        /// keep the same flow as the non-Orleans approach, but this require
        /// to have the EntityProcessor serializable
        /// </summary>
        public async Task DoAnalysisAsync()
        {
            Contract.Assert(this.methodEntity != null);
            var codeProvider = await ProjectGrainWrapper.CreateProjectGrainWrapperAsync(methodEntity.MethodDescriptor);
            var methodEntityProcessor = new MethodEntityProcessor(this.methodEntity, this.dispatcher, codeProvider);
            await methodEntityProcessor.DoAnalysisAsync();
        }

        /// <summary>
        /// Ideally I would prefer to have a method GetEntityWithProcessor and 
        /// keep the same flow as the non-Orleans approach, but this require
        /// to have the EntityProcessor serializable
        /// </summary>
        public async Task ProcessMessaggeAsync(IEntityDescriptor source, IMessage message)
        {
            Contract.Assert(this.methodEntity != null);
            var codeProvider = await ProjectGrainWrapper.CreateProjectGrainWrapperAsync(methodEntity.MethodDescriptor);
            var methodEntityProcessor = new MethodEntityProcessor(this.methodEntity, this.dispatcher,codeProvider);
            await methodEntityProcessor.ProcessMessageAsync(source, message);
        }

        public async Task<PropagationEffects> PropagateAsync(PropagationKind propKind)
        {
            var codeProvider = await ProjectGrainWrapper.CreateProjectGrainWrapperAsync(this.methodEntity.MethodDescriptor);
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
           
            return propagationEffects;
        }

		public async Task UpdateMethodArgumentsAsync(ISet<TypeDescriptor> receiverTypes, 
            IList<ISet<TypeDescriptor>> argumentsPossibleTypes, PropagationKind propKind)
        {
            if (!this.methodEntity.CanBeAnalized) return;

            if (this.methodEntity.ThisRef != null)
            {
                await this.methodEntity.PropGraph.DiffPropAsync(receiverTypes, this.methodEntity.ThisRef, propKind);
            }

            for (var i = 0; i < this.methodEntity.ParameterNodes.Count; i++)
            {
                var parameterNode = this.methodEntity.ParameterNodes[i];

                if (parameterNode != null)
                {
                    await this.methodEntity.PropGraph.DiffPropAsync(argumentsPossibleTypes[i], parameterNode, propKind);
                }
            }
        }

        public async Task UpdateMethodReturnAsync(ISet<TypeDescriptor> returnValues, VariableNode lhs, PropagationKind propKind)
        {
            //PropGraph.Add(lhs, retValues);
            await this.methodEntity.PropGraph.DiffPropAsync(returnValues,lhs, propKind);
            // This should be Async
            //switch (retMesssageInfo.PropagationKind)
            //{
            //    case PropagationKind.ADD_TYPES:
            //        Propagate();
            //        break;
            //    case PropagationKind.REMOVE_TYPES:
            //        PropagateDelete();
            //        break;
            //    default:
            //        throw new ArgumentException();
            //}
            //return;
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
				else
				{
					// We don't have any possibleType for the receiver,
					// so we just use the receiver's declared type to
					// identify the calle method implementation
					possibleCallees.Add(methodCallInfo.Method);
				}
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

        /// <summary>
        /// We use this to obtain a processor directly from the grain
        /// If we make it public we require the processor to be serializable
        /// One option is making this private and implemente 
        /// DoAnalysisAsync and ProcessMessageAsync in the grain
        /// </summary>
        /// <returns></returns>
        public async Task<IEntityProcessor> GetEntityWithProcessorAsync()
        {
            Contract.Assert(this.methodEntity != null);
            if(this.methodEntityProcessor==null)
            {
                var codeProvider = await ProjectGrainWrapper.CreateProjectGrainWrapperAsync(methodEntity.MethodDescriptor);
                methodEntityProcessor = new MethodEntityProcessor(this.methodEntity, this.dispatcher, codeProvider);
            }
            return methodEntityProcessor;
        }

        public Task<bool> IsInitialized()
        {
            return Task.FromResult(this.methodEntity != null);
        }
    }
}
