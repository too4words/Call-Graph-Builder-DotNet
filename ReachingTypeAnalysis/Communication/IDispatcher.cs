// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis
{
	public abstract class Dispatcher : IDispatcher
    {
        public bool Async { get; protected set; }

        public Dispatcher(bool async)
        {
            this.Async = async;
        }

        private IDictionary<IEntityDescriptor, IEntity> entityMapping = new Dictionary<IEntityDescriptor, IEntity>();

        public void RegisterEntity(IEntityDescriptor entityDesc, IEntity entity)
        {
            entityMapping[entityDesc] = entity;
        }
              
        public virtual Task<IEntity> GetEntityAsync(IEntityDescriptor entityDesc)
        {
            IEntity entity;
            entityMapping.TryGetValue(entityDesc, out entity);

			return Task.FromResult(entity);
        }
        public virtual IEntity GetEntity(IEntityDescriptor entityDesc)
        {
            IEntity entity;
            entityMapping.TryGetValue(entityDesc, out entity);
            return entity;
        }


        public async virtual Task<IEntityProcessor> GetEntityWithProcessorAsync(IEntityDescriptor entityDesc)
        {
            var entity = (Analysis.MethodEntity)await GetEntityAsync(entityDesc);
            if (entity != null)
            {
                return new ReachingTypeAnalysis.Analysis.MethodEntityProcessor(entity, this);
                //return entity.GetEntityProcessor(this);
            }
            return null;
        }
        public  virtual IEntityProcessor GetEntityWithProcessor(IEntityDescriptor entityDesc)
        {
            var entity = (Analysis.MethodEntity)GetEntity(entityDesc);
            if (entity != null)
            {
                return new ReachingTypeAnalysis.Analysis.MethodEntityProcessor(entity, this);
                //return entity.GetEntityProcessor(this);
            }
            return null;
        }

        public ImmutableHashSet<IEntity> GetAllEntites()
        {
            return entityMapping.Values.ToImmutableHashSet();
        }

        public abstract void DeliverMessage(IEntityDescriptor destination, IMessage message);
        public abstract Task DeliverMessageAsync(IEntityDescriptor destination, IMessage message);
    }
}
