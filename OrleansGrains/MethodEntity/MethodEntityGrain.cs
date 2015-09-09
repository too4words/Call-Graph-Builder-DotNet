﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

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
using Orleans.Runtime;
using Orleans.Core;

namespace ReachingTypeAnalysis.Analysis
{
    public interface IOrleansEntityState : IGrainState
    {
        MethodDescriptor MethodDescriptor { get; set; }
    }

    //[StorageProvider(ProviderName = "FileStore")]
    //[StorageProvider(ProviderName = "MemoryStore")]
	[StorageProvider(ProviderName = "AzureStore")]
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
        [NonSerialized]
        private ISolutionGrain solutionGrain;
		[NonSerialized]
		private long messages = 0;
		
		public override async Task OnActivateAsync()
        {
			//await StatsHelper.RegisterMsg("OnActivate", this.GrainFactory);

			Logger.OrleansLogger = this.GetLogger();
            Logger.LogVerbose(this.GetLogger(),"MethodEntityGrain", "OnActivate", "Activation for {0} ", this.GetPrimaryKeyString());

			var methodDescriptor = MethodDescriptor.DeMarsall(this.GetPrimaryKeyString());

			// Shold not be null..
			if (this.State.Etag != null)
			{
                if (this.State.MethodDescriptor != null && !this.State.MethodDescriptor.Name.Equals("."))
                {
                    methodDescriptor = this.State.MethodDescriptor;
                }
			}

			await CreateMethodEntityAsync(methodDescriptor);            
        }

		public async Task ForceDeactivationAsync()
		{
			//await StatsHelper.RegisterMsg("ForceDeactivation", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "MethodEntityGrain", "ForceDeactivation", "force for {0} ", this.GetPrimaryKeyString());
            //await this.ClearStateAsync();

            //this.State.Etag = null;
            this.State.MethodDescriptor = null;
            await this.WriteStateAsync();

			//await this.CreateMethodEntityAsync(methodDescriptor);

			this.DeactivateOnIdle();
		}

		public override Task OnDeactivateAsync()
		{
			//await StatsHelper.RegisterMsg("OnDeactivate", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "MethodEntityGrain", "OnDeactivate", "Deactivation for {0} ", this.GetPrimaryKeyString());

			this.methodEntity = null;
			return TaskDone.Done;
		}

		private async Task CreateMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			// This is a private method. We must not register this as a grain callee
			// await StatsHelper.RegisterMsg("CreateMethodEntity", this.GrainFactory);

			solutionGrain = OrleansSolutionManager.GetSolutionGrain(this.GrainFactory);

            this.State.MethodDescriptor = methodDescriptor;
            var methodDescriptorToSearch = methodDescriptor.BaseDescriptor;

			var codeProviderGrain = await solutionGrain.GetProjectCodeProviderAsync(methodDescriptorToSearch);
            
			// This wrapper caches some of the queries to codeProvider
			//this.codeProvider = new ProjectCodeProviderWithCache(codeProviderGrain);

			this.codeProvider = codeProviderGrain;

			Logger.LogVerbose(this.GetLogger(), "MethodEntityGrain", "CreateMethodEntity", "{0} calls to proivder {1}", methodDescriptor, this.codeProvider);
			Stopwatch sw = new Stopwatch();
			sw.Start();

            this.methodEntity = (MethodEntity)await codeProvider.CreateMethodEntityAsync(methodDescriptorToSearch);

            sw.Stop();

            Logger.LogInfo(this.GetLogger(), "MethodEntityGrain", "CreateMethodEntity", "{0};call to provider;{1};ms;{2};ticks", methodDescriptor, sw.ElapsedMilliseconds,sw.ElapsedTicks);

            if (methodDescriptor.IsAnonymousDescriptor)
            {
                this.methodEntity = this.methodEntity.GetAnonymousMethodEntity((AnonymousMethodDescriptor) methodDescriptor);
            }

			//// this is for RTA analysis
			//await solutionGrain.AddInstantiatedTypesAsync(this.methodEntity.InstantiatedTypes);

			// This take cares of doing the progation of types
			this.methodEntityPropagator = new MethodEntityWithPropagator(methodEntity, codeProvider);

            await this.WriteStateAsync();
		}

        public Task<ISet<MethodDescriptor>> GetCalleesAsync()
        {
			StatsHelper.RegisterMsg("GetCallees", this.GrainFactory);

			return this.methodEntityPropagator.GetCalleesAsync();
        }

        public Task<IDictionary<AnalysisCallNode, ISet<MethodDescriptor>>> GetCalleesInfoAsync()
        {
			StatsHelper.RegisterMsg("GetCalleesInfo", this.GrainFactory);

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
		public Task<PropagationEffects> PropagateAsync(PropagationKind propKind, IEnumerable<PropGraphNodeDescriptor> reWorkSet)
		{
			StatsHelper.RegisterMsg("Propagate", this.GrainFactory);

			return this.methodEntityPropagator.PropagateAsync(propKind, reWorkSet);
		}

		public async Task<PropagationEffects> PropagateAsync(PropagationKind propKind)
        {
			await StatsHelper.RegisterMsg("Propagate", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "MethodEntityGrain", "Propagate", "Propagation for {0} ", this.methodEntity.MethodDescriptor);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var propagationEffects = await this.methodEntityPropagator.PropagateAsync(propKind);
            sw.Stop();

            Logger.LogInfo(this.GetLogger(),"MethodEntityGrain", "Propagate", "End Propagation for {0}. Time elapsed {1} ", this.methodEntity.MethodDescriptor,sw.Elapsed);
            return propagationEffects;
        }

        public  Task<PropagationEffects> PropagateAsync(CallMessageInfo callMessageInfo)
        {
			StatsHelper.RegisterMsg("Propagate", this.GrainFactory);

			return this.methodEntityPropagator.PropagateAsync(callMessageInfo);
        }

        public  Task<PropagationEffects> PropagateAsync(ReturnMessageInfo returnMessageInfo)
        {
			StatsHelper.RegisterMsg("Propagate", this.GrainFactory);

			return this.methodEntityPropagator.PropagateAsync(returnMessageInfo);
        }

        public Task<ISet<MethodDescriptor>> GetCalleesAsync(int invocationPosition)
        {
			StatsHelper.RegisterMsg("GetCallees", this.GrainFactory);

			return this.methodEntityPropagator.GetCalleesAsync(invocationPosition);
        }

        public Task<int> GetInvocationCountAsync()
        {
			StatsHelper.RegisterMsg("GetInvocationCount", this.GrainFactory);

			return this.methodEntityPropagator.GetInvocationCountAsync();
        }

        public Task<bool> IsInitializedAsync()
        {
			StatsHelper.RegisterMsg("IsInitialized", this.GrainFactory);

			return Task.FromResult(this.methodEntity != null);
        }
           
        public Task<IEnumerable<TypeDescriptor>> GetInstantiatedTypesAsync()
        {
			StatsHelper.RegisterMsg("GetInstantiatedTypes", this.GrainFactory);

			return this.methodEntityPropagator.GetInstantiatedTypesAsync();
        }

		public Task<SymbolReference> GetDeclarationInfoAsync()
		{
			StatsHelper.RegisterMsg("GetDeclarationInfo", this.GrainFactory);

			return this.methodEntityPropagator.GetDeclarationInfoAsync();
        }

		public Task<IEnumerable<Annotation>> GetAnnotationsAsync()
		{
			StatsHelper.RegisterMsg("GetAnnotations", this.GrainFactory);

			return this.methodEntityPropagator.GetAnnotationsAsync();
		}

        public Task<IEnumerable<SymbolReference>> GetCallersDeclarationInfoAsync()
        {
			StatsHelper.RegisterMsg("GetCallersDeclarationInfo", this.GrainFactory);

			return this.methodEntityPropagator.GetCallersDeclarationInfoAsync();
        }

		public Task<PropagationEffects> RemoveMethodAsync()
		{
			StatsHelper.RegisterMsg("RemoveMethod", this.GrainFactory);

			return this.methodEntityPropagator.RemoveMethodAsync();
		}

		public Task UnregisterCallerAsync(CallContext callContext)
		{
			StatsHelper.RegisterMsg("UnregisterCaller", this.GrainFactory);

			return this.methodEntityPropagator.UnregisterCallerAsync(callContext);
		}

		//public Task UnregisterCalleeAsync(CallContext callContext)
		//{
		//	StatsHelper.RegisterMsg("UnregisterCallee", this.GrainFactory);
		//
		//	return this.methodEntityPropagator.UnregisterCalleeAsync(callContext);
		//}

		public Task<IEnumerable<CallContext>> GetCallersAsync()
		{
			StatsHelper.RegisterMsg("GetCallers", this.GrainFactory);

			return this.methodEntityPropagator.GetCallersAsync();
		}
	}
}