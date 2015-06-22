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
            return oed != null && this.MethodDescriptor.Equals(oed.MethodDescriptor);
        }
        public override int GetHashCode()
        {
            return this.MethodDescriptor.GetHashCode();
        }
    }

    [Serializable]
	internal class OrleansDispatcher : IDispatcher
	{
		private IEntityDescriptor self;
		private IEntity entity;
		public OrleansDispatcher()
		{
		}
        /// <summary>
        /// We use this constructor in the case we use one dispatcher per grain
        /// </summary>
        /// <param name="self"></param>
        /// <param name="entity"></param>
		public OrleansDispatcher(IEntityDescriptor self, IEntity entity)
		{
			this.self = self;
			this.entity = entity;
		}

		public void DeliverMessage(IEntityDescriptor destination, IMessage message)
		{
			throw new NotImplementedException();
		}

		public async Task DeliverMessageAsync(IEntityDescriptor destination, IMessage message)
		{
            // Option 1: Using directly the grain
            var destinationEntity = await GetEntityAsync(destination);
            var destinationGrain = (IMethodEntityGrain)destinationEntity;
            await destinationGrain.ProcessMessaggeAsync(message.Source, message);

            // Option 2: Using a MethodProcessor created by the grain.
            // This option requires the processor to be serializable
            //var methodEntityProcessor = (MethodEntityProcessor)await GetEntityWithProcessorAsync(destination);
            //await methodEntityProcessor.ProcessMessageAsync(message.Source, message);

            // await ReceiveMessageAsync((OrleansEntityDescriptor)message.Source, message,destinationGrain);
		}

        //public async Task ReceiveMessageAsync(IEntityDescriptor source, 
        //                                IMessage message,
        //                                IMethodEntityGrain destinationGrain)
        //{        
        //    //await destinationGrain.ProcessMessaggeAsync(source, message,this);
        //}


        /// <summary>
        ///  
        /// </summary>
        /// <returns></returns>
		public ImmutableHashSet<IEntity> GetAllEntites()
		{
			var result = new HashSet<IEntity>();
			var solutionGrain = SolutionGrainFactory.GetGrain("Solution");
			var methodDescriptors = solutionGrain.GetMethodDescriptors().Result;

			foreach (var methodDescriptor in methodDescriptors)
			{
				var orleansEnitityDesc = new OrleansEntityDescriptor(methodDescriptor);
				IMethodEntityGrain entity = (IMethodEntityGrain)GetEntityAsync(orleansEnitityDesc).Result;
				result.Add(entity);
			}

			return result.ToImmutableHashSet<IEntity>();
		}

        public ImmutableHashSet<IEntityDescriptor> GetAllEntitiesDescriptors()
        {
            var result = new HashSet<IEntityDescriptor>();
			var solutionGrain = SolutionGrainFactory.GetGrain("Solution");
			var methodDescriptors = solutionGrain.GetMethodDescriptors().Result;

			foreach (var methodDescriptor in methodDescriptors)
            {
                var orleansEnitityDesc = new OrleansEntityDescriptor(methodDescriptor);
                result.Add(orleansEnitityDesc);
            }

            return result.ToImmutableHashSet<IEntityDescriptor>();
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
			if(entityDesc.Equals(self))
			{
				return entity;
			}
            Contract.Assert(entityDesc != null);
            var grainDesc = (OrleansEntityDescriptor)entityDesc;
            Contract.Assert(grainDesc != null);

            //var guid = ((OrleansEntityDescriptor)grainDesc).Guid;
			return await CreateMethodEntityGrain(grainDesc);
        }

		internal static async Task<IMethodEntityGrain> CreateMethodEntityGrain(OrleansEntityDescriptor grainDesc)
		{
			var methodEntityGrain = MethodEntityGrainFactory.GetGrain(grainDesc.MethodDescriptor.ToString());
			// check if the result is initialized
			var methodEntity = await methodEntityGrain.GetMethodEntity();
			if (methodEntity == null)
			{
				Contract.Assert(grainDesc.MethodDescriptor != null);
				////  methodEntity = await providerGrain.CreateMethodEntityAsync(grainDesc.MethodDescriptor);
				methodEntity = await CreateMethodEntityUsingGrainsAsync(grainDesc.MethodDescriptor);
				Contract.Assert(methodEntity != null);
				await methodEntityGrain.SetMethodEntity(methodEntity, grainDesc);
				await methodEntityGrain.SetDescriptor(grainDesc);
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
            return await entity.GetEntityWithProcessorAsync();
            //Contract.Assert(entity != null);
            //var methodEntity = (MethodEntity) await entity.GetMethodEntity();
            //var codeProvider = await ProjectGrainWrapper.CreateProjectGrainWrapperAsync(methodEntity.MethodDescriptor);
            //return new MethodEntityProcessor(methodEntity, this, codeProvider, entityDesc, true);
		}

		public void RegisterEntity(IEntityDescriptor entityDesc, IEntity entity)
		{
			//var descriptor = (OrleansEntityDescriptor)entityDesc;
			//this.entities.Add(descriptor.MethodDescriptor);
 		}
        public IEntity GetEntity(IEntityDescriptor entityDesc)
        {
            throw new NotImplementedException();
        }

        public IEntityProcessor GetEntityWithProcessor(IEntityDescriptor entityDesc)
        {
            //throw new NotImplementedException();
            return GetEntityWithProcessorAsync(entityDesc).Result;
        }
    }
}