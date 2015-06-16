// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using ReachingTypeAnalysis.Analysis;
using ReachingTypeAnalysis.Roslyn;
using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Communication
{
    internal class OnDemandSyncDispatcher : SynchronousLocalDispatcher
    {
        public OnDemandSyncDispatcher(bool async = false)
            : base(async)
        {
        }
        /// <summary>
        /// This dispacher tries to find and load methods on the fly
        /// </summary>
        /// <param name="entityDesc"></param>
        /// <returns></returns>
        public async override Task<IEntity> GetEntityAsync(IEntityDescriptor entityDesc)
        {
            IEntity entity = await base.GetEntityAsync(entityDesc);
            if (entity == null)
            {
                MethodDescriptor methodDescriptor = GetMethodDescriptor(entityDesc);
                var methodEntity = await ProjectCodeProvider.FindProviderAndCreateMethodEntityAsync(methodDescriptor);
                base.RegisterEntity(methodEntity.EntityDescriptor, methodEntity);
                entity = methodEntity;
            }

            return entity;
        }

        /// <summary>
        /// This method should not exists. Because the IEntityDescriptor should have a MethdodDescriptor 
        /// and Orleans entities should not appear here
        /// </summary>
        /// <param name="entityDesc"></param>
        /// <returns></returns>
        private MethodDescriptor GetMethodDescriptor(IEntityDescriptor entityDesc)
        {
            if(entityDesc is MethodEntityDescriptor)
            {
                return ((MethodEntityDescriptor)entityDesc).MethodDescriptor;
            }
            if (entityDesc is OrleansEntityDescriptor)
            {
                var grainDesc = (OrleansEntityDescriptor)entityDesc;
    			Contract.Assert(grainDesc != null);
	
                return grainDesc.MethodDescriptor;
            }
            throw new NotImplementedException("We shouldn't reach this place");
        }
    }
}
