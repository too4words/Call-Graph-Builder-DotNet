// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using ReachingTypeAnalysis.Analysis;
using ReachingTypeAnalysis.Roslyn;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Communication
{
    /// <summary>
    /// This dispatcher uses a separate threat on which to execute the 
    /// event dispatch.
    /// </summary>
    internal class QueueingDispatcher : OnDemandSyncDispatcher, IDisposable
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

        private Queue<MessageWithDestination> Queue { get; set; }
        private Task Task { get; set; }
        public Compilation Solution { get; private set; }
        private bool loop = true;
        //public long MessageCount { get; private set; }

        internal QueueingDispatcher()
            : base(true)
        {
            //            this.Thread = new Thread(() => Run());
            //            this.Thread.Start();
            this.Queue = new Queue<MessageWithDestination>();
            //this.Task = new Task(() => Run());

            //Task.Start();

            //this.Task.RunSynchronously();
            //this.task = Task.Run(() => Run());
        }

        public bool IsDoneProcessing
        {
            get { return Queue.Count == 0; }
        }

        public int GetQueueCount()
        {
            return Queue.Count;
        }

        private void Run()
        {
            long iterations = 0L;
            long queueSize = 0L;
            long threadCounts = 0L;

            while (loop)
            {
                lock (this.Queue)
                {
                    if (this.Queue.Count > 0)
                    {
                        iterations++;
                        queueSize += this.Queue.Count;
                        threadCounts += Process.GetCurrentProcess().Threads.Count;
                    
                        var next = this.Queue.Dequeue();
                        var temp = next;
                        // this can be  done on a different thread and outside the lock
                        var task = Task.Run(() =>
                        {
                            Console.WriteLine("Dequeueing {0}", temp);
                            // Here we can use GetEntitywithProcessor or 
                            // GetEntity(dest) + GetEntityProcesor(entity) 
                            //IEntity entity = GetEntity(temp.Destination);
                            // I choose the first option but can be easily changed
                            var entityProcessor = GetEntityWithProcessorAsync(temp.Destination).Result;
                            if (entityProcessor != null)
                            {
                                entityProcessor.ReceiveMessage(temp.Message.Source, temp.Message);
                            }
                        });
                    }
                }
            }

            Contract.Assert(this.Queue.Count == 0);
            Contract.Assert(iterations != 0);

            var averageLength = 1.0 * queueSize / iterations;
            var averageThreads = threadCounts / iterations;

            Debug.WriteLine("Average queue length: {0}", averageLength);
            Debug.WriteLine("Thread count: {0}", 1.0 * averageThreads);
        }

        public override void DeliverMessage(IEntityDescriptor destination, IMessage message)
        {
            this.MessageCount++;
            Task.Factory.StartNew(() =>
            {
                var messageAndDestination = new MessageWithDestination
                {
                    //Source = message.Source,
                    Destination = destination,
                    Message = message
                };
                Console.WriteLine("Enqueueing {0}", messageAndDestination);
                lock (this.Queue)
                {
                    this.Queue.Enqueue(messageAndDestination);
                }
                //entity.ReceiveMessage(message.Source, message);
                //}
                //return messageAndDestination;
            });
            //.ContinueWith((mad) => ProcessNext(mad.Result));
        }

        public void Dispose()
        {
            loop = false;
            this.Task.Wait();
            //this.Thread.Abort();
        }
    }
}
