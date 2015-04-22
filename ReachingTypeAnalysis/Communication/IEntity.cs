// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
namespace ReachingTypeAnalysis.Communication
{
    public abstract class Entity : IEntity
    {
        public abstract IEntityProcessor GetEntityProcessor(IDispatcher dispacther);       
    }
}
