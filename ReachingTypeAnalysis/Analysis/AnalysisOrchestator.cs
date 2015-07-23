// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using OrleansInterfaces;
using ReachingTypeAnalysis.Communication;
using SolutionTraversal.Callgraph;
using System.Linq;
using Orleans;
using System.Diagnostics;

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
    internal class AnalysisOrchestator
	{
		private IAnalysisStrategy strategy;
        //private ISet<Message> messageWorkList = new HashSet<Message>();
        private Queue<Message> messageWorkList = new Queue<Message>();

		public AnalysisOrchestator(IAnalysisStrategy strategy)
		{
			this.strategy = strategy;
		}

        private async Task ProcessMessages()
        {
            while(messageWorkList.Count>0)
            {
                var message = messageWorkList.Dequeue();
                //var message = messageWorkList.First();
                //messageWorkList.Remove(message);
                if (message is CallerMessage)
                {
                    var callerMessage = (CallerMessage)message;
                    var callerMessageInfo = callerMessage.CallMessageInfo;
                    await AnalyzeCalleeAsync(callerMessageInfo.Callee, callerMessage, callerMessageInfo.PropagationKind);
                }
                else if(message is CalleeMessage)
                {
                    var calleeMessage = (CalleeMessage)message;
                    await AnalyzeReturnAsync(calleeMessage.ReturnMessageInfo.Caller,calleeMessage,calleeMessage.ReturnMessageInfo.PropagationKind);
                }
            }
        }
		public async Task AnalyzeAsync(MethodDescriptor method)
		{
			Logger.Instance.Log("AnalysisOrchestator", "AnalyzeAsync", "Analyzing {0} ", method);

			var entityDescriptor = new MethodEntityDescriptor(method);
			var methodEntityProc = await strategy.GetMethodEntityAsync(method);
            var propagationEffects = await methodEntityProc.PropagateAsync(PropagationKind.ADD_TYPES);
			await PropagateEffectsAsync(propagationEffects, PropagationKind.ADD_TYPES);
            await ProcessMessages();
		}

		private async Task PropagateEffectsAsync(PropagationEffects propagationEffects, PropagationKind propKind)
		{
			Logger.Instance.Log("AnalysisOrchestator", "DoPropagationOfEffects", "");

			await ProcessCalleesAsync(propagationEffects.CalleesInfo, propKind);

			if (propagationEffects.ResultChanged)
			{
				await ProcessReturnAsync(propagationEffects.CallersInfo, propKind);
			}
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
                    
					var task = DispatchCallMessageForMethodCallAsync(calleeInfo as MethodCallInfo, propKind);
					//await task;
					tasks.Add(task);
				}
				else if (calleeInfo is DelegateCallInfo)
				{
					var task = DispatchCallMessageForDelegateCallAsync(calleeInfo as DelegateCallInfo, propKind);
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
				var task = CreateAndSendCallMessageAsync(methodCallInfo, callee, propKind);
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
				var task = CreateAndSendCallMessageAsync(delegateCallInfo, callee, propKind);
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
			Logger.Instance.Log("AnalysisOrchestator", "AnalyzeCalleeAsync", "Analyzing call to {0} ", callee);

			var methodEntityProc = await strategy.GetMethodEntityAsync(callee);
            var propagationEffects = await methodEntityProc.PropagateAsync(callerMessage.CallMessageInfo);
            await PropagateEffectsAsync(propagationEffects, PropagationKind.ADD_TYPES);

            Logger.Instance.Log("AnalysisOrchestator", "AnalyzeCalleeAsync", "End Analyzing call to {0} ", callee);
		}

		private async Task ProcessReturnAsync(IEnumerable<ReturnInfo> callersInfo, PropagationKind propKind)
		{
			var tasks = new List<Task>();

			foreach (var callerInfo in callersInfo)
			{
				var task = DispachReturnMessageAsync(callerInfo, propKind);
				//await task;
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);
		}

		private async Task DispachReturnMessageAsync(ReturnInfo returnInfo, PropagationKind propKind)
		{
			await CreateAndSendReturnMessageAsync(returnInfo, propKind);
        }

		private Task CreateAndSendReturnMessageAsync(ReturnInfo returnInfo, PropagationKind propKind)
		{
			var returnMessageInfo = new ReturnMessageInfo(returnInfo.CallerContext.Caller, returnInfo.Callee, returnInfo.ResultPossibleTypes, returnInfo.InstantiatedTypes,
				returnInfo.CallerContext.CallNode, returnInfo.CallerContext.LHS, propKind);

			var source = new MethodEntityDescriptor(returnInfo.Callee);
			var calleeMessage = new CalleeMessage(source, returnMessageInfo);
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
			Logger.Instance.Log("AnalysisOrchestator", "AnalyzeReturnAsync", "Analyzing return to {0} ", caller);

			var methodEntityProc = await strategy.GetMethodEntityAsync(caller);
			var propagationEffects = await methodEntityProc.PropagateAsync(calleeMessage.ReturnMessageInfo);
			await PropagateEffectsAsync(propagationEffects, propKind);

            Logger.Instance.Log("AnalysisOrchestator", "AnalyzeReturnAsync", "End Analyzing return to {0} ", caller);
		}

        internal async Task<CallGraph<MethodDescriptor, LocationDescriptor>> GenerateCallGraphAsync(ISolution solution)
        {
            Logger.Instance.Log("AnalysisOrchestator", "GenerateCallGraph", "Start building CG");
            var callgraph = new CallGraph<MethodDescriptor, LocationDescriptor>();
            var roots = await solution.GetRoots();
            callgraph.AddRootMethods(roots);
            var visited = new HashSet<MethodDescriptor>(roots);
            var worklist = new Queue<MethodDescriptor>(roots);
            while (worklist.Count > 0)
            {
                var currentMethodDescriptor = worklist.Dequeue();
                visited.Add(currentMethodDescriptor);
                Logger.Instance.Log("AnalysisOrchestator", "GenerateCallGraph", "Proccesing  {0}",currentMethodDescriptor);

				var currentProc = await strategy.GetMethodEntityAsync(currentMethodDescriptor);
                var calleesInfoForMethod = await currentProc.GetCalleesInfoAsync();
  
                foreach (var entry in calleesInfoForMethod)
                {
                    var analysisNode = entry.Key;
                    var callees = entry.Value;

                    foreach (var calleeDescriptor in callees)
                    {
                        Logger.Instance.Log("AnalysisOrchestator", "GenerateCallGraph", "Adding {0}-{1} to CG",currentMethodDescriptor, calleeDescriptor);
                        callgraph.AddCallAtLocation(analysisNode.LocationDescriptor, currentMethodDescriptor, calleeDescriptor);

                        if (!visited.Contains(calleeDescriptor) && !worklist.Contains(calleeDescriptor))
                        {
                            worklist.Enqueue(calleeDescriptor);
                        }
                    }
                }
            }

            return callgraph;
        }
    }

    public static class CallGraphQueryInterface
    {
        /// These 2 method can work either with Orleans or OndDemandAsync Strategy
        /// <summary>
        /// Return the calless for a given call site
        /// </summary>
        /// <param name="strategy"></param>
        /// <param name="methodDescriptor"></param>
        /// <param name="invocationPosition"></param>
        /// <returns></returns>
        public static async Task<ISet<MethodDescriptor>> GetCalleesAsync(IAnalysisStrategy strategy, MethodDescriptor methodDescriptor, int invocationPosition)
        {
            var totalStopWatch = Stopwatch.StartNew();

            var stopWatch = Stopwatch.StartNew();

            var entityWithPropagator = await strategy.GetMethodEntityAsync(methodDescriptor);
            Meausure("GetMethodEntityProp", stopWatch);

            var result = await entityWithPropagator.GetCalleesAsync(invocationPosition);
            Meausure("entProp.GetCalleesAsync", stopWatch);

            Meausure("Total GetCalleesAsync", totalStopWatch);
            return result;
        }
        /// <summary>
        ///  Return the numnber of calls sites for a 
        /// </summary>
        /// <param name="methodDescriptor"></param>
        /// <param name="projectName"></param>
        /// <returns></returns>
        public static async Task<int> GetInvocationCountAsync(IAnalysisStrategy strategy, MethodDescriptor methodDescriptor)
        {
            var totalStopWatch = Stopwatch.StartNew();

            var stopWatch = Stopwatch.StartNew();

            var entityWithPropagator = await strategy.GetMethodEntityAsync(methodDescriptor);
            Meausure("GetMethodEntityProp", stopWatch);

            var result = await entityWithPropagator.GetInvocationCountAsync();
            Meausure("entProp.GetInvocationCountAsync", stopWatch);

            Meausure("Total GetInvocationCountAsync", totalStopWatch);
            return result;
        }

        /// This version is Orleans ONLY

        /// <summary>
        /// This method used Orleans Grain to obtain the set of calless of a call site
        /// </summary>
        /// <param name="methodDescriptor"></param>
        /// <param name="invocationPosition"></param>
        /// <param name="projectName"></param>
        /// <returns></returns>
        public static async Task<ISet<MethodDescriptor>> GetCalleesOrleansAsync(MethodDescriptor methodDescriptor, int invocationPosition, string projectName)
        {
            var totalStopWatch = Stopwatch.StartNew();
            var stopWatch = Stopwatch.StartNew();

            var projectGrain = ProjectCodeProviderGrainFactory.GetGrain(projectName);
            Meausure("GetProjectGrain", stopWatch);

            var projectProviderWrapper = new ProjectGrainWrapper(projectGrain);
            Meausure("GetProjectGrainWrapper", stopWatch);

            var methodGrain = MethodEntityGrainFactory.GetGrain(methodDescriptor.Marshall());
            Meausure("GetMethodGrain", stopWatch);

            var methodEntity = (MethodEntity)await methodGrain.GetMethodEntity();
            Meausure("GetMethodEntity", stopWatch);

            var invocationNode = methodEntity.GetCallSiteByOrdinal(invocationPosition);
            Meausure("GetCallSiteByOrdinal", stopWatch);

            var result = await GetCalleesAsync(methodEntity, invocationNode, projectProviderWrapper);
            Meausure("GetCalleesAsync", stopWatch);

            Meausure("Total GetCalleesOrleansAsync", totalStopWatch);

            return result;
        }

        private static void Meausure(string label, Stopwatch timer)
        {
            timer.Stop();
            var timeMS = timer.ElapsedMilliseconds;
            var ticks = timer.ElapsedTicks;
            Debug.WriteLine("{0}: {1} ms, {2} ticks", label, timeMS, ticks);
            timer.Reset();
            timer.Start();
        }

        /// <summary>
        ///  Return the numnber of calls sites for a 
        /// </summary>
        /// <param name="methodDescriptor"></param>
        /// <param name="projectName"></param>
        /// <returns></returns>
        public static async Task<int> GetInvocationCountOrleansAsync(MethodDescriptor methodDescriptor, string projectName)
        {
            var projectGrain = ProjectCodeProviderGrainFactory.GetGrain(projectName);
            var projectProviderWrapper = new ProjectGrainWrapper(projectGrain);
            var methodGrain = MethodEntityGrainFactory.GetGrain(methodDescriptor.Marshall());
            var methodEntity = (MethodEntity)await methodGrain.GetMethodEntity();
            return methodEntity.PropGraph.CallNodes.Count();
        }

        /// <summary>
        /// Compute all the calless of this method entities
        /// </summary>
        /// <returns></returns>
        internal static async Task<ISet<MethodDescriptor>> GetCalleesAsync(MethodEntity methodEntity, ICodeProvider codeProvider)
        {
            var result = new HashSet<MethodDescriptor>();

            foreach (var callNode in methodEntity.PropGraph.CallNodes)
            {
                result.UnionWith(await GetCalleesAsync(methodEntity, callNode, codeProvider));
            }

            return result;
        }

        /// <summary>
        /// Computes all the potential callees for a particular method invocation
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        internal static async Task<ISet<MethodDescriptor>> GetCalleesAsync(MethodEntity methodEntity, AnalysisCallNode node, ICodeProvider codeProvider)
        {
            var stopWatch = Stopwatch.StartNew();

            ISet<MethodDescriptor> result;
            var calleesForNode = new HashSet<MethodDescriptor>();
            var invExp = methodEntity.PropGraph.GetInvocationInfo((AnalysisCallNode)node);

            Meausure("ME.PG.GetInvocationInfo", stopWatch);

            Contract.Assert(invExp != null);
            Contract.Assert(codeProvider != null);

            var calleeResult = await methodEntity.PropGraph.ComputeCalleesForNodeAsync(invExp, codeProvider);

            Meausure("ME.PG.ComputeCalleesForNode", stopWatch);

            calleesForNode.UnionWith(calleeResult);
            
            //calleesForNode.UnionWith(invExp.ComputeCalleesForNode(this.MethodEntity.PropGraph,this.codeProvider));

            result = calleesForNode;
            return result;
        }

        /// <summary>
        /// Generates a dictionary invocationNode -> potential callees
        /// This is used for example by the demo to get the caller / callee info
        /// </summary>
        /// <returns></returns>
        internal static async Task<IDictionary<AnalysisCallNode, ISet<MethodDescriptor>>> GetCalleesInfo(MethodEntity methodEntity, ICodeProvider codeProvider)
        {
            var calleesPerEntity = new Dictionary<AnalysisCallNode, ISet<MethodDescriptor>>();

            foreach (var calleeNode in methodEntity.PropGraph.CallNodes)
            {
                calleesPerEntity[calleeNode] = await GetCalleesAsync(methodEntity, calleeNode, codeProvider);
            }

            return calleesPerEntity;
        }
    }
}
