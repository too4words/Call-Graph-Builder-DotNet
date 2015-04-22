// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Communication
{
    public abstract class EntityProcessor: IEntityProcessor
    {
        protected IDispatcher dispatcher;
        public virtual IEntity Entity { get; private set; }

        public EntityProcessor(IEntity entity, IDispatcher dispatcher)
        {
            this.Entity = entity;
            this.dispatcher = dispatcher;
        }

        public void SendMessage(IEntityDescriptor destination, IMessage message)
        {
            dispatcher.DeliverMessage(destination, message);
        }

        public void ReceiveMessage(IEntityDescriptor source, IMessage message)
        {
            ProcessMessage(source, message);
        }

        public Task SendMessageAsync(IEntityDescriptor destination, IMessage message)
        {
            return dispatcher.DeliverMessageAsync(destination, message);
        }

        public Task ReceiveMessageAsync(IEntityDescriptor source, IMessage message)
        {
            return ProcessMessageAsync(source, message);
        }

        public abstract void ProcessMessage(IEntityDescriptor source, IMessage message);
        public abstract Task ProcessMessageAsync(IEntityDescriptor source, IMessage message);

        public abstract void DoAnalysis();

        public abstract Task  DoAnalysisAsync();
    }
}
