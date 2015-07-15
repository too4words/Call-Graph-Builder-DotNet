// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Communication
{
    /// <summary>
    /// This dispatcher uses a separate threat on which to execute the 
    /// event dispatch.
    /// </summary>
    internal class AsyncDispatcher : OnDemandSyncDispatcher  
    {
        internal struct MessageWithDestination
        {
            //internal IEntity Source;
            internal IEntityDescriptor Destination;
            internal IMessage Message;

            public override string ToString()
            {
                return string.Format("{0} -> {1}", this.Message, this.Destination);
            }
        }

        public Solution Solution { get; private set; }
        private List<Task> Tasks = new List<Task>();
        //public long MessageCount { get; private set; }

        internal AsyncDispatcher()
            : base(true)
        {
            this.IsAsync = true;
        }

        //long iterations = 0L;

        public override void DeliverMessage(IEntityDescriptor destination, IMessage message)
        {
            //Logger.Instance.Log(string.Format("Delivering {0} to {1}...", message, destination));
            //this.MessageCount++;
            //var t = new Task(() =>
            //{
            //    var messageAndDestination = new MessageWithDestination
            //    {
            //        //Source = message.Source,
            //        Destination = destination,
            //        Message = message
            //    };
            //    var entityProcessor = GetEntityWithProcessor(messageAndDestination.Destination);
            //    if (entityProcessor != null)
            //    {
            //        entityProcessor.ReceiveMessage(messageAndDestination.Message.Source, messageAndDestination.Message);
            //    }
            //    //Console.WriteLine("Enqueueing {0}", messageAndDestination);
            //    //lock (this.Queue)
            //    //{
            //    //    this.Queue.Enqueue(messageAndDestination);
            //    //}
            //    //entity.ReceiveMessage(message.Source, message);
            //    //}
            //    Logger.Instance.Log(string.Format("Delivered {0} to {1}", message, destination));
            //});
            //lock (this.Tasks)
            //{
            //    this.Tasks.Add(t);
            //}
            //t.Start();
            throw new NotImplementedException();
        }

        public override async Task DeliverMessageAsync(IEntityDescriptor destination, IMessage message)
        {
            Logger.Instance.Log("AsyncDispatcher", "DeliverMessageAsync", "Delivering {0} to {1}", message, destination);
            this.MessageCount++;
            var messageAndDestination = new MessageWithDestination
            {
                //Source = message.Source,
                Destination = destination,
                Message = message
            };
            var entityProcessor = await GetEntityWithProcessorAsync(messageAndDestination.Destination);
                        
            if (entityProcessor != null)
            {
                await entityProcessor.ReceiveMessageAsync(
                    messageAndDestination.Message.Source, 
					messageAndDestination.Message);
            }
        }
    }
}