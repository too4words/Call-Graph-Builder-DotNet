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
	internal class OrleansDispatcher : IDispatcher
	{
        // Keeps a mapping between method descriptors and orleans entities Guids
        // Should be necessary if we are able to build the OrleansDescriptors properly
        private Dictionary<MethodDescriptor, Guid> orleansGuids = new Dictionary<MethodDescriptor, Guid>();//
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
            destinationGrain.ReceiveMessageAsync((OrleansEntityDescriptor)message.Source, message);
			//var processor = destinationGrain.GetEntityProcessor(this);
			//return processor.ReceiveMessageAsync(message.Source, message);
		}

        /// <summary>
        ///  
        /// </summary>
        /// <returns></returns>
		public ImmutableHashSet<IEntity> GetAllEntites()
		{
            var result = new HashSet<IEntity>();
            foreach(var methodDescriptor in this.orleansGuids.Keys.ToImmutableList())
            {
                var guid = orleansGuids[methodDescriptor];
                var orleansEnitityDesc = new OrleansEntityDescriptor(methodDescriptor,guid);
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
            var methodDescriptor = grainDesc.MethodDescriptor;

            Contract.Assert(methodDescriptor != null);
            return await CreateMethodEntityAsync(grainDesc);
        }

        public async Task<IEntity> GetEntityAsync(IEntityDescriptor entityDesc)
        {
            Contract.Assert(entityDesc != null);
            var grainDesc = (OrleansEntityDescriptor)entityDesc;
            Contract.Assert(grainDesc != null);

            var guid = ((OrleansEntityDescriptor)grainDesc).Guid;
            var methodEntityGrain = MethodEntityGrainFactory.GetGrain(guid);
            // check if the result is initialized
            var methodEntity = await methodEntityGrain.GetMethodEntity();
            if (methodEntity == null)
            {
                Contract.Assert(grainDesc.MethodDescriptor != null);
                methodEntity = await CreateMethodEntityAsync(grainDesc);
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


        private async Task<MethodEntity> CreateMethodEntityAsync(OrleansEntityDescriptor descriptor)
        {
           var pair = await ProjectCodeProvider.GetAsync(descriptor.MethodDescriptor);
           MethodEntity methodEntity = null;

            if (pair != null)
            {
                var provider = pair.Item1;
                var tree = pair.Item2;
                var model = provider.Compilation.GetSemanticModel(tree);
                var methodEntityGenerator = new MethodSyntaxProcessor(model, provider, tree, descriptor.MethodDescriptor, this);
                methodEntity = methodEntityGenerator.ParseMethod();
            }
            else
            {
                var libraryMethodVisitor = new LibraryMethodProcessor(descriptor.MethodDescriptor, this);
                methodEntity = libraryMethodVisitor.ParseLibraryMethod();
            }
            return methodEntity;
        }

		public async Task<IEntityProcessor> GetEntityWithProcessorAsync(IEntityDescriptor entityDesc)
		{
			Contract.Assert(entityDesc != null);
			var entity = (IMethodEntityGrain)await GetEntityAsync(entityDesc);
			Contract.Assert(entity != null);
            var methodEntity = (MethodEntity) await entity.GetMethodEntity();
			return new MethodEntityProcessor(methodEntity, this, true);
		}

		public void RegisterEntity(IEntityDescriptor entityDesc, IEntity entity)
		{
			var descriptor = (OrleansEntityDescriptor)entityDesc;
            this.orleansGuids[descriptor.MethodDescriptor]= descriptor.Guid;
			//Contract.Assert(entity is MethodEntityGrain);
			//this.orleansGrains.Add(descriptor.GetGuid().Result, (MethodEntityGrain)entity);
		}
        public Guid GetGuidForMethod(MethodDescriptor methodDescriptor)
        {
            Guid result = Guid.Empty; 
            if (orleansGuids.TryGetValue(methodDescriptor, out result))
                return result;
            orleansGuids[methodDescriptor] = Guid.NewGuid();
            return orleansGuids[methodDescriptor];
        }

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