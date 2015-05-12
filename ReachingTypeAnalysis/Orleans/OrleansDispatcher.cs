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
	internal class OrleansDispatcher : IDispatcher
	{
		private Dictionary<Guid, MethodEntityGrain> orleansGrains = new Dictionary<Guid, MethodEntityGrain>();

		public OrleansDispatcher()
		{

		}

		public void DeliverMessage(IEntityDescriptor destination, IMessage message)
		{
			throw new NotImplementedException();
		}

		public Task DeliverMessageAsync(IEntityDescriptor destination, IMessage message)
		{
			Contract.Assert(destination is IOrleansEntityDescriptor);
			var guid = ((IOrleansEntityDescriptor)destination).GetGuid().Result;
			var destinationGrain = GrainFactory.GetGrain<IMethodEntityGrain>(guid);
			return destinationGrain.ReceiveMessageAsync((IOrleansEntityDescriptor)message.Source, message);
			//var processor = destinationGrain.GetEntityProcessor(this);
			//return processor.ReceiveMessageAsync(message.Source, message);
		}

		public ImmutableHashSet<IEntity> GetAllEntites()
		{
			return this.orleansGrains.Values.ToImmutableHashSet<IEntity>();
		}

		public async Task<IEntity> GetEntity(IEntityDescriptor entityDesc)
		{
			Contract.Assert(entityDesc != null);
			var grainDesc = (OrleansEntityDescriptor)entityDesc;
			Contract.Assert(grainDesc != null);
			var guid = grainDesc.GetGuid().Result;
			MethodEntityGrain result;
			if (this.orleansGrains.TryGetValue(guid, out result))
			{
				Contract.Assert(result != null);

				return result;
			}
			else
			{
				// new grain
				Contract.Assert(grainDesc.MethodDescriptor != null);
				var provider = await CodeProvider.GetAsync(grainDesc.MethodDescriptor);
				Contract.Assert(provider != null);

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
		}

		public async Task<IEntityProcessor> GetEntityWithProcessorAsync(IEntityDescriptor entityDesc)
		{
			Contract.Assert(entityDesc != null);
			var entity = (MethodEntityGrain)await GetEntity(entityDesc);
			Contract.Assert(entity != null);

			return new MethodEntityProcessor(entity.GetMethodEntity(), this, true);
		}

		public void RegisterEntity(IEntityDescriptor entityDesc, IEntity entity)
		{
			var descriptor = (IOrleansEntityDescriptor)entityDesc;

			Contract.Assert(entity is MethodEntityGrain);
			this.orleansGrains.Add(descriptor.GetGuid().Result, (MethodEntityGrain)entity);
		}
	}
}