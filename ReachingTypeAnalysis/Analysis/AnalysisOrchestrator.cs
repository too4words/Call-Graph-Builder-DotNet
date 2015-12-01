// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using OrleansInterfaces;
using ReachingTypeAnalysis.Communication;
using SolutionTraversal.CallGraph;
using System.Linq;
using Orleans;
using System.Diagnostics;
using System;
using System.Collections.Concurrent;

namespace ReachingTypeAnalysis.Analysis
{
	/// <summary>
	/// This class is a prototype version of the propagation analysis avoiding the rentrancy in grains.
	/// All methods are static.
	/// Essentially, now the MethodGrain only performs the Propagation within its graph but doesn't trigger the 
	/// propagation to other methods. This is done by this orchestator
	/// This version of the prototype "flattens" the OrleansDispacther and MethodEntityProcessor and make extensive use of await
	/// This should be improved in next versions.
	/// </summary>
	internal class AnalysisOrchestrator
	{
		private const long QUEUE_THRESHOLD = 10000;
		private ISolutionManager solutionManager;
		//private ISet<Message> messageWorkList;
		//private Queue<Message> messageWorkList;
		private ConcurrentQueue<Message> messageWorkList;

		public AnalysisOrchestrator(ISolutionManager solutionManager)
		{
			this.solutionManager = solutionManager;
			//this.messageWorkList = new HashSet<Message>();
			//this.messageWorkList = new Queue<Message>();
			this.messageWorkList = new ConcurrentQueue<Message>();
		}

        public async Task AnalyzeDistributedAsync(IEnumerable<MethodDescriptor> rootMethods)
        {
            foreach (var method in rootMethods)
            {
                if (GrainClient.IsInitialized)
                {
                    Logger.LogInfo(GrainClient.Logger, "Orchestrator", "AnalyzeDistributedAsync", "Analyzing: {0}", method);
                }

                var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(method) as IMethodEntityGrain;
  
                await methodEntityProc.UseDeclaredTypesForParameters();
                //var propagationEffects = await methodEntityProc.PropagateAsync(PropagationKind.ADD_TYPES);
                //await this.PropagateEffectsAsync(propagationEffects, PropagationKind.ADD_TYPES, methodEntityProc);
                await methodEntityProc.PropagateAndProcessAsync(PropagationKind.ADD_TYPES);
            }
            var count = 0;

            var solutionGrain = (ISolutionGrain)this.solutionManager;
			count = await solutionGrain.UpdateCounter(0);
            while(count>0)
            {
				await Task.Delay(5000);
				count = await solutionGrain.UpdateCounter(0);
				if (GrainClient.IsInitialized)
				{
					Logger.LogWarning(GrainClient.Logger, "Orchestrator", "AnalyzeDistributedAsync", "Message Counter: {0}", count);
				}
            }

            // await this.ProcessMessages();

            // TODO: Remove these lines
            var reachableMethodsCount = await this.solutionManager.GetReachableMethodsCountAsync();

            if (GrainClient.IsInitialized)
            {
                Logger.LogWarning(GrainClient.Logger, "Orchestrator", "AnalyzeDistributedAsync", "Reachable methods={0}", reachableMethodsCount);
            }

            Console.WriteLine("Reachable methods={0}", reachableMethodsCount);
        }

