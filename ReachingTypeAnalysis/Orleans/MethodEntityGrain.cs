// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Orleans;
using Orleans.Concurrency;
using Orleans.Providers;
using OrleansInterfaces;
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
    [Reentrant]
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
 				dispatcher = new OrleansDispatcher(orleansEntityDesc,this.methodEntity);
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
			dispatcher = new OrleansDispatcher(descriptor, this.methodEntity);
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
