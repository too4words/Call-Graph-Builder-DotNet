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
using Orleans.Concurrency;
using Orleans.Streams;
using System.Linq;
using System.IO;

namespace ReachingTypeAnalysis.Analysis
{
	//public interface IMethodState : IGrainState
	public class MethodState
	{
		public MethodDescriptor MethodDescriptor { get; set; }
    }

    //[StorageProvider(ProviderName = "FileStore")]
    //[StorageProvider(ProviderName = "MemoryStore")]
	//[StorageProvider(ProviderName = "AzureTableStore")]
    //[Reentrant]
	//[PreferLocalPlacement]
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
		[NonSerialized]
		private int currentStreamIndex;
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

			this.currentStreamIndex = this.GetHashCode() % AnalysisConstants.StreamCount;

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
			this.methodEntityPropagator = new MethodEntityWithPropagator(methodEntity, codeProvider);

            await this.WriteStateAsync();

			//Logger.LogWarning(this.GetLogger(), "MethodEntityGrain", "CreateMethodEntity", "Exit {0}", methodDescriptor);
		}

        public async Task<ISet<MethodDescriptor>> GetCalleesAsync()
        {
			await StatsHelper.RegisterMsg("MethodEntityGrain::GetCallees", this.GrainFactory);

			var result = await this.methodEntityPropagator.GetCalleesAsync();
			return result;
        }

        public async Task<IDictionary<AnalysisCallNode, ISet<MethodDescriptor>>> GetCalleesInfoAsync()
        {
			await StatsHelper.RegisterMsg("MethodEntityGrain::GetCalleesInfo", this.GrainFactory);

			var result = await this.methodEntityPropagator.GetCalleesInfoAsync();
			return result;
        }

        public async Task PropagateAndProcessAsync(PropagationKind propKind, IEnumerable<PropGraphNodeDescriptor> reWorkSet)
        {
            await StatsHelper.RegisterMsg("MethodEntityGrain::PropagateAndProcess", this.GrainFactory);
            var effects = await this.methodEntityPropagator.PropagateAsync(propKind, reWorkSet); // await this.PropagateAsync(propKind, reWorkSet);
            await StatsHelper.RegisterPropagationUpdates(effects.NumberOfUpdates, effects.WorkListInitialSize, this.GrainFactory);

            await ProcessEffectsAsync(effects);
        }

        public async Task PropagateAndProcessAsync(PropagationKind propKind)
        {
            await StatsHelper.RegisterMsg("MethodEntityGrain::PropagateAndProcess", this.GrainFactory);
            var effects = await this.methodEntityPropagator.PropagateAsync(propKind);
            await StatsHelper.RegisterPropagationUpdates(effects.NumberOfUpdates, effects.WorkListInitialSize, this.GrainFactory);

            // await this.PropagateAsync(propKind);
            await ProcessEffectsAsync(effects);
        }

		public async Task PropagateAndProcessAsync(CallMessageInfo callMessageInfo)
		{
			await StatsHelper.RegisterMsg("MethodEntityGrain::PropagateAndProcess", this.GrainFactory);
			var effects = await this.methodEntityPropagator.PropagateAsync(callMessageInfo); // await this.PropagateAsync(callMessageInfo);
			await StatsHelper.RegisterPropagationUpdates(effects.NumberOfUpdates, effects.WorkListInitialSize, this.GrainFactory);

			await ProcessEffectsAsync(effects);
		}

		public async Task PropagateAndProcessAsync(ReturnMessageInfo returnMessageInfo)
		{
			await StatsHelper.RegisterMsg("MethodEntityGrain::PropagateAndProcess", this.GrainFactory);
			var effects = await this.methodEntityPropagator.PropagateAsync(returnMessageInfo); //await this.PropagateAsync(returnMessageInfo);
			await StatsHelper.RegisterPropagationUpdates(effects.NumberOfUpdates, effects.WorkListInitialSize, this.GrainFactory);

			await ProcessEffectsAsync(effects);
		}

        private async Task ProcessEffectsAsync(PropagationEffects effects, PropagationKind propKind = PropagationKind.ADD_TYPES)
		{
			effects.Kind = propKind;
			var maxCalleesInfoCount = Math.Min(effects.CalleesInfo.Count, 8);

			await this.SplitAndEnqueueEffectsAsync(effects, maxCalleesInfoCount, 8 * 2);
		}

		private async Task SplitAndEnqueueEffectsAsync(PropagationEffects effects, int maxCalleesInfoCount, int maxPossibleCalleesCount)
		{
			IEnumerable<PropagationEffects> messages = null;

			if (maxCalleesInfoCount > 1)
			{
				messages = SplitCalleesInfo(effects, maxCalleesInfoCount);
			}
			else if (maxCalleesInfoCount == 1 && maxPossibleCalleesCount > 1)
			{
				messages = SplitPossibleCallees(effects, maxPossibleCalleesCount);
			}
			else if(maxCalleesInfoCount == 1 && maxPossibleCalleesCount == 1)
			{
				messages = new PropagationEffects[] { effects };
			}

			if (messages != null)
			{
				var tasks = new List<Task>();

				foreach (var message in messages)
				{
					var task = this.EnqueueEffectsAsync(message, maxCalleesInfoCount, maxPossibleCalleesCount);
					//await task;
					tasks.Add(task);
				}

				await Task.WhenAll(tasks);
			}
		}
		
		private async Task EnqueueEffectsAsync(PropagationEffects effects, int maxCalleesInfoCount, int maxPossibleCalleesCount)
		{
			var splitEffects = false;
			var retryCount = AnalysisConstants.StreamCount; // 3

			do
			{
				var stream = this.SelectStream();

				try
				{
					await stream.OnNextAsync(effects);
					break;
				}
				catch (Exception ex)
				{
					var innerEx = ex;
					while (innerEx is AggregateException) innerEx = innerEx.InnerException;

					if (innerEx is ArgumentException && (maxCalleesInfoCount > 1 || maxPossibleCalleesCount > 1))
					{
						// Messages cannot be larger than 65536 bytes
						splitEffects = true;
						break;
					}
					else
					{
						retryCount--;

						if (retryCount == 0)
						{
							//var effectsInfo = this.SerializeEffects(effects);
							var effectsInfo = this.GetEffectsInfo(effects);
							Logger.LogError(this.GetLogger(), "MethodEntityGrain", "EnqueueEffects", "Exception on OnNextAsync (maxCount = {0}, {1})\n{2}", maxCalleesInfoCount, effectsInfo, ex);
							throw ex;
						}
					}
				}
			}
			while (retryCount > 0);

			if (splitEffects)
			{
				if (maxCalleesInfoCount > 1)
				{
					var newMaxCalleesInfoCount = (maxCalleesInfoCount / 2) + (maxCalleesInfoCount % 2);

					Logger.LogForRelease(this.GetLogger(), "@@[MethodEntityGrain {0}] Splitting effects (calleesInfo) of size {1} into parts of size {2}", this.methodEntity.MethodDescriptor, maxCalleesInfoCount, newMaxCalleesInfoCount);

					await this.SplitAndEnqueueEffectsAsync(effects, newMaxCalleesInfoCount, maxPossibleCalleesCount);
				}
				else if (maxCalleesInfoCount == 1 && maxPossibleCalleesCount > 1)
				{
					// TODO: Bug here in max: sequence contains no elements!
					var possibleCalleesCount = effects.CalleesInfo.Max(x => x.PossibleCallees.Count);
					maxPossibleCalleesCount = Math.Min(possibleCalleesCount, maxPossibleCalleesCount);
					var newMaxPossibleCalleesCount = (maxPossibleCalleesCount / 2) + (maxPossibleCalleesCount % 2);

					Logger.LogForRelease(this.GetLogger(), "@@[MethodEntityGrain {0}] Splitting effects (possibleCallees) of size {1} into parts of size {2}", this.methodEntity.MethodDescriptor, maxPossibleCalleesCount, newMaxPossibleCalleesCount);

					await this.SplitAndEnqueueEffectsAsync(effects, maxCalleesInfoCount, newMaxPossibleCalleesCount);
				}
			}
		}

		private string GetEffectsInfo(PropagationEffects effects)
		{
			var callees = 0;
			var arguments = 0;

			foreach (var calleeInfo in effects.CalleesInfo)
			{
				callees += calleeInfo.PossibleCallees.Count;
				arguments += calleeInfo.ArgumentsPossibleTypes.Sum(ts => ts.Count);
			}

			var result = string.Format("callees = {0}, argument types = {1}", callees, arguments);
			return result;
		}

		// This doesn't work with circular references present in effects (MethodDescriptor.BaseDescriptor)
		//private string SerializeEffects(PropagationEffects effects)
		//{
		//	var result = string.Empty;
		//	var serializer = Newtonsoft.Json.JsonSerializer.CreateDefault();

		//	using (var writer = new StringWriter())
		//	{
		//		serializer.Serialize(writer, effects);
		//		result = writer.ToString();
		//	}

		//	return result;
		//}

		private static IEnumerable<PropagationEffects> SplitCalleesInfo(PropagationEffects effects, int maxCount)
		{
			var result = new List<PropagationEffects>();
			var calleesInfo = effects.CalleesInfo.ToList();
			var count = calleesInfo.Count;
			var index = 0;

			while (count > maxCount)
			{
				var callees = calleesInfo.GetRange(index, maxCount);
				var message = new PropagationEffects(callees, false);

				result.Add(message);

				count -= maxCount;
				index += maxCount;
			}

			if (count > 0)
			{
				var callees = calleesInfo.GetRange(index, count);
				var message = new PropagationEffects(callees, false);

				result.Add(message);
			}

			if (effects.ResultChanged)
			{
				var callersInfo = effects.CallersInfo.ToList();
				count = callersInfo.Count;
				index = 0;

				while (count > maxCount)
				{
					var callers = callersInfo.GetRange(index, maxCount);
					var message = new PropagationEffects(callers);

					result.Add(message);

					count -= maxCount;
					index += maxCount;
				}

				if (count > 0)
				{
					var callers = callersInfo.GetRange(index, count);
					var message = new PropagationEffects(callers);

					result.Add(message);
				}
			}

			return result;
		}

		private static IEnumerable<PropagationEffects> SplitPossibleCallees(PropagationEffects effects, int maxCount)
		{
			var result = new List<PropagationEffects>();
			var calleeInfo = effects.CalleesInfo.Single();
			var possibleCallees = calleeInfo.PossibleCallees.ToList();
			var count = possibleCallees.Count;
			var index = 0;

			while (count > maxCount)
			{
				var callees = possibleCallees.GetRange(index, maxCount);
				var newCalleeInfo = calleeInfo.Clone(callees);
				var calleesInfo = new CallInfo[] { newCalleeInfo };
				var message = new PropagationEffects(calleesInfo, false);

				result.Add(message);

				count -= maxCount;
				index += maxCount;
			}

			if (count > 0)
			{
				var callees = possibleCallees.GetRange(index, count);
				var newCalleeInfo = calleeInfo.Clone(callees);
				var calleesInfo = new CallInfo[] { newCalleeInfo };
				var message = new PropagationEffects(calleesInfo, false);

				result.Add(message);
			}

			if (effects.ResultChanged)
			{
				var message = new PropagationEffects(effects.CallersInfo);

				result.Add(message);
			}

			return result;
		}

		private IAsyncStream<PropagationEffects> SelectStream()
		{
			this.currentStreamIndex = (this.currentStreamIndex + 1) % AnalysisConstants.StreamCount;

			var streamId = string.Format(AnalysisConstants.StreamGuidFormat, currentStreamIndex);
			var streamGuid = Guid.Parse(streamId);
			var streamProvider = this.GetStreamProvider(AnalysisConstants.StreamProvider);
			var stream = streamProvider.GetStream<PropagationEffects>(streamGuid, AnalysisConstants.StreamNamespace);

			Logger.LogInfoForDebug(this.GetLogger(), "@@[MethodEntityGrain {0}] Enqueuing effects into stream {1}", this.methodEntity.MethodDescriptor, streamGuid);

			return stream;
		}

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
            await StatsHelper.RegisterPropagationUpdates(propagationEffects.NumberOfUpdates, propagationEffects.WorkListInitialSize, this.GrainFactory);

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
            await StatsHelper.RegisterPropagationUpdates(propagationEffects.NumberOfUpdates, propagationEffects.WorkListInitialSize, this.GrainFactory);

            return propagationEffects;
        }

        public async Task<PropagationEffects> PropagateAsync(ReturnMessageInfo returnMessageInfo)
        {
			await StatsHelper.RegisterMsg("MethodEntityGrain::PropagateReturn", this.GrainFactory);

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
            await StatsHelper.RegisterPropagationUpdates(propagationEffects.NumberOfUpdates, propagationEffects.WorkListInitialSize, this.GrainFactory);

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
