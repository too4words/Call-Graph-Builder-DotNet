// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Orleans;
using OrleansGrains;
using OrleansInterfaces;
using ReachingTypeAnalysis.Roslyn;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Analysis
{
	internal class MethodEntityGrain: Orleans.Grain, IMethodEntityGrain, IEntity
    {
        private IOrleansEntityDescriptor descriptor;
        private MethodEntity methodEntity;

        public override Task OnActivateAsync()
        {
            var guid = this.GetPrimaryKey();
            this.descriptor = new OrleansEntityDescriptor(guid);

            return TaskDone.Done;
        }
        public Task SetMethodEntity(MethodEntity methodEntity)
        {
            Contract.Assert(methodEntity != null);

            this.methodEntity = methodEntity;

            return TaskDone.Done;
        }

        public Task<IOrleansEntityDescriptor> GetDescriptor()
        {
            Contract.Assert(this.descriptor != null);

            return Task.FromResult<IOrleansEntityDescriptor>(this.descriptor);
        }

        public Task SetDescriptor(IOrleansEntityDescriptor descriptor)
        {
            Contract.Assert(descriptor != null);
            this.descriptor = descriptor;

            return TaskDone.Done;
        }

        public IEntityProcessor GetEntityProcessor(IDispatcher dispatcher)
        {
            Contract.Assert(this.methodEntity != null);
            Contract.Assert(dispatcher != null);

            return new MethodEntityProcessor(this.methodEntity, dispatcher, true);
        }

        internal MethodEntity GetMethodEntity()
        {
            Contract.Assert(this.methodEntity != null);
            return this.methodEntity;
        }

		public Task ReceiveMessageAsync(IOrleansEntityDescriptor source, IMessage message)
		{
			Contract.Assert(this.methodEntity != null);
			Contract.Assert(this.methodEntity.EntityProcessor != null);

            return this.methodEntity.EntityProcessor.ReceiveMessageAsync(source, message);
        }
	}
}
