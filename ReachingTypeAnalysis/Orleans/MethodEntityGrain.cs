// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

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

        public async Task<PropagationEffects> PropagateAsync()
        {
            //TODO: Pass propKind as parameters
            var propKind = PropagationKind.ADD_TYPES;

            var codeProvider = await ProjectGrainWrapper.CreateProjectGrainWrapperAsync(methodEntity.MethodDescriptor);
            var callsAndRet = await this.methodEntity.PropGraph.PropagateAsync(codeProvider);
            foreach (var invocationInfo in callsAndRet.Calls)
            {
                //  Add instanciated types! 
                /// Diego: Ben. This may not work well in parallel... 
                /// We need a different way to update this info
                invocationInfo.InstantiatedTypes = this.methodEntity.InstantiatedTypes;
                // TODO: This is because of the refactor
                if (invocationInfo is CallInfo)
                {
                    invocationInfo.ReceiverPotentialTypes = GetTypes(invocationInfo.Receiver);
                }
                else
                {
                    Contract.Assert(invocationInfo is DelegateCallInfo);
                    var delegateInvocation = (DelegateCallInfo)invocationInfo;
                    invocationInfo.ReceiverPotentialTypes = GetTypes(delegateInvocation.CalleeDelegate);
                    delegateInvocation.ResolvedCallees = await GetDelegateCalleesAsync(delegateInvocation.CalleeDelegate, codeProvider);
                }


                for (int i = 0; i < invocationInfo.Arguments.Count; i++)
                {
                    var arg = invocationInfo.Arguments[i];
                    var potentialTypes = arg != null ? GetTypes(arg, propKind) : new HashSet<TypeDescriptor>();
                    invocationInfo.ArgumentsPotentialTypes.Add(potentialTypes);
                }
            }

            //TODO: Add instanciation for return 
            foreach(var callerContext in this.methodEntity.Callers)
            {
                var returnInfo = new ReturnInfo(callerContext);
                //TODO: add return info, compute return values with GetTypes from returnVariable (if !null)
                returnInfo.ReturnPotentialTypes = GetTypes(this.methodEntity.ReturnVariable);
                returnInfo.InstantiatedTypes = this.methodEntity.InstantiatedTypes;
                callsAndRet.ReturnInfoForCallers.Add(returnInfo);
            }
           
            return callsAndRet;
        }

        public async Task UpdateMethodArgumentsAsync(ISet<TypeDescriptor> receiverTypes, 
            IList<ISet<TypeDescriptor>> argumentsPotentialTypes, PropagationKind propKind)
        {
            if (!this.methodEntity.CanBeAnalized)
            {
                return;
            }
            if (this.methodEntity.ThisRef != null)
            {
                await this.methodEntity.PropGraph.DiffPropAsync(receiverTypes, this.methodEntity.ThisRef, propKind);
            }
            for(int i = 0; i < this.methodEntity.ParameterNodes.Count;i++)
            {
                var parameterNode = this.methodEntity.ParameterNodes[i];

                if (parameterNode != null)
                {
                    await this.methodEntity.PropGraph.DiffPropAsync(argumentsPotentialTypes[i], parameterNode, propKind);
                }
            }
            return; 
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



        private async Task<ISet<MethodDescriptor>> GetDelegateCalleesAsync(DelegateVariableNode delegateVariableNode, 
                                                                                    ICodeProvider codeProvider)
        {
            var callees = new HashSet<MethodDescriptor>();
            var types = GetTypes(delegateVariableNode);
            foreach (var delegateInstance in GetDelegates(delegateVariableNode))
            {
                if (types.Count > 0)
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
        internal ISet<MethodDescriptor> GetDelegates(DelegateVariableNode node)
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
