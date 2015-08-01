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

namespace ReachingTypeAnalysis.Analysis
{
    public interface IOrleansEntityState: IGrainState
    {
        MethodDescriptor MethodDescriptor { get; set; }
    }

    //[StorageProvider(ProviderName = "FileStore")]
    [StorageProvider(ProviderName = "MemoryStore")]
    //[Reentrant]
    internal class MethodEntityGrain : Grain<IOrleansEntityState>, IMethodEntityGrain
    {
        [NonSerialized]
        private IMethodEntityWithPropagator methodEntityPropagator;
        [NonSerialized]
        private MethodEntity methodEntity;
        [NonSerialized]
        private ICodeProvider codeProvider;
        [NonSerialized]
        private IProjectCodeProviderGrain codeProviderGrain;
        [NonSerialized]
        private ISolutionGrain solutionGrain; 

        public override async Task OnActivateAsync()
        {
            Logger.Log(this.GetLogger(),"MethodEntityGrain", "OnActivate", "Activation for {0} ", this.GetPrimaryKeyString());

            solutionGrain = GrainFactory.GetGrain<ISolutionGrain>("Solution");

            MethodDescriptor methodDescriptor = MethodDescriptor.DeMarsall(this.GetPrimaryKeyString());

	        // Shold not be null..
            if (this.State.Etag!= null)
            {
                methodDescriptor = this.State.MethodDescriptor;
            }

            this.State.MethodDescriptor = methodDescriptor;
            var methodDescriptorToSearch = methodDescriptor.BaseDescriptor;

            this.codeProviderGrain = await solutionGrain.GetCodeProviderAsync(methodDescriptorToSearch);
            this.codeProvider = new ProjectGrainWrapper(codeProviderGrain);

            this.methodEntity = (MethodEntity)await codeProviderGrain.CreateMethodEntityAsync(methodDescriptorToSearch);

            if (methodDescriptor.IsAnonymousDescriptor)
            {
                this.methodEntity = this.methodEntity.GetAnonymousMethodEntity((AnonymousMethodDescriptor) methodDescriptor);
            }

            await solutionGrain.AddInstantiatedTypes(this.methodEntity.InstantiatedTypes);

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


		public async Task SetMethodEntityAsync(IEntity methodEntity, MethodDescriptor methodDescriptor)
		{
			Contract.Assert(methodEntity != null);
			this.methodEntity = (MethodEntity)methodEntity;

			Contract.Assert(this.State != null);
			this.State.MethodDescriptor = methodDescriptor;

			codeProviderGrain = await solutionGrain.GetCodeProviderAsync(methodDescriptor);
			this.codeProvider = new ProjectGrainWrapper(codeProviderGrain);

			await solutionGrain.AddInstantiatedTypes(this.methodEntity.InstantiatedTypes);

            this.methodEntityPropagator = new MethodEntityWithPropagator(this.methodEntity, this.codeProvider);

			await this.WriteStateAsync();
		}

        public  Task<IEntity> GetMethodEntityAsync()
        {
            // Contract.Assert(this.methodEntity != null);
            return Task.FromResult<IEntity>(this.methodEntity);
        }

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
