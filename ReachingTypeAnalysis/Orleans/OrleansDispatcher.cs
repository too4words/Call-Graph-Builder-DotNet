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
        public Guid Guid { get; set; }
        public MethodDescriptor MethodDescriptor { get; set; }
        //public string Etag { get; set; }


        public OrleansEntityDescriptor(MethodDescriptor methodDescriptor)
        {
            this.Guid = Guid.Empty;
            this.MethodDescriptor = methodDescriptor;
        }

        public OrleansEntityDescriptor(MethodDescriptor methodDescriptor, Guid guid)
        {
            this.Guid = guid;
            this.MethodDescriptor = methodDescriptor;
        }
        public override bool Equals(object obj)
        {
            var oed = (OrleansEntityDescriptor)obj;
            return oed != null && this.Guid.Equals(oed.Guid);
        }
        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }
    }

    [Serializable]
	internal class OrleansDispatcher : IDispatcher
	{
        // Keeps a mapping between method descriptors and orleans entities Guids
        // Should be necessary if we are able to build the OrleansDescriptors properly
        //private Dictionary<MethodDescriptor, Guid> orleansGuids = new Dictionary<MethodDescriptor, Guid>();//
        private ISet<MethodDescriptor> entities = new HashSet<MethodDescriptor>();
        internal static OrleansDispatcher Instance = null;
		public OrleansDispatcher()
		{
            Instance = this;
		}

		public void DeliverMessage(IEntityDescriptor destination, IMessage message)
		{
			throw new NotImplementedException();
		}

		public async Task DeliverMessageAsync(IEntityDescriptor destination, IMessage message)
		{
            var destinationEntity = await GetEntityAsync(destination);
            var destinationGrain = (IMethodEntityGrain)destinationEntity;
          
            //var guid = ((OrleansEntityDescriptor)destination).Guid;
            //var destinationGrain = MethodEntityGrainFactory.GetGrain(guid);
			/*return*/ 

            await ReceiveMessageAsync((OrleansEntityDescriptor)message.Source, 
                                    message,destinationGrain);
			//var processor = destinationGrain.GetEntityProcessor(this);
			//return processor.ReceiveMessageAsync(message.Source, message);
		}

        public async Task ReceiveMessageAsync(IEntityDescriptor source, 
                                        IMessage message,
                                        IMethodEntityGrain destinationGrain)
        {
            await destinationGrain.ProcessMessagge(source, message,this);
        }


        /// <summary>
        ///  
        /// </summary>
        /// <returns></returns>
		public ImmutableHashSet<IEntity> GetAllEntites()
		{
            var result = new HashSet<IEntity>();
            foreach(var methodDescriptor in this.entities.ToImmutableList())
            {
                var orleansEnitityDesc = new OrleansEntityDescriptor(methodDescriptor);
                IMethodEntityGrain entity = (IMethodEntityGrain)GetEntityAsync(orleansEnitityDesc).Result;
                result.Add(entity.GetMethodEntity().Result);
            }
            return result.ToImmutableHashSet<IEntity>();
           // throw new NotImplementedException();
		}
        public async Task<MethodEntity> GetMethodEntityAsync(IEntityDescriptor entityDesc)
        {
            var grainDesc = (OrleansEntityDescriptor)entityDesc;
            //Contract.Assert(grainDesc != null);
            Contract.Assert(grainDesc.MethodDescriptor != null);
            return await ProjectCodeProvider.FindProviderAndCreateMethodEntityAsync(grainDesc.MethodDescriptor);
        }

        public async Task<IEntity> GetEntityAsync(IEntityDescriptor entityDesc)
        {
            Contract.Assert(entityDesc != null);
            var grainDesc = (OrleansEntityDescriptor)entityDesc;
            Contract.Assert(grainDesc != null);

            //var guid = ((OrleansEntityDescriptor)grainDesc).Guid;
            var methodEntityGrain = MethodEntityGrainFactory.GetGrain(grainDesc.MethodDescriptor.ToString());
            // check if the result is initialized
            var methodEntity = await methodEntityGrain.GetMethodEntity();
            if (methodEntity == null)
            {
                Contract.Assert(grainDesc.MethodDescriptor != null);
				////  methodEntity = await providerGrain.CreateMethodEntityAsync(grainDesc.MethodDescriptor);
                methodEntity = await CreateMethodEntityUsingGrainsAsync(grainDesc.MethodDescriptor);
                Contract.Assert(methodEntity != null);
                methodEntityGrain.SetMethodEntity(methodEntity, grainDesc).Wait();
                methodEntityGrain.SetDescriptor(grainDesc).Wait();
                return methodEntityGrain;
            }
            else
            {
                return methodEntityGrain;
            }
        }

        async internal static Task<MethodEntity> CreateMethodEntityUsingGrainsAsync(MethodDescriptor methodDescriptor)
        {
            MethodEntity methodEntity = null;
            var solutionGrain = SolutionGrainFactory.GetGrain("Solution");
            IProjectCodeProviderGrain providerGrain = await solutionGrain.GetCodeProviderAsync(methodDescriptor);
            if (providerGrain == null)
            {
                var libraryMethodVisitor = new LibraryMethodProcessor(methodDescriptor);
                methodEntity = libraryMethodVisitor.ParseLibraryMethod();
            }
            else
            {
                methodEntity = (MethodEntity)await providerGrain.CreateMethodEntityAsync(methodDescriptor);
            }
            return methodEntity;
        }




		public async Task<IEntityProcessor> GetEntityWithProcessorAsync(IEntityDescriptor entityDesc)
		{
			Contract.Assert(entityDesc != null);
			var entity = (IMethodEntityGrain)await GetEntityAsync(entityDesc);
			Contract.Assert(entity != null);
            var methodEntity = (MethodEntity) await entity.GetMethodEntity();
			return new MethodEntityProcessor(methodEntity, this, entityDesc, true);
		}

		public void RegisterEntity(IEntityDescriptor entityDesc, IEntity entity)
		{
			var descriptor = (OrleansEntityDescriptor)entityDesc;
            this.entities.Add(descriptor.MethodDescriptor);
            //this.orleansGuids[descriptor.MethodDescriptor]= descriptor.Guid;
			//Contract.Assert(entity is MethodEntityGrain);
			//this.orleansGrains.Add(descriptor.GetGuid().Result, (MethodEntityGrain)entity);
		}
        //public Guid GetGuidForMethod(MethodDescriptor methodDescriptor)
        //{
        //    Guid result = Guid.Empty; 
        //    if (orleansGuids.TryGetValue(methodDescriptor, out result))
        //        return result;
        //    orleansGuids[methodDescriptor] = Guid.NewGuid();
        //    return orleansGuids[methodDescriptor];
        //}

        public IEntity GetEntity(IEntityDescriptor entityDesc)
        {
            throw new NotImplementedException();
        }

        public IEntityProcessor GetEntityWithProcessor(IEntityDescriptor entityDesc)
        {
            throw new NotImplementedException();
        }
    }
}