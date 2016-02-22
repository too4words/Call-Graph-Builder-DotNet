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
using Orleans.Runtime;
using Orleans.Core;
using Orleans.Concurrency;

namespace ReachingTypeAnalysis.Analysis
{
	//public interface IMethodState : IGrainState
	public class MethodState
	{
		public MethodDescriptor MethodDescriptor { get; set; }
    }

    //[StorageProvider(ProviderName = "FileStore")]
    //[StorageProvider(ProviderName = "MemoryStore")]
	//[StorageProvider(ProviderName = "AzureStore")]
    //[Reentrant]
	[PreferLocalPlacement]
	//public class MethodEntityGrain : Grain<IMethodState>, IMethodEntityGrain
	public class MethodEntityGrain : Grain, IMethodEntityGrain
	{
		//private const int WAIT_TIME = 200;

		[NonSerialized]
        private IMethodEntityWithPropagator methodEntityPropagator;
        [NonSerialized]
        private MethodEntity methodEntity;
        [NonSerialized]
        private IProjectCodeProvider codeProvider;
		[NonSerialized]
		private ISolutionGrain solutionGrain;
        	//[NonSerialized]
		//private EntityGrainStatus status;

		private MethodState State;

		private Task WriteStateAsync()
		{
			return TaskDone.Done;
		}

		private Task ClearStateAsync()
		{
			return TaskDone.Done;
		}

		public override async Task OnActivateAsync()
        {
			this.State = new MethodState();

			await StatsHelper.RegisterActivation("MethodEntityGrain", this.GrainFactory);

			Logger.OrleansLogger = this.GetLogger();
            Logger.LogVerbose(this.GetLogger(),"MethodEntityGrain", "OnActivate", "Activation for {0} ", this.GetPrimaryKeyString());

			var methodDescriptor = MethodDescriptor.DeMarsall(this.GetPrimaryKeyString());

			//if (this.State.MethodDescriptor != null && !this.State.MethodDescriptor.Name.Equals("."))
			//{
			//    methodDescriptor = this.State.MethodDescriptor;
			//}

			//Task.Run(async () =>
			//await Task.Factory.StartNew(async () =>
			//{
				try
				{
					//this.status = EntityGrainStatus.Busy;

					await this.CreateMethodEntityAsync(methodDescriptor);

					//this.status = EntityGrainStatus.Ready;
				}
				catch (Exception ex)
				{
					var inner = ex;
					while (inner is AggregateException) inner = inner.InnerException;

					Logger.LogError(this.GetLogger(), "MethodEntityGrain", "OnActivate", "Error:\n{0}\nInner:\n{1}", ex, inner);
					throw ex;
				}
			//});
        }

		public async Task ForceDeactivationAsync()
		{
			//await StatsHelper.RegisterMsg("MethodEntityGrain::ForceDeactivation", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "MethodEntityGrain", "ForceDeactivation", "force for {0} ", this.GetPrimaryKeyString());
            await this.ClearStateAsync();
        
			//this.State.MethodDescriptor = null;
			//await this.WriteStateAsync();

			this.DeactivateOnIdle();
		}

		public override Task OnDeactivateAsync()
		{
			StatsHelper.RegisterDeactivation("MethodEntityGrain", this.GrainFactory);

			Logger.LogWarning(this.GetLogger(), "MethodEntityGrain", "OnDeactivate", "Deactivation for {0} ", this.GetPrimaryKeyString());

			this.methodEntity = null;
			return TaskDone.Done;
		}

		private async Task CreateMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			// This is a private method. We must not register this as a grain callee
			// await StatsHelper.RegisterMsg("MethodEntityGrain::CreateMethodEntity", this.GrainFactory);

			this.solutionGrain = OrleansSolutionManager.GetSolutionGrain(this.GrainFactory);

            this.State.MethodDescriptor = methodDescriptor;
            var methodDescriptorToSearch = methodDescriptor.BaseDescriptor;

			var codeProviderGrain = await solutionGrain.GetProjectCodeProviderAsync(methodDescriptorToSearch);
            
			// This wrapper caches some of the queries to codeProvider
			this.codeProvider = new ProjectCodeProviderWithCache(codeProviderGrain);

			//this.codeProvider = codeProviderGrain;

			//Logger.LogWarning(this.GetLogger(), "MethodEntityGrain", "CreateMethodEntity", "{0} calls to proivder {1}", methodDescriptor, this.codeProvider);
			var sw = new Stopwatch();
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
			var orchestrator = OrleansOrchestratorManager.GetOrchestratorGrain(this.GrainFactory);
			this.methodEntityPropagator = new MethodEntityWithPropagator(methodEntity, codeProvider, orchestrator);

            await this.WriteStateAsync();

			//Logger.LogWarning(this.GetLogger(), "MethodEntityGrain", "CreateMethodEntity", "Exit {0}", methodDescriptor);
		}

        public Task<ISet<MethodDescriptor>> GetCalleesAsync()
        {
			StatsHelper.RegisterMsg("MethodEntityGrain::GetCallees", this.GrainFactory);

			return this.methodEntityPropagator.GetCalleesAsync();
        }

        public Task<IDictionary<AnalysisCallNode, ISet<MethodDescriptor>>> GetCalleesInfoAsync()
        {
			StatsHelper.RegisterMsg("MethodEntityGrain::GetCalleesInfo", this.GrainFactory);

			return this.methodEntityPropagator.GetCalleesInfoAsync();
        }

		#region Static Silo Orchestrator

		public async Task PropagateAndProcessAsync(PropagationKind propKind, IEnumerable<PropGraphNodeDescriptor> reWorkSet)
        {
            await StatsHelper.RegisterMsg("MethodEntityGrain::PropagateAndProcess", this.GrainFactory);
            var effects = await this.methodEntityPropagator.PropagateAsync(propKind, reWorkSet); // await this.PropagateAsync(propKind, reWorkSet);
            await StatsHelper.RegisterProgagationUpdates(effects.NumberOfUpdates, effects.WorkListInitialSize, this.GrainFactory);

            await ProcessEffects(effects);
            return;
        }

        public async Task PropagateAndProcessAsync(PropagationKind propKind)
        {
            await StatsHelper.RegisterMsg("MethodEntityGrain::PropagateAndProcess", this.GrainFactory);
            var effects = await this.methodEntityPropagator.PropagateAsync(propKind);
            await StatsHelper.RegisterProgagationUpdates(effects.NumberOfUpdates, effects.WorkListInitialSize, this.GrainFactory);

            // await this.PropagateAsync(propKind);
            await ProcessEffects(effects);
            return;
        }

		public async Task PropagateAndProcessAsync(CallMessageInfo callMessageInfo)
		{
			await StatsHelper.RegisterMsg("MethodEntityGrain::PropagateAndProcess", this.GrainFactory);
			var effects = await this.methodEntityPropagator.PropagateAsync(callMessageInfo); // await this.PropagateAsync(callMessageInfo);
			await StatsHelper.RegisterProgagationUpdates(effects.NumberOfUpdates, effects.WorkListInitialSize, this.GrainFactory);

			await ProcessEffects(effects);
			return;
		}

		public async Task PropagateAndProcessAsync(ReturnMessageInfo returnMessageInfo)
		{
			await StatsHelper.RegisterMsg("MethodEntityGrain::PropagateAndProcess", this.GrainFactory);
			var effects = await this.methodEntityPropagator.PropagateAsync(returnMessageInfo); //await this.PropagateAsync(returnMessageInfo);
			await StatsHelper.RegisterProgagationUpdates(effects.NumberOfUpdates, effects.WorkListInitialSize ,this.GrainFactory);

			await ProcessEffects(effects);
			return;
		}

        private async Task ProcessEffects(PropagationEffects effects, PropagationKind propKind = PropagationKind.ADD_TYPES)
        {
            var orchestrator = SiloOrchestrator.Instance;
            orchestrator.solutionManager = solutionGrain;
            await orchestrator.PropagateEffectsAsync(effects, propKind);
            //await orchestrator.ProcessMessages();
        }

		#endregion

		public Task<PropagationEffects> PropagateAsync(PropagationKind propKind, IEnumerable<PropGraphNodeDescriptor> reWorkSet)
		{
			StatsHelper.RegisterMsg("MethodEntityGrain::PropagateWithRework", this.GrainFactory);

			//if (status.Equals(EntityGrainStatus.Busy))
			//{
			//	await Task.Delay(WAIT_TIME);
			//	if (status.Equals(EntityGrainStatus.Busy))
			//	{
			//		return new PropagationEffects();
			//	}
			//}

            return this.methodEntityPropagator.PropagateAsync(propKind, reWorkSet);
		}

		public async Task<PropagationEffects> PropagateAsync(PropagationKind propKind)
        {
			await StatsHelper.RegisterMsg("MethodEntityGrain::Propagate", this.GrainFactory);
			
			//if (status.Equals(EntityGrainStatus.Busy))
			//{
			//	await Task.Delay(WAIT_TIME);
			//	if (status.Equals(EntityGrainStatus.Busy))
			//	{
			//		return new PropagationEffects();
			//	}
			//}

            Logger.LogVerbose(this.GetLogger(), "MethodEntityGrain", "Propagate", "Propagation for {0} ", this.methodEntity.MethodDescriptor);

            var sw = new Stopwatch();
            sw.Start();
            var propagationEffects = await this.methodEntityPropagator.PropagateAsync(propKind);
            sw.Stop();
            propagationEffects.SiloAddress = StatsHelper.GetMyIPAddr();

            Logger.LogInfo(this.GetLogger(),"MethodEntityGrain", "Propagate", "End Propagation for {0}. Time elapsed {1} Effects size: {2}", this.methodEntity.MethodDescriptor,sw.Elapsed, propagationEffects.CalleesInfo.Count);
            await StatsHelper.RegisterProgagationUpdates(propagationEffects.NumberOfUpdates, propagationEffects.WorkListInitialSize, this.GrainFactory);

            return propagationEffects;
        }

        public async Task<PropagationEffects> PropagateAsync(CallMessageInfo callMessageInfo)
        {
			await StatsHelper.RegisterMsg("MethodEntityGrain::PropagateCall", this.GrainFactory);
            
			//if (status.Equals(EntityGrainStatus.Busy))
			//{
			//	await Task.Delay(WAIT_TIME);
			//	if (status.Equals(EntityGrainStatus.Busy))
			//	{
			//		return new PropagationEffects();
			//	}
			//}

            var propagationEffects = await this.methodEntityPropagator.PropagateAsync(callMessageInfo);
            propagationEffects.SiloAddress = StatsHelper.GetMyIPAddr();
            StatsHelper.RegisterProgagationUpdates(propagationEffects.NumberOfUpdates, propagationEffects.WorkListInitialSize, this.GrainFactory);

            return propagationEffects;
        }

        public async Task<PropagationEffects> PropagateAsync(ReturnMessageInfo returnMessageInfo)
        {
			StatsHelper.RegisterMsg("MethodEntityGrain::PropagateReturn", this.GrainFactory);

            //if (status.Equals(EntityGrainStatus.Busy))
            //{
            //	await Task.Delay(WAIT_TIME);
            //	if (status.Equals(EntityGrainStatus.Busy))
            //	{
            //		return new PropagationEffects();
            //	}
            //}

            var propagationEffects = await this.methodEntityPropagator.PropagateAsync(returnMessageInfo);
            propagationEffects.SiloAddress = StatsHelper.GetMyIPAddr();
            StatsHelper.RegisterProgagationUpdates(propagationEffects.NumberOfUpdates, propagationEffects.WorkListInitialSize, this.GrainFactory);

            return propagationEffects;
        }

        public Task<ISet<MethodDescriptor>> GetCalleesAsync(int invocationPosition)
        {
			StatsHelper.RegisterMsg("MethodEntityGrain::GetCallees", this.GrainFactory);

			return this.methodEntityPropagator.GetCalleesAsync(invocationPosition);
        }

        public Task<int> GetInvocationCountAsync()
        {
			StatsHelper.RegisterMsg("MethodEntityGrain::GetInvocationCount", this.GrainFactory);

			return this.methodEntityPropagator.GetInvocationCountAsync();
        }

        public Task<bool> IsInitializedAsync()
        {
			StatsHelper.RegisterMsg("MethodEntityGrain::IsInitialized", this.GrainFactory);

			return Task.FromResult(this.methodEntity != null);
        }
           
        public Task<IEnumerable<TypeDescriptor>> GetInstantiatedTypesAsync()
        {
			StatsHelper.RegisterMsg("MethodEntityGrain::GetInstantiatedTypes", this.GrainFactory);

			return this.methodEntityPropagator.GetInstantiatedTypesAsync();
        }

		public Task<SymbolReference> GetDeclarationInfoAsync()
		{
			StatsHelper.RegisterMsg("MethodEntityGrain::GetDeclarationInfo", this.GrainFactory);

			return this.methodEntityPropagator.GetDeclarationInfoAsync();
        }

		public Task<IEnumerable<Annotation>> GetAnnotationsAsync()
		{
			StatsHelper.RegisterMsg("MethodEntityGrain::GetAnnotations", this.GrainFactory);

			return this.methodEntityPropagator.GetAnnotationsAsync();
		}

        public Task<IEnumerable<SymbolReference>> GetCallersDeclarationInfoAsync()
        {
			StatsHelper.RegisterMsg("MethodEntityGrain::GetCallersDeclarationInfo", this.GrainFactory);

			return this.methodEntityPropagator.GetCallersDeclarationInfoAsync();
        }

		public Task<PropagationEffects> RemoveMethodAsync()
		{
			StatsHelper.RegisterMsg("MethodEntityGrain::RemoveMethod", this.GrainFactory);

			return this.methodEntityPropagator.RemoveMethodAsync();
		}

		public Task UnregisterCallerAsync(CallContext callContext)
		{
			StatsHelper.RegisterMsg("MethodEntityGrain::UnregisterCaller", this.GrainFactory);

			return this.methodEntityPropagator.UnregisterCallerAsync(callContext);
		}

		//public Task UnregisterCalleeAsync(CallContext callContext)
		//{
		//	StatsHelper.RegisterMsg("MethodEntityGrain::UnregisterCallee", this.GrainFactory);
		//
		//	return this.methodEntityPropagator.UnregisterCalleeAsync(callContext);
		//}

		public Task<IEnumerable<CallContext>> GetCallersAsync()
		{
			StatsHelper.RegisterMsg("MethodEntityGrain::GetCallers", this.GrainFactory);

			return this.methodEntityPropagator.GetCallersAsync();
		}

		public Task UseDeclaredTypesForParameters()
		{
			StatsHelper.RegisterMsg("MethodEntityGrain::UseDeclaredTypesForParameters", this.GrainFactory);

			return this.methodEntityPropagator.UseDeclaredTypesForParameters();
		}

		public Task<MethodCalleesInfo> FixUnknownCalleesAsync()
		{
			StatsHelper.RegisterMsg("MethodEntityGrain::FixUnknownCalleesAsync", this.GrainFactory);

			return this.methodEntityPropagator.FixUnknownCalleesAsync();
		}

		//public Task<PropagationEffects> GetMoreEffects()
		//{
		//	//StatsHelper.RegisterMsg("MethodEntityGrain::GetMoreEffects:" + this.methodEntity.MethodDescriptor, this.GrainFactory);
		//	StatsHelper.RegisterMsg("MethodEntityGrain::GetMoreEffects", this.GrainFactory);

		//	return this.methodEntityPropagator.GetMoreEffects();
		//}

		//public Task<EntityGrainStatus> GetStatusAsync()
		//{
		//	//StatsHelper.RegisterMsg("MethodEntityGrain::GetStatus:" + this.methodEntity.MethodDescriptor, this.GrainFactory);
		//	StatsHelper.RegisterMsg("MethodEntityGrain::GetStatus", this.GrainFactory);

		//	return Task.FromResult(this.status);
		//}
	}
}