		public async Task AnalyzeAsync(IEnumerable<MethodDescriptor> rootMethods)
		{
			foreach (var method in rootMethods)
			{
				if (GrainClient.IsInitialized)
				{
					Logger.LogInfo(GrainClient.Logger, "Orchestrator", "AnalyzeAsync", "Analyzing: {0}", method);
				}

				var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(method);

				await methodEntityProc.UseDeclaredTypesForParameters();

				//PropagationEffects propagationEffects = null;
				//var ready = true;
				//
				//do
				//{
				//	propagationEffects = await methodEntityProc.PropagateAsync(PropagationKind.ADD_TYPES);
				//	ready = await WaitForReady(propagationEffects,method);
				//}
				//while (!ready);
				//
				//await PropagateEffectsAsync(propagationEffects, PropagationKind.ADD_TYPES, methodEntityProc);

				var propagationEffects = await methodEntityProc.PropagateAsync(PropagationKind.ADD_TYPES);

				await this.PropagateEffectsAsync(propagationEffects, PropagationKind.ADD_TYPES, methodEntityProc);
			}

			await this.ProcessMessages();

			// TODO: Remove these lines
			var reachableMethodsCount = await this.solutionManager.GetReachableMethodsCountAsync();

			if (GrainClient.IsInitialized)
			{
				Logger.LogWarning(GrainClient.Logger, "Orchestrator", "AnalyzeAsync", "Reachable methods={0}", reachableMethodsCount);
			}

			Console.WriteLine("Reachable methods={0}", reachableMethodsCount);

			//var newlyResolvedCalleesCount = 0;

			//do
			//{
			//	newlyResolvedCalleesCount = await this.PropagateUsingDeclaredTypes();
			//	await this.ProcessMessages();

			//	// TODO: Remove these lines
			//	reachableMethodsCount = await this.solutionManager.GetReachableMethodsCountAsync();

			//	if (GrainClient.IsInitialized)
			//	{
			//		Logger.LogWarning(GrainClient.Logger, "Orchestrator", "AnalyzeAsync", "Reachable methods={0}", reachableMethodsCount);
			//	}

			//	Console.WriteLine("Reachable methods={0}", reachableMethodsCount);
			//}
			//while (newlyResolvedCalleesCount > 0);
		}

		//private async Task<ISet<MethodDescriptor>> PropagateUsingDeclaredTypes()
        private async Task<int> PropagateUsingDeclaredTypes()
		{
			var roots = await this.solutionManager.GetRootsAsync();
			var worklist = new Queue<MethodDescriptor>(roots);
			var visited = new HashSet<MethodDescriptor>();
			//var newlyResolvedCallees = new HashSet<MethodDescriptor>();
			var newlyResolvedCalleesCount = 0;

			while (worklist.Count > 0)
			{
				var currentMethodDescriptor = worklist.Dequeue();
				visited.Add(currentMethodDescriptor);

				var methodEntity = await this.solutionManager.GetMethodEntityAsync(currentMethodDescriptor);
				var calleesInfo = await methodEntity.FixUnknownCalleesAsync();

				if (calleesInfo.HasUnknownCallees)
				{
					var propagationEffects = await methodEntity.PropagateAsync(PropagationKind.ADD_TYPES);

					//newlyResolvedCallees.UnionWith(propagationEffects.CalleesInfo.SelectMany(ci => ci.PossibleCallees));
					newlyResolvedCalleesCount = propagationEffects.CalleesInfo.Sum(ci => ci.PossibleCallees.Count);

					await this.PropagateEffectsAsync(propagationEffects, PropagationKind.ADD_TYPES, methodEntity);
				}

				//var propagationEffects = await methodEntity.PropagateAsync(PropagationKind.ADD_TYPES, calleesInfo.UnknownCallees);
				//await PropagateEffectsAsync(propagationEffects, PropagationKind.ADD_TYPES, methodEntity);

				//await this.ProcessCalleesAsync(calleesInfo.UnknownCallees, PropagationKind.ADD_TYPES);

				foreach (var calleeDescriptor in calleesInfo.ResolvedCallees)
				{
					if (!visited.Contains(calleeDescriptor) && !worklist.Contains(calleeDescriptor))
					{
						worklist.Enqueue(calleeDescriptor);
					}
				}
			}

			//return newlyResolvedCallees;
			return newlyResolvedCalleesCount;
        }

		//private async Task<bool> WaitForReady(PropagationEffects propagationEffects, MethodDescriptor method,  int millisecondsDelay = 100)
		//{
		//	var ready = propagationEffects.MethodEntityReady;

		//	if (!ready)
		//	{
		//		Logger.LogS("AnalysisOrchestator", "WaitForReady", "Method {0} not ready", method);
		//		await Task.Delay(millisecondsDelay);
		//	}

		//	return ready;
		//}

		//public async Task WaitMethodEntityGrainToBeReady(IMethodEntityWithPropagator methodEntityProp,  int millisecondsDelay = 100)
		//{
		//	if (methodEntityProp is IMethodEntityGrain)
		//	{
		//		var methodEntityGrain = (IMethodEntityGrain)methodEntityProp;
		//		var methodEntityGrainStatus = await methodEntityGrain.GetStatusAsync();

		//		while (methodEntityGrainStatus!= EntityGrainStatus.Ready)
		//		{
		//			await Task.Delay(millisecondsDelay);
		//			methodEntityGrainStatus = await methodEntityGrain.GetStatusAsync();
		//		}
		//	}

		//	return;
		//}

		private async Task WaitQueue(long threshold, int millisecondsDelay = 100)
		{
			while (this.messageWorkList.Count > threshold)
			{
				if (GrainClient.IsInitialized)
				{
					Logger.LogWarning(GrainClient.Logger, "AnalysisOrchestator", "WaitQueue", "Size {0}", this.messageWorkList.Count);
				}

				await ProcessMessages();
				// await Task.Delay(millisecondsDelay);
			}

			return;
		}

		public async Task AnalyzeAsync(MethodDescriptor method, IEnumerable<PropGraphNodeDescriptor> reworkSet = null, PropagationKind propKind = PropagationKind.ADD_TYPES)
		{
			Logger.LogS("AnalysisOrchestator", "AnalyzeAsync", "Analyzing {0} ", method);

			if (reworkSet == null)
			{
				reworkSet = new HashSet<PropGraphNodeDescriptor>();
			}

			var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(method);

			//PropagationEffects propagationEffects = null;
			//var ready = true;
			//
			//do
			//{
			//	propagationEffects = await methodEntityProc.PropagateAsync(propKind, reworkSet);
			//	ready = await WaitForReady(propagationEffects, method);
			//}
			//while (!ready);
			//
			//await PropagateEffectsAsync(propagationEffects, propKind, methodEntityProc);

			var propagationEffects = await methodEntityProc.PropagateAsync(propKind, reworkSet);
			await PropagateEffectsAsync(propagationEffects, propKind, methodEntityProc);
			await ProcessMessages();
		}

		private async Task ProcessMessages()
		{
			while (messageWorkList.Count > 0)
			{
				// TODO: This version is much faster, works well with orleans but there are some data races for the async case
				var tasks = new List<Task>();

				while (messageWorkList.Count > 0)
				{
					//var message = messageWorkList.Dequeue();
					Message message = null;

					if (messageWorkList.TryDequeue(out message))
					{
						// Logger.LogWarning(GrainClient.Logger, "Orchestrator", "ProcessMessage", "Deqeued: {0} Count: {1}", message, messageWorkList.Count);

						if (message is CallerMessage)
						{
							var callerMessage = (CallerMessage)message;
							var callerMessageInfo = callerMessage.CallMessageInfo;
							var task = this.AnalyzeCalleeAsync(callerMessageInfo.Callee, callerMessage, callerMessageInfo.PropagationKind);
							//await task;
							tasks.Add(task);
						}
						else if (message is CalleeMessage)
						{
							var calleeMessage = (CalleeMessage)message;
							var calleeMessageInfo = calleeMessage.ReturnMessageInfo;
                            var task = this.AnalyzeReturnAsync(calleeMessageInfo.Caller, calleeMessage, calleeMessageInfo.PropagationKind);
							//await task;
							tasks.Add(task);
						}
					}
				}

				await Task.WhenAll(tasks);
			}

			//while (messageWorkList.Count > 0)
			//{
			//	var message = messageWorkList.Dequeue();

			//	if (message is CallerMessage)
			//	{
			//		var callerMessage = (CallerMessage)message;
			//		var callerMessageInfo = callerMessage.CallMessageInfo;
			//		await this.AnalyzeCalleeAsync(callerMessageInfo.Callee, callerMessage, callerMessageInfo.PropagationKind);
			//	}
			//	else if (message is CalleeMessage)
			//	{
			//		var calleeMessage = (CalleeMessage)message;
			//		var calleeMessageInfo = calleeMessage.ReturnMessageInfo;
			//		await this.AnalyzeReturnAsync(calleeMessageInfo.Caller, calleeMessage, calleeMessageInfo.PropagationKind);
			//	}
			//}
		}

		private async Task PropagateEffectsAsync(PropagationEffects propagationEffects, PropagationKind propKind, IMethodEntityWithPropagator methodEntityProp = null)
		{
			//var hasMoreEffects = true;

			//do
			//{
			//Logger.LogS("AnalysisOrchestator", "DoPropagationOfEffects", "");

			if (GrainClient.IsInitialized)
			{
				Logger.LogInfo(GrainClient.Logger, "Orchestrator", "PropagatEffFects", "Propagating effets computed in {0}", propagationEffects.SiloAddress);
			}

			await this.ProcessCalleesAsync(propagationEffects.CalleesInfo, propKind);

			if (propagationEffects.ResultChanged)
			{
				await this.ProcessReturnAsync(propagationEffects.CallersInfo, propKind);
			}

			//hasMoreEffects = propagationEffects.MoreEffectsToFetch;

			//if (hasMoreEffects && methodEntityProp != null)
			//{
			//	propagationEffects = await methodEntityProp.GetMoreEffects();
			//}
			//}
			//while (hasMoreEffects);
		}

		private async Task ProcessCalleesAsync(IEnumerable<CallInfo> calleesInfo, PropagationKind propKind)
		{
			var tasks = new List<Task>();

			foreach (var calleeInfo in calleesInfo)
			{
				//TODO: Need to Refactor to get rid of this ifs!
				if (calleeInfo is MethodCallInfo)
				{
					// TODO: This should work in any order because the technique is flow insensitive 
					// But actually for some reason the ordering is affecting the result
					// I think the problem is the conservative treatment when types(receiver).Count = 0 
					// (FIXED??)

					var task = this.DispatchCallMessageForMethodCallAsync(calleeInfo as MethodCallInfo, propKind);
					//await task;
					tasks.Add(task);
				}
				else if (calleeInfo is DelegateCallInfo)
				{
					var task = this.DispatchCallMessageForDelegateCallAsync(calleeInfo as DelegateCallInfo, propKind);
					//await task;
					tasks.Add(task);
				}
			}

			await Task.WhenAll(tasks);
		}

		private async Task DispatchCallMessageForMethodCallAsync(MethodCallInfo methodCallInfo, PropagationKind propKind)
		{
			var tasks = new List<Task>();

			foreach (var callee in methodCallInfo.PossibleCallees)
			{
				var task = this.CreateAndSendCallMessageAsync(methodCallInfo, callee, propKind);
				//await task;
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
		}

		private async Task DispatchCallMessageForDelegateCallAsync(DelegateCallInfo delegateCallInfo, PropagationKind propKind)
		{
			var tasks = new List<Task>();

			foreach (var callee in delegateCallInfo.PossibleCallees)
			{
				var task = this.CreateAndSendCallMessageAsync(delegateCallInfo, callee, propKind);
				//await task;
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
		}

		private Task CreateAndSendCallMessageAsync(CallInfo callInfo, MethodDescriptor callee, PropagationKind propKind)
		{
			var callMessageInfo = new CallMessageInfo(callInfo.Caller, callee, callInfo.ReceiverPossibleTypes,
				callInfo.ArgumentsPossibleTypes, callInfo.InstantiatedTypes, callInfo.CallNode, callInfo.LHS, propKind);

			var source = new MethodEntityDescriptor(callInfo.Caller);
			var callerMessage = new CallerMessage(source, callMessageInfo);

			//Logger.LogWarning(GrainClient.Logger, "Orchestrator", "CreateAndSendCallMsg", "Enqueuing: {0}", callee);

			//await WaitQueue(QUEUE_THRESHOLD);
			this.messageWorkList.Enqueue(callerMessage);
			//this.messageWorkList.Add(callerMessage);

			return TaskDone.Done;
			//return AnalyzeCalleeAsync(callMessageInfo.Callee, callerMessage, propKind);
		}

		/// <summary>
		/// This method "replaces" the send + dispatch + processCallMessage for calless that used the methodProcessor and dispatcher
		/// The idea is trying to avoid reentrant calls to grains
		/// We wil need to improve the code / design but this is a first approach to solve that problem
		/// </summary>
		/// <param name="callee"></param>
		/// <param name="callerMessage"></param>
		/// <param name="propKind"></param>
		/// <returns></returns>
		private async Task AnalyzeCalleeAsync(MethodDescriptor callee, CallerMessage callerMessage, PropagationKind propKind)
		{
			if (GrainClient.IsInitialized)
			{
				Logger.LogInfo(GrainClient.Logger, "Orchestrator", "AnalyzeCalleeAsync", "Analyzing: {0}", callee);
			}

			//Logger.LogS("AnalysisOrchestator", "AnalyzeCalleeAsync", "Analyzing call to {0} ", callee);

			var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(callee);

			//PropagationEffects propagationEffects = null;
			//var ready = true;
			//
			//do
			//{
			//	propagationEffects = await methodEntityProc.PropagateAsync(callerMessage.CallMessageInfo);
			//	ready = await WaitForReady(propagationEffects,callee);
			//}
			//while (!ready);
			//
			//await this.PropagateEffectsAsync(propagationEffects, propKind, methodEntityProc);

			var propagationEffects = await methodEntityProc.PropagateAsync(callerMessage.CallMessageInfo);
			await this.PropagateEffectsAsync(propagationEffects, propKind, methodEntityProc);

			Logger.LogS("AnalysisOrchestator", "AnalyzeCalleeAsync", "End Analyzing call to {0} ", callee);
		}

		private async Task ProcessReturnAsync(IEnumerable<ReturnInfo> callersInfo, PropagationKind propKind)
		{
			var tasks = new List<Task>();

			foreach (var callerInfo in callersInfo)
			{
				var task = this.DispachReturnMessageAsync(callerInfo, propKind);
				//await task;
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
		}

		private Task DispachReturnMessageAsync(ReturnInfo returnInfo, PropagationKind propKind)
		{
			return this.CreateAndSendReturnMessageAsync(returnInfo, propKind);
		}

		private Task CreateAndSendReturnMessageAsync(ReturnInfo returnInfo, PropagationKind propKind)
		{
			var returnMessageInfo = new ReturnMessageInfo(returnInfo.CallerContext.Caller, returnInfo.Callee, returnInfo.ResultPossibleTypes, returnInfo.InstantiatedTypes,
				returnInfo.CallerContext.CallNode, returnInfo.CallerContext.LHS, propKind);

			var source = new MethodEntityDescriptor(returnInfo.Callee);
			var calleeMessage = new CalleeMessage(source, returnMessageInfo);

			//await WaitQueue(QUEUE_THRESHOLD);
			this.messageWorkList.Enqueue(calleeMessage);
			//this.messageWorkList.Add(calleeMessage);

			return TaskDone.Done;
			//return AnalyzeReturnAsync(returnMessageInfo.Caller, calleeMessage, propKind);
		}

		/// <summary>
		/// This method "replaces" the send + dispatch + processReturnMessage for calless that used the methodProcessor and dispatcher
		/// The idea is trying to avoid reentrant calls to grains
		/// We wil need to improve the code / design but this is a first approach to solve that problem
		/// </summary>
		/// <param name="caller"></param>
		/// <param name="calleeMessage"></param>
		/// <param name="propKind"></param>
		/// <returns></returns>
		private async Task AnalyzeReturnAsync(MethodDescriptor caller, CalleeMessage calleeMessage, PropagationKind propKind)
		{
			Logger.LogS("AnalysisOrchestator", "AnalyzeReturnAsync", "Analyzing return to {0} ", caller);

			var methodEntityProc = await this.solutionManager.GetMethodEntityAsync(caller);

			//PropagationEffects propagationEffects = null;
			//var ready = true;
			//
			//do
			//{
			//	propagationEffects = await methodEntityProc.PropagateAsync(calleeMessage.ReturnMessageInfo);
			//	ready = await WaitForReady(propagationEffects, caller);
			//}
			//while (!ready);
			//
			//await this.PropagateEffectsAsync(propagationEffects, propKind, methodEntityProc);

			var propagationEffects = await methodEntityProc.PropagateAsync(calleeMessage.ReturnMessageInfo);
			await this.PropagateEffectsAsync(propagationEffects, propKind, methodEntityProc);

			Logger.LogS("AnalysisOrchestator", "AnalyzeReturnAsync", "End Analyzing return to {0} ", caller);
		}

		#region Incremental Analysis

		public async Task RemoveMethodAsync(MethodDescriptor method, string newSource)
		{
			var projectProvider = await this.solutionManager.GetProjectCodeProviderAsync(method);

			var propagationEffects = await projectProvider.RemoveMethodAsync(method);

			await this.PropagateEffectsAsync(propagationEffects, PropagationKind.REMOVE_TYPES);
			await this.ProcessMessages();
			await this.UnregisterCallerAsync(propagationEffects.CalleesInfo);

			await projectProvider.ReplaceDocumentSourceAsync(newSource, TestConstants.DocumentPath);

			// TODO: Is this necessary? 
			// await this.PropagateFromCallersAsync(propagationEffects.CallersInfo);

			// await this.UnregisterCallee(propagationEffects.CallersInfo);
		}

		private async Task UnregisterCallerAsync(IEnumerable<CallInfo> calleesInfo)
		{
			var tasks = new List<Task>();

			foreach (var calleeInfo in calleesInfo)
			{
				foreach (var callee in calleeInfo.PossibleCallees)
				{
					var calleeEntity = await this.solutionManager.GetMethodEntityAsync(callee);
					var callContext = new CallContext(calleeInfo.Caller, calleeInfo.LHS, calleeInfo.CallNode);
					var task = calleeEntity.UnregisterCallerAsync(callContext);
					//await task;
					tasks.Add(task);
				}
			}

			await Task.WhenAll(tasks);
		}

		//private async Task UnregisterCallee(IEnumerable<ReturnInfo> callersInfo)
		//{
		//    var tasks = new List<Task>();

		//    foreach (var callerInfo in callersInfo)
		//    {
		//        var callerMethodEntity = await this.solutionManager.GetMethodEntityAsync(callerInfo.CallerContext.Caller);
		//        var task = callerMethodEntity.UnregisterCalleeAsync(callerInfo.CallerContext);
		//        //await task;
		//        tasks.Add(task);
		//    }

		//    await Task.WhenAll(tasks);
		//}

		public async Task UpdateMethodAsync(MethodDescriptor method, string newSource)
		{
			var projectProvider = await this.solutionManager.GetProjectCodeProviderAsync(method);

			var propagationEffects = await projectProvider.RemoveMethodAsync(method);

			await this.PropagateEffectsAsync(propagationEffects, PropagationKind.REMOVE_TYPES);
			await this.ProcessMessages();
			await this.UnregisterCallerAsync(propagationEffects.CalleesInfo);

			await projectProvider.ReplaceDocumentSourceAsync(newSource, TestConstants.DocumentPath);

			// TODO: if there is no caller (e.g., main in the future public method) you should call yourself

			await this.PropagateFromCallersAsync(propagationEffects.CallersInfo);

			await this.AddMethodAsync(method, newSource);

			// await this.UnregisterCallee(propagationEffects.CallersInfo);
		}

		private async Task PropagateFromCallersAsync(IEnumerable<ReturnInfo> callersInfo, PropagationKind propKind = PropagationKind.ADD_TYPES)
		{
			var tasks = new List<Task>();

			foreach (var callerInfo in callersInfo)
			{
				var reworkSet = new HashSet<PropGraphNodeDescriptor>();
				var callContext = callerInfo.CallerContext;
				reworkSet.Add(callContext.CallNode);
				var task = this.AnalyzeAsync(callContext.Caller, reworkSet, propKind);
				//await task;
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
		}

		public async Task AddMethodAsync(MethodDescriptor method, string newSource)
		{
			var projectProvider = await this.solutionManager.GetProjectCodeProviderAsync(method);

			await projectProvider.ReplaceDocumentSourceAsync(newSource, TestConstants.DocumentPath);

			// TODO: is the method is an overrride reanalize callers of *base* method
			// For the case of overload we need to detect the callers of all possible overloads for all "compatible" types
		}

		public async Task ApplyModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			/*
			 * 1) Compute diffs
			 *		In every code provider compute diff (optionally keep a copy of compilation and semantic models). Do not replace old compilation and models
			 *		Return list of method descriptors and action (or 3 lists, remove, update, add)
			 * 2) Perfom propagation
			 *		Start from Deletes (and updates seeing as delete)
			 *		a) For each method m: compute propagation effects (call method entity remove)
			 *			Enqueue as Remove_Types
			 *			Process all together
			 *		b) Swtich all providers to their new verions
			 *			Replace compilation and semantic models in providers 
			 *			Replace solution (in grain and manager)
			 *		c) finalize remove code (propagate from callers)
			 *		Now propagate Adds	
			*/

			var calleesInfo = new List<CallInfo>();
			var callersInfo = new List<ReturnInfo>();
			var modifications = await this.solutionManager.GetModificationsAsync(modifiedDocuments.ToList());

			var methodsRemoved = from m in modifications
								 where m.ModificationKind == ModificationKind.MethodRemoved
								 select m.MethodDescriptor;

			var methodsUpdated = from m in modifications
								 where m.ModificationKind == ModificationKind.MethodUpdated
								 select m.MethodDescriptor;

			var methodsAdded = from m in modifications
							   where m.ModificationKind == ModificationKind.MethodAdded
							   select m.MethodDescriptor;

			// TODO: log modified methods
			Logger.Log("Removed methods:\n{0}", string.Join("\n", methodsRemoved));
			Logger.Log("Modified methods:\n{0}", string.Join("\n", methodsUpdated));
			Logger.Log("Added methods:\n{0}", string.Join("\n", methodsAdded));

			var methodsRemovedOrUpdated = methodsRemoved.Union(methodsUpdated);
			var methodsAddedOrUpdated = methodsAdded.Union(methodsUpdated);

			foreach (var method in methodsRemovedOrUpdated)
			{
				var projectProvider = await this.solutionManager.GetProjectCodeProviderAsync(method);
				var propagationEffects = await projectProvider.RemoveMethodAsync(method);

				calleesInfo.AddRange(propagationEffects.CalleesInfo);
				callersInfo.AddRange(propagationEffects.CallersInfo);

				await this.PropagateEffectsAsync(propagationEffects, PropagationKind.REMOVE_TYPES);
			}

			await this.ProcessMessages();
			await this.UnregisterCallerAsync(calleesInfo);
			await this.solutionManager.ReloadAsync();

			// Don't propagate from modified callers since their call nodes may no longer exist
			callersInfo.RemoveAll(callerInfo => methodsRemovedOrUpdated.Contains(callerInfo.CallerContext.Caller));
			await this.PropagateFromCallersAsync(callersInfo);

			// If there is no caller (e.g., main or in the future public methods) you should call yourself
			var roots = await this.solutionManager.GetRootsAsync();
			var modifiedRoots = roots.Intersect(methodsAddedOrUpdated);

			await AnalyzeAsync(modifiedRoots);

			// This is only for overrides
			foreach (var method in methodsAdded)
			{
				// TODO: is the method is an overrride reanalyze callers of *base* method
				// For the case of overload we need to detect the callers of all possible overloads for all "compatible" types
				var codeProvider = await this.solutionManager.GetProjectCodeProviderAsync(method);

				// This call actually computes the callers of an overriden method, for other cases there is no effect
				var propagationEffects = await codeProvider.AddMethodAsync(method);

				if (propagationEffects.CallersInfo.Count > 0)
				{
					// For each caller we need to do the following
					// 0: Propagate forward the deletion of the previous calle (now overriden)
					// TODO: Maybe this needs to be done before reloading the project [but we need another way of discovering the override]
					// ANOTHER OPTION: Get in the list of modifications the overriden methods with a qualifier [OVERRIDEN]
					// in this way we can move this to the previous 
					await this.PropagateFromCallersAsync(propagationEffects.CallersInfo, PropagationKind.REMOVE_TYPES);

					// 1: Propagate the information from the callers of the overriden method to the *new* method
					await this.PropagateFromCallersAsync(propagationEffects.CallersInfo, PropagationKind.ADD_TYPES);

					// 2: In the overriden method we need to remove the callers that do not call this method any longer 
					// [they are the caller that have the type of the subclass as possible type as receiver of the invocation]

					var methodEntityWP = await codeProvider.GetMethodEntityAsync(method);
					var callersOfMethod = await methodEntityWP.GetCallersAsync();
					// Here all callees should be the same overriden method
					var overridenMethodDescriptor = propagationEffects.CallersInfo.Select(callerInfo => callerInfo.Callee).First();
					var overridenCalleeEntity = await codeProvider.GetMethodEntityAsync(overridenMethodDescriptor);

					foreach (var callContext in callersOfMethod)
					{
						await overridenCalleeEntity.UnregisterCallerAsync(callContext);
					}
				}
			}
		}

		#endregion
	}
}
