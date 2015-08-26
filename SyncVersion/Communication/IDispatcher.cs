using ReachingTypeAnalysis.Communication;
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis
{
    public interface IDispatcher
    {
        ImmutableHashSet<IEntity> GetAllEntites();
        ImmutableHashSet<IEntityDescriptor> GetAllEntitiesDescriptors();
        void DeliverMessage(IEntityDescriptor destination, IMessage message);
        //Task<IEntity> GetEntityAsync(IEntityDescriptor entityDesc);
        //IEntity GetEntity(IEntityDescriptor entityDesc);
        void RegisterEntity(IEntityDescriptor entityDesc, IEntity entity);
        IEntityProcessor GetEntityWithProcessor(IEntityDescriptor entityDesc);
    }

	public abstract class Dispatcher : IDispatcher
    {
		private IDictionary<IEntityDescriptor, IEntity> entityMapping;
        public bool IsAsync { get; protected set; }

        public Dispatcher(bool async)
        {
            this.IsAsync = async;
			this.entityMapping = new Dictionary<IEntityDescriptor, IEntity>();
        }

        public void RegisterEntity(IEntityDescriptor entityDesc, IEntity entity)
        {
            entityMapping[entityDesc] = entity;
        }
        public virtual IEntity GetEntity(IEntityDescriptor entityDesc)
        {
            IEntity entity;
            entityMapping.TryGetValue(entityDesc, out entity);
            return entity;
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

        public ImmutableHashSet<IEntityDescriptor> GetAllEntitiesDescriptors()
        {
            return entityMapping.Keys.ToImmutableHashSet();
        }

        public abstract void DeliverMessage(IEntityDescriptor destination, IMessage message);
    }
}
