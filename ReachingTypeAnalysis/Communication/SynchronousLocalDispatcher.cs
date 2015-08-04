﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Communication
{
    internal class SynchronousLocalDispatcher : Dispatcher
    {
        private Bag<IEntityDescriptor> activeEntities;

        public SynchronousLocalDispatcher(bool async = true)
            : base(async)
        {
            this.activeEntities = new Bag<IEntityDescriptor>();
        }

        public long MessageCount { get; protected set; }

        public override void DeliverMessage(IEntityDescriptor destination, IMessage message)
        {
            Task.Run(() => DeliverMessageAsync(destination, message)).Wait();

            //// We can put the message in the destination queue
            ////var entity = GetEntity(destination);
            //var entityProcessor = GetEntityWithProcessorAsync(destination).Result;

            //if (entityProcessor != null)
            //{
            //    // We really need to fix this. 
            //    // TO-DO Remove this: add the check in the HandleCallEvent method (as I did with async)
            //    Contract.Assert(activeEntities.Occurrences(destination) < 5);
            //    //if (activeEntities.Occurrences(destination) > 5)
            //    //{
            //    //    Console.Error.WriteLine("Occurs check on {0}", destination);
            //    //}
            //    //else
            //    {
            //        activeEntities.Add(destination);
            //        entityProcessor.ReceiveMessage(message.Source, message);
            //        activeEntities.Remove(destination);
            //    }
            //}
            //this.MessageCount++;
        }

        public override async Task DeliverMessageAsync(IEntityDescriptor destination, IMessage message)
        {
            // throw new NotImplementedException();
            var entityProcessor = await GetEntityWithProcessorAsync(destination);

            if (entityProcessor != null)
            {
                // We really need to fix this. 
                // TO-DO Remove this: add the check in the HandleCallEvent method (as I did with async)
                Contract.Assert(activeEntities.Occurrences(destination) < 5);
                //if (activeEntities.Occurrences(destination) > 5)
                //{
                //    Console.Error.WriteLine("Occurs check on {0}", destination);
                //}
                //else
                {
                    activeEntities.Add(destination);
                    await entityProcessor.ReceiveMessageAsync(message.Source, message);
                    activeEntities.Remove(destination);
                }
            }
            this.MessageCount++;
        }
    }
}
