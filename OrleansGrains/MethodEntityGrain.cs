// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using OrleansInterfaces;
using ReachingTypeAnalysis.Communication;
using System.Diagnostics;
using CodeGraphModel;
using Orleans.Placement;

namespace ReachingTypeAnalysis.Analysis
{
    public interface IOrleansEntityState: IGrainState
    {
        MethodDescriptor MethodDescriptor { get; set; }
    }

    //[StorageProvider(ProviderName = "FileStore")]
    [StorageProvider(ProviderName = "MemoryStore")]
    //[Reentrant]
	[PreferLocalPlacement]
    public class MethodEntityGrain : Grain<IOrleansEntityState>, IMethodEntityGrain
    {
        [NonSerialized]
        private IMethodEntityWithPropagator methodEntityPropagator;
        [NonSerialized]
        private MethodEntity methodEntity;
        [NonSerialized]
        private IProjectCodeProvider codeProvider;
        //[NonSerialized]
        //private IProjectCodeProvider codeProviderGrain;
        [NonSerialized]
        private ISolutionGrain solutionGrain; 

        public override async Task OnActivateAsync()
        {
            Logger.Log(this.GetLogger(),"MethodEntityGrain", "OnActivate", "Activation for {0} ", this.GetPrimaryKeyString());

            solutionGrain = GrainFactory.GetGrain<ISolutionGrain>("Solution");

            var methodDescriptor = MethodDescriptor.DeMarsall(this.GetPrimaryKeyString());

	        // Shold not be null..
            if (this.State.Etag!= null)
            {
                methodDescriptor = this.State.MethodDescriptor;
            }

            this.State.MethodDescriptor = methodDescriptor;
            var methodDescriptorToSearch = methodDescriptor.BaseDescriptor;

			var codeProviderGrain = await solutionGrain.GetProjectCodeProviderAsync(methodDescriptorToSearch);
            
			// This wrapper caches some of the queries to codeProvider
			//this.codeProvider = new ProjectCodeProviderWithCache(codeProviderGrain);

			this.codeProvider = codeProviderGrain;

            this.methodEntity = (MethodEntity)await codeProvider.CreateMethodEntityAsync(methodDescriptorToSearch);

            if (methodDescriptor.IsAnonymousDescriptor)
            {
                this.methodEntity = this.methodEntity.GetAnonymousMethodEntity((AnonymousMethodDescriptor) methodDescriptor);
            }

			// this is for RTA analysis
            await solutionGrain.AddInstantiatedTypesAsync(this.methodEntity.InstantiatedTypes);

            // This take cares of doing the progation of types
            this.methodEntityPropagator = new MethodEntityWithPropagator(methodEntity, codeProvider);

            await this.WriteStateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            Logger.Log(this.GetLogger(), "MethodEntityGrain", "OnDeactivate", "Deactivation for {0} ", this.GetPrimaryKeyString());
            this.methodEntity = null;
            return TaskDone.Done;
        }

        public Task<ISet<MethodDescriptor>> GetCalleesAsync()
        {
            return this.methodEntityPropagator.GetCalleesAsync();
        }

        public Task<IDictionary<AnalysisCallNode, ISet<MethodDescriptor>>> GetCalleesInfoAsync()
        {
            return this.methodEntityPropagator.GetCalleesInfoAsync();
        }

		///// <summary>
		///// This method shoudld be removed
		///// </summary>
		///// <param name="methodEntity"></param>
		///// <param name="methodDescriptor"></param>
		///// <returns></returns>
		//public async Task SetMethodEntityAsync(IEntity methodEntity, MethodDescriptor methodDescriptor)
		//{
		//	Contract.Assert(methodEntity != null);
		//	this.methodEntity = (MethodEntity)methodEntity;

		//	Contract.Assert(this.State != null);
		//	this.State.MethodDescriptor = methodDescriptor;

		//	var codeProviderGrain = await solutionGrain.GetProjectCodeProviderAsync(methodDescriptor);
		//          // this.codeProvider = new ProjectCodeProviderWithCache(codeProviderGrain);
		//	this.codeProvider = codeProviderGrain;

		//	await solutionGrain.AddInstantiatedTypesAsync(this.methodEntity.InstantiatedTypes);

		//	this.methodEntityPropagator = new MethodEntityWithPropagator(this.methodEntity, this.codeProvider);

		//	await this.WriteStateAsync();
		//}

		//public  Task<IEntity> GetMethodEntityAsync()
		//{
		//    // Contract.Assert(this.methodEntity != null);
		//    return Task.FromResult<IEntity>(this.methodEntity);
		//}

		public async Task<PropagationEffects> PropagateAsync(PropagationKind propKind)
        {
            Logger.Log(this.GetLogger(), "MethodEntityGrain", "PropagateAsync", "Propagation for {0} ", this.methodEntity.MethodDescriptor);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var propagationEffects = await this.methodEntityPropagator.PropagateAsync(propKind);
            sw.Stop();
            Logger.Log(this.GetLogger(),"MethodEntityGrain", "PropagateAsync", "End Propagation for {0}. Time elapsed {1} ", this.methodEntity.MethodDescriptor,sw.Elapsed);
            //this.methodEntity.Save(@"C:\Temp\"+this.methodEntity.MethodDescriptor.MethodName + @".dot");
            return propagationEffects;
        }

        public  Task<PropagationEffects> PropagateAsync(CallMessageInfo callMessageInfo)
        {
            return this.methodEntityPropagator.PropagateAsync(callMessageInfo);
        }

        public  Task<PropagationEffects> PropagateAsync(ReturnMessageInfo returnMessageInfo)
        {
            return this.methodEntityPropagator.PropagateAsync(returnMessageInfo);
        }

        public Task<ISet<MethodDescriptor>> GetCalleesAsync(int invocationPosition)
        {
            return this.methodEntityPropagator.GetCalleesAsync(invocationPosition);
        }

        public Task<int> GetInvocationCountAsync()
        {
            return this.methodEntityPropagator.GetInvocationCountAsync();
        }

        public Task<bool> IsInitializedAsync()
        {
            return Task.FromResult(this.methodEntity != null);
        }

        //public Task<IEntityDescriptor> GetDescriptor()
        //{
        //    return Task.FromResult<IEntityDescriptor>(this.orleansEntityDescriptor);
        //}

        //public Task<MethodDescriptor> GetMethodDescriptor()
        //{
        //    return Task.FromResult<MethodDescriptor>(this.orleansEntityDescriptor.MethodDescriptor);
        //}

        //public Task SetDescriptor(IEntityDescriptor descriptor)
        //{
        //    var orleansEntityDescriptor = (MethodEntityDescriptor)descriptor;

        //    Contract.Assert(this.State != null);
        //    this.State.MethodDescriptor = orleansEntityDescriptor.MethodDescriptor;
        //    return State.WriteStateAsync();
        //}


        public Task<IEnumerable<TypeDescriptor>> GetInstantiatedTypesAsync()
        {
           return this.methodEntityPropagator.GetInstantiatedTypesAsync();
        }

		public Task<SymbolReference> GetDeclarationInfoAsync()
		{
			return this.methodEntityPropagator.GetDeclarationInfoAsync();
        }


        public Task<IEnumerable<SymbolReference>> GetCallersDeclarationInfoAsync()
        {
            return this.methodEntityPropagator.GetCallersDeclarationInfoAsync();
        }

		public Task<PropagationEffects> RemoveMethodAsync()
		{
			return this.methodEntityPropagator.RemoveMethodAsync();
		}

		public Task UnRegisterCaller(VariableNode lhs, MethodDescriptor caller, AnalysisCallNode callNode)
		{
			return this.methodEntityPropagator.UnRegisterCaller(lhs, caller, callNode);
		}


		public Task UnRegisterCallee(CallContext callContext)
		{
			return this.methodEntityPropagator.UnRegisterCallee(callContext);
		}
	}

	internal class ProjectCodeProviderWithCache : IProjectCodeProvider
	{
		private IProjectCodeProvider codeProvider;
		private IDictionary<TypeDescriptor,ISet<TypeDescriptor>> IsSubTypeReply = new Dictionary<TypeDescriptor,ISet<TypeDescriptor>>();
		private IDictionary<TypeDescriptor,ISet<TypeDescriptor>> IsSubTypeNegativeReply = new Dictionary<TypeDescriptor,ISet<TypeDescriptor>>();
		private IDictionary<Tuple<MethodDescriptor, TypeDescriptor>, MethodDescriptor> FindMethodReply = new Dictionary<Tuple<MethodDescriptor, TypeDescriptor>, MethodDescriptor>();

		internal ProjectCodeProviderWithCache(IProjectCodeProvider codeProvider)
		{
			this.codeProvider = codeProvider;
		}

		public async Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
		{
			if (IsSubTypeReply.ContainsKey(typeDescriptor1) 
					&& IsSubTypeReply[typeDescriptor1].Contains(typeDescriptor2))
				return true;
			if(IsSubTypeNegativeReply.ContainsKey(typeDescriptor1) 
				&& IsSubTypeNegativeReply[typeDescriptor1].Contains(typeDescriptor2))
				return false;

			var isSubType = await codeProvider.IsSubtypeAsync(typeDescriptor1, typeDescriptor2);
			if (isSubType)
			{
				AddToSubTypeCache(this.IsSubTypeReply, typeDescriptor1, typeDescriptor2);
			}
			else
			{
				AddToSubTypeCache(this.IsSubTypeNegativeReply, typeDescriptor1, typeDescriptor2);
			}

			return isSubType;
		}

		private void AddToSubTypeCache(IDictionary<TypeDescriptor,ISet<TypeDescriptor>> typeCache, 
					TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
		{
			ISet<TypeDescriptor> subTypes;
			if (typeCache.TryGetValue(typeDescriptor1, out subTypes))
			{
				subTypes.Add(typeDescriptor2);
			}
			else
			{
				subTypes = new HashSet<TypeDescriptor>();
				subTypes.Add(typeDescriptor2);
				typeCache[typeDescriptor1] = subTypes;
			}
		}

		public async Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
		{
			MethodDescriptor reply;
			var key = new Tuple<MethodDescriptor, TypeDescriptor>(methodDescriptor, typeDescriptor);
			if(FindMethodReply.TryGetValue(key, out reply))
			{
				return reply;
			}
			reply = await codeProvider.FindMethodImplementationAsync(methodDescriptor,typeDescriptor);
			FindMethodReply.Add(key, reply);
			return reply;
		}

		public Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			return codeProvider.CreateMethodEntityAsync(methodDescriptor);
		}

		public Task<IEnumerable<MethodDescriptor>> GetRootsAsync()
		{
			return codeProvider.GetRootsAsync();
		}

		public Task<IEnumerable<FileResponse>> GetDocumentsAsync()
		{
			return codeProvider.GetDocumentsAsync();
		}

		public Task<IEnumerable<FileResponse>> GetDocumentEntitiesAsync(string filePath)
		{
			return codeProvider.GetDocumentEntitiesAsync(filePath);
		}

		public Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			return codeProvider.GetMethodEntityAsync(methodDescriptor);
        }

		public Task<PropagationEffects> RemoveMethodAsync(MethodDescriptor methodToUpdate)
		{
			return this.codeProvider.RemoveMethodAsync(methodToUpdate);
		}
	}

    /// <summary>
    /// This wrapper was used when the IMethodEntity grain couldn't implement another interface
    /// This was solved with Orleans version 1.0.9, so this wrapper is no longer needed 
    /// </summary>
    //public class MethodEntityGrainWrapper : IMethodEntityWithPropagator
    //{
    //    IMethodEntityGrain grainRef;
    //    public MethodEntityGrainWrapper(IMethodEntityGrain grainRef)
    //    {
    //        this.grainRef = grainRef;
    //    }
    //    public Task<PropagationEffects> PropagateAsync(PropagationKind propKind)
    //    {
    //        return this.grainRef.PropagateAsync(propKind);
    //    }

    //    public Task<PropagationEffects> PropagateAsync(CallMessageInfo callMessageInfo)
    //    {
    //        return this.grainRef.PropagateAsync(callMessageInfo);
    //    }

    //    public Task<PropagationEffects> PropagateAsync(ReturnMessageInfo returnMessageInfo)
    //    {
    //        return this.grainRef.PropagateAsync(returnMessageInfo);
    //    }

    //    public Task<bool> IsInitializedAsync()
    //    {
    //        return this.grainRef.IsInitializedAsync();
    //    }

    //    public Task<IEntity> GetMethodEntityAsync()
    //    {
    //        return this.grainRef.GetMethodEntityAsync();
    //    }

    //    public Task<ISet<MethodDescriptor>> GetCalleesAsync()
    //    {
    //        return this.grainRef.GetCalleesAsync();
    //    }

    //    public Task<IDictionary<AnalysisCallNode, ISet<MethodDescriptor>>> GetCalleesInfoAsync()
    //    {
    //        return this.grainRef.GetCalleesInfoAsync();
    //    }

    //    public async Task<ISet<MethodDescriptor>> GetCalleesAsync(int invocationPosition)
    //    {
    //        return await this.grainRef.GetCalleesAsync(invocationPosition);
    //    }

    //    public Task<int> GetInvocationCountAsync()
    //    {
    //        return this.grainRef.GetInvocationCountAsync();
    //    }
    //}
}
