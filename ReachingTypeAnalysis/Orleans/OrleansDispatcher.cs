// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Orleans;
using OrleansGrains;
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
		//private Dictionary<Guid, MethodEntityGrain> orleansGrains = new Dictionary<Guid, MethodEntityGrain>();//
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
			//Contract.Assert(destination is IOrleansEntityDescriptor);
			//var guid = await ((OrleansEntityDescriptor)destination).GetGuid();
            var guid = ((OrleansEntityDescriptor)destination).Guid;
			var destinationGrain = MethodEntityGrainFactory.GetGrain(guid);
			/*return*/ 
            destinationGrain.ReceiveMessageAsync((OrleansEntityDescriptor)message.Source, message);
			//var processor = destinationGrain.GetEntityProcessor(this);
			//return processor.ReceiveMessageAsync(message.Source, message);
		}

		public ImmutableHashSet<IEntity> GetAllEntites()
		{
            //return this.orleansGrains.Values.ToImmutableHashSet<IEntity>();
            throw new NotImplementedException();
		}

        public async Task<IEntity> GetEntityAsync(IEntityDescriptor entityDesc)
        {
            //Contract.Assert(entityDesc != null);
            var grainDesc = (OrleansEntityDescriptor)entityDesc;
            //Contract.Assert(grainDesc != null);
            //var guid = await grainDesc.GetGuid();
            var guid = ((OrleansEntityDescriptor)grainDesc).Guid;

            var result = MethodEntityGrainFactory.GetGrain(guid);
            // check if the result is initialized

            // check if result is initialized

            
            //Contract.Assert(grainDesc.MethodDescriptor != null);
            var provider = await CodeProvider.GetAsync(grainDesc.MethodDescriptor);
            //Contract.Assert(provider != null);

            var methodEntityGenerator = new MethodSyntaxProcessor(provider, grainDesc.MethodDescriptor, this);
            return methodEntityGenerator.ParseMethod();


            //          var node = Utils.FindMethodDeclaration(grainDesc.Symbol, out symbol);
            //          //var node = Utils.FindMethodImplementation(ed.Method.RoslynMethod);
            //          MethodEntityGrain<ANode,AType,AMethod> methodEntity;
            //          if (node != null)
            //          {
            //              var sm = Utils.SearchSemanticModelForMethods(this.DeliverMessage, node);
            //              if (sm != null)
            //              {
            //                  var methodEntityGenerator = new MethodSyntaxProcessor(symbol, sm, this);
            //methodEntity = (MethodEntityGrain<ANode, AType, AMethod>)methodEntityGenerator.ParseMethod();
            //                  //base.RegisterEntity(entityDesc, e);
            //              }
            //              else
            //              {
            //                  throw new ArgumentException("Cannot find a method for " + entityDesc);
            //              }
            //          }

            //else
            //{
            //	var libraryMethodVisitor = new LibraryMethodProcessor(symbol, this);
            //	var methodEntity = (MethodEntityGrain<ANode, AType, AMethod>)libraryMethodVisitor.ParseLibraryMethod();
            //	return methodEntity;
            //	//this.RegisterEntity(entityDesc, entity);
            //}

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

			//var descriptor = (IOrleansEntityDescriptor)entityDesc;

			//Contract.Assert(entity is MethodEntityGrain);
			//this.orleansGrains.Add(descriptor.GetGuid().Result, (MethodEntityGrain)entity);
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