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
			//Contract.Assert(destination is IOrleansEntityDescriptor);
			//var guid = await ((OrleansEntityDescriptor)destination).GetGuid();
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
        public async Task<IEntity> GetMethodEntityAsync(IEntityDescriptor entityDesc)
        {
            var grainDesc = (OrleansEntityDescriptor)entityDesc;
            //Contract.Assert(grainDesc != null);
            var methodDescriptor = grainDesc.MethodDescriptor;

            //var guid = ((OrleansEntityDescriptor)grainDesc).Guid;
            //var result = MethodEntityGrainFactory.GetGrain(guid);
            //// check if the result is initialized
            //var methodEntity = await result.GetMethodEntity();
            //if (methodEntity != null)
            //{
                Contract.Assert(methodDescriptor != null);
                var pair = await ProjectCodeProvider.GetAsync(methodDescriptor);
                var provider = pair.Item1;
                var tree = pair.Item2;
                var model = provider.Compilation.GetSemanticModel(tree);
                Contract.Assert(provider != null);

                var methodEntityGenerator = new MethodSyntaxProcessor(model, provider, tree, methodDescriptor, this);
                var methodEntityGrain = (IMethodEntityGrain)methodEntityGenerator.ParseMethod();
                return await methodEntityGrain.GetMethodEntity();
            //}
            //else
            //{
            //    return result;
            //}
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
            var methodEntity = await result.GetMethodEntity();
            if (methodEntity == null)
            {
                //Contract.Assert(grainDesc.MethodDescriptor != null);
                var pair = await ProjectCodeProvider.GetAsync(grainDesc.MethodDescriptor);
                var provider = pair.Item1;
                var tree = pair.Item2;
                var model = provider.Compilation.GetSemanticModel(tree);
                //Contract.Assert(provider != null);
                if (provider != null)
                {
                    var methodEntityGenerator = new MethodSyntaxProcessor(model, provider, tree, grainDesc.MethodDescriptor, this);
                    return methodEntityGenerator.ParseMethod();
                }
                else 
                {
                    var libraryMethodVisitor = new LibraryMethodProcessor(grainDesc.MethodDescriptor, this);
                    return libraryMethodVisitor.ParseLibraryMethod();
                }
            }
            else
            {
                return result;
            }

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
            return Guid.NewGuid();
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