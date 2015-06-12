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
        Guid Guid { get; set; }
        MethodDescriptor MethodDescriptor { get; set; }
        // MethodEntity MethodEntity { get; set; }
    }
    [Serializable]
    internal class OrleansEntityDescriptor :  IEntityDescriptor
    {
        public Guid Guid { get;  set; }
        public MethodDescriptor MethodDescriptor { get; set; }
        //public string Etag { get; set; }
        

        public OrleansEntityDescriptor(MethodDescriptor methodDescriptor, Guid guid)
        {
            this.Guid = guid;
            this.MethodDescriptor = methodDescriptor;
        }
    }

    [StorageProvider(ProviderName = "TestStore")]
    internal class MethodEntityGrain : Orleans.Grain<IOrleansEntityState>, IMethodEntityGrain
    {
        private OrleansEntityDescriptor orleansEntityDescriptor;
        
        [NonSerialized]
        private MethodEntity methodEntity;


        public override async Task OnActivateAsync()
        {
            var guid = this.GetPrimaryKey();
            // Shold not be null..
            if (this.State.MethodDescriptor != null)
            {
                var orleansEntityDesc = new OrleansEntityDescriptor(this.State.MethodDescriptor, this.State.Guid);
                // TODO: do we need to check and restore methodEntity
                this.methodEntity = (MethodEntity) await OrleansDispatcher.Instance.GetMethodEntityAsync(orleansEntityDesc);
            }
            //return TaskDone.Done;
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
            var guid = this.GetPrimaryKey();
            // Should not be null
            if (this.State != null)
            {
                this.State.MethodDescriptor = this.orleansEntityDescriptor.MethodDescriptor;
                return State.WriteStateAsync();
            }
            return TaskDone.Done;
        }

        public Task<IEntityDescriptor> GetDescriptor()
        {
            //Contract.Assert(this.State != null);
            if (this.State != null)
            {
                this.orleansEntityDescriptor = new OrleansEntityDescriptor(this.State.MethodDescriptor, this.State.Guid);   
            }
            return Task.FromResult<IEntityDescriptor>(this.orleansEntityDescriptor);
        }

        public Task SetDescriptor(IEntityDescriptor descriptor)
        {
            var orleansEntityDescriptor = (OrleansEntityDescriptor)descriptor;

            //Contract.Assert(this.State != null);
            if (this.State != null)
            {
                this.State.MethodDescriptor = orleansEntityDescriptor.MethodDescriptor;
            }

            return TaskDone.Done;
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

        public Task ReceiveMessageAsync(IEntityDescriptor source, IMessage message)
        {
            Contract.Assert(this.methodEntity != null);
            //Contract.Assert(this.methodEntity.EntityProcessor != null);
            var methodEntityProcessor = new MethodEntityProcessor(this.methodEntity, OrleansDispatcher.Instance);
            // this.methodEntity.GetEntityProcessor(OrleansDispatcher.Instance)
            return methodEntityProcessor.ReceiveMessageAsync(source, message);
        }

        public Task<bool> IsInitialized()
        {
            return Task.FromResult(this.methodEntity != null);
        }
    }
}
