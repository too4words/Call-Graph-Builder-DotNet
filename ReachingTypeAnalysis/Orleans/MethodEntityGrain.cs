// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Orleans;
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
        //Guid Guid { get; set; }
        MethodDescriptor MethodDescriptor { get; set; }
    }

    [StorageProvider(ProviderName = "TestStore")]
    internal class MethodEntityGrain : Grain<IOrleansEntityState>, IMethodEntityGrain
    {
        private OrleansEntityDescriptor orleansEntityDescriptor;
        
        [NonSerialized]
        private MethodEntity methodEntity;
        //[NonSerialized]
        //private OrleansDispatcher dispacther;
        [NonSerialized]
        private IProjectCodeProviderGrain codeProviderGrain;
        public override async Task OnActivateAsync()
        {
			var solutionGrain = SolutionGrainFactory.GetGrain("Solution");
            

            //var guid = this.GetPrimaryKey();
            // Shold not be null..
            if (this.State.Etag!= null)
            {
				codeProviderGrain = await solutionGrain.GetCodeProviderAsync(this.State.MethodDescriptor);
                var orleansEntityDesc = new OrleansEntityDescriptor(this.State.MethodDescriptor);
                // TODO: do we need to check and restore methodEntity
                // To restore the full entity state we need to save propagation data
                // or repropagate
				this.methodEntity = (MethodEntity)await codeProviderGrain.CreateMethodEntityAsync(this.State.MethodDescriptor);// dispacther.GetMethodEntityAsync(orleansEntityDesc);
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
            //var guid = this.GetPrimaryKey();
            // Should not be null
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

        //public IEntityProcessor GetEntityProcessor(IDispatcher dispatcher)
        //{
        //    Contract.Assert(this.methodEntity != null);
        //    Contract.Assert(dispatcher != null);

        //    return new MethodEntityProcessor(this.methodEntity, dispatcher, true);
        //}

        public  Task<IEntity> GetMethodEntity()
        {
            // Contract.Assert(this.methodEntity != null);
            return Task.FromResult<IEntity>(this.methodEntity);
        }
        /// <summary>
        /// Ideally I would prefer to have a method GetEntityWithProcessor and 
        /// keep the same flow as the non-Orleans approach, but we cannot return a 
        /// MethodEntityProcessor...
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <returns></returns>

        public async Task DoAnalysisAsync(IDispatcher dispatcher)
        {
            Contract.Assert(this.methodEntity != null);
            var codeProvider = await ProjectGrainWrapper.CreateProjectGrainWrapperAsync(methodEntity.MethodDescriptor);
            var methodEntityProcessor = new MethodEntityProcessor(this.methodEntity, dispatcher, codeProvider);
            await methodEntityProcessor.DoAnalysisAsync();
        }

        /// <summary>
        /// Ideally I would prefer to have a method GetEntityWithProcessor and 
        /// keep the same flow as the non-Orleans approach, but we cannot return a 
        /// MethodEntityProcessor...
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <returns></returns>

        public async Task ProcessMessaggeAsync(IEntityDescriptor source, IMessage message, IDispatcher dispatcher)
        {
            Contract.Assert(this.methodEntity != null);
            var codeProvider = await ProjectGrainWrapper.CreateProjectGrainWrapperAsync(methodEntity.MethodDescriptor);
            var methodEntityProcessor = new MethodEntityProcessor(this.methodEntity, dispatcher,codeProvider);
            await methodEntityProcessor.ProcessMessageAsync(source, message);
        }

        public async Task<IEntityProcessor> GetEntityWithProcessorAsync(IDispatcher dispatcher)
        {
            Contract.Assert(this.methodEntity != null);
            var codeProvider = await ProjectGrainWrapper.CreateProjectGrainWrapperAsync(methodEntity.MethodDescriptor);
            var methodEntityProcessor = new MethodEntityProcessor(this.methodEntity, dispatcher, codeProvider);
            return methodEntityProcessor;
        }

        public Task<bool> IsInitialized()
        {
            return Task.FromResult(this.methodEntity != null);
        }
    }
}
