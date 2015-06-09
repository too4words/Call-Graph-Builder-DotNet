// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Orleans;
using OrleansInterfaces;
using ReachingTypeAnalysis.Roslyn;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Analysis
{
    [Serializable]
    internal class OrleansEntityDescriptor : IEntityDescriptor
    {
        public Guid Guid { get;  set; }
        public MethodDescriptor MethodDescriptor { get; private set; }
        public OrleansEntityDescriptor(MethodDescriptor methodDescriptor, Guid guid)
        {
            this.Guid = guid;
            this.MethodDescriptor = methodDescriptor;
        }
    }
    internal class MethodEntityGrain : Orleans.Grain, IMethodEntityGrain
    {
        private IEntityDescriptor descriptor;
        [NonSerialized]
        private MethodEntity methodEntity;

        public override Task OnActivateAsync()
        {
            var guid = this.GetPrimaryKey();
            //this.descriptor =  new OrleansEntityDescriptor(guid);

            return TaskDone.Done;
        }
        public Task SetMethodEntity(IEntity methodEntity, IEntityDescriptor descriptor)
        {
            Contract.Assert(methodEntity != null);

            this.methodEntity = (MethodEntity) methodEntity;
            this.descriptor = descriptor;
            return TaskDone.Done;
        }

        public Task<IEntityDescriptor> GetDescriptor()
        {
            Contract.Assert(this.descriptor != null);

            return Task.FromResult<IEntityDescriptor>(this.descriptor);
        }

        public Task SetDescriptor(IEntityDescriptor descriptor)
        {
            Contract.Assert(descriptor != null);
            this.descriptor = descriptor;

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
            Contract.Assert(this.methodEntity != null);
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
