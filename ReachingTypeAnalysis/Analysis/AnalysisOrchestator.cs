// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using OrleansInterfaces;
using ReachingTypeAnalysis.Communication;
using SolutionTraversal.Callgraph;

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
    internal static class AnalysisOrchestator
	{
		public static async Task AnalyzeAsync(MethodDescriptor method)
		{
			Logger.Instance.Log("AnalysisOrchestator", "AnalyzeAsync", "Analyzing {0} ", method);

			var entityDescriptor = new MethodEntityDescriptor(method);
			var methodEntityProc = await MethodEntityFactory.ObtainMethodEntityAsync(entityDescriptor);
            var propagationEffects = await methodEntityProc.PropagateAsync(PropagationKind.ADD_TYPES);
			await DoPropagationOnEffectsAsync(propagationEffects, PropagationKind.ADD_TYPES);
		}

		private static async Task DoPropagationOnEffectsAsync(PropagationEffects propagationEffects, PropagationKind propKind)
		{
			Logger.Instance.Log("AnalysisOrchestator", "DoPropagationOfEffects", "");

			await ProcessCalleesAsync(propagationEffects.CalleesInfo, propKind);

			if (propagationEffects.ResultChanged)
			{
				await ProcessReturnAsync(propagationEffects.CallersInfo, propKind);
			}
		}

		private static async Task ProcessCalleesAsync(IEnumerable<CallInfo> calleesInfo, PropagationKind propKind)
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

		private static async Task DispatchCallMessageForMethodCallAsync(MethodCallInfo methodCallInfo, PropagationKind propKind)
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

		private static async Task DispatchCallMessageForDelegateCallAsync(DelegateCallInfo delegateCallInfo, PropagationKind propKind)
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

		private static Task CreateAndSendCallMessageAsync(CallInfo callInfo, MethodDescriptor callee, PropagationKind propKind)
		{
			var callMessageInfo = new CallMessageInfo(callInfo.Caller, callee, callInfo.ReceiverPossibleTypes,
				callInfo.ArgumentsPossibleTypes, callInfo.InstantiatedTypes, callInfo.CallNode, callInfo.LHS, propKind);

			var source = new MethodEntityDescriptor(callInfo.Caller);
			var callerMessage = new CallerMessage(source, callMessageInfo);

			return AnalyzeCalleeAsync(callMessageInfo.Callee, callerMessage, propKind);
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
		private static async Task AnalyzeCalleeAsync(MethodDescriptor callee, CallerMessage callerMessage, PropagationKind propKind)
		{
			Logger.Instance.Log("AnalysisOrchestator", "AnalyzeCalleeAsync", "Analyzing call to {0} ", callee);

			var entityDescriptor = new MethodEntityDescriptor(callee);
			var methodEntityProc = await MethodEntityFactory.ObtainMethodEntityAsync(entityDescriptor);

            var propagationEffects = await methodEntityProc.PropagateAsync(callerMessage.CallMessageInfo);

            await DoPropagationOnEffectsAsync(propagationEffects, PropagationKind.ADD_TYPES);

            Logger.Instance.Log("AnalysisOrchestator", "AnalyzeCalleeAsync", "End Analyzing call to {0} ", callee);

		}

		private static async Task ProcessReturnAsync(IEnumerable<ReturnInfo> callersInfo, PropagationKind propKind)
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

		internal static async Task DispachReturnMessageAsync(ReturnInfo returnInfo, PropagationKind propKind)
		{
			await CreateAndSendReturnMessageAsync(returnInfo, propKind);
        }

		private static Task CreateAndSendReturnMessageAsync(ReturnInfo returnInfo, PropagationKind propKind)
		{
			var returnMessageInfo = new ReturnMessageInfo(returnInfo.CallerContext.Caller, returnInfo.Callee, returnInfo.ResultPossibleTypes, returnInfo.InstantiatedTypes,
				returnInfo.CallerContext.CallNode, returnInfo.CallerContext.LHS, propKind);

			var source = new MethodEntityDescriptor(returnInfo.Callee);
			var calleeMessage = new CalleeMessage(source, returnMessageInfo);

			return AnalyzeReturnAsync(returnMessageInfo.Caller, calleeMessage, propKind);
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
		private static async Task AnalyzeReturnAsync(MethodDescriptor caller, CalleeMessage calleeMessage, PropagationKind propKind)
		{
			Logger.Instance.Log("AnalysisOrchestator", "AnalyzeReturnAsync", "Analyzing return to {0} ", caller);

			var entityDescriptor = new MethodEntityDescriptor(caller);
			var methodEntityProc = await MethodEntityFactory.ObtainMethodEntityAsync(entityDescriptor);

			var propagationEffects = await methodEntityProc.PropagateAsync(calleeMessage.ReturnMessageInfo);
			await DoPropagationOnEffectsAsync(propagationEffects, propKind);

            Logger.Instance.Log("AnalysisOrchestator", "AnalyzeReturnAsync", "End Analyzing return to {0} ", caller);
		}

        


        

        internal static async Task<CallGraph<MethodDescriptor, LocationDescriptor>> GenerateCallGraph(ISolution solution)
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

                var currentProc = await MethodEntityFactory.ObtainMethodEntityAsync(new MethodEntityDescriptor(currentMethodDescriptor));
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

    internal static class CallGraphQueryInterface
    {
        /// <summary>
        /// Compute all the calless of this method entities
        /// </summary>
        /// <returns></returns>
        static async internal Task<IEnumerable<MethodDescriptor>> CalleesAsync(MethodEntity methodEntity, ICodeProvider codeProvider)
        {
            var result = new HashSet<MethodDescriptor>();
            foreach (var callNode in methodEntity.PropGraph.CallNodes)
            {
                result.UnionWith(await CalleesAsync(methodEntity, callNode, codeProvider));
            }
            return result;
        }

        /// <summary>
        /// Computes all the potential callees for a particular method invocation
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        static internal async Task<ISet<MethodDescriptor>> CalleesAsync(MethodEntity methodEntity, PropGraphNodeDescriptor node, ICodeProvider codeProvider)
        {
            ISet<MethodDescriptor> result;
            var calleesForNode = new HashSet<MethodDescriptor>();
            var invExp = methodEntity.PropGraph.GetInvocationInfo((AnalysisCallNode)node);
            Contract.Assert(invExp != null);
            Contract.Assert(codeProvider != null);

            var calleeResult = await methodEntity.PropGraph.ComputeCalleesForNodeAsync(invExp, codeProvider);
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
                calleesPerEntity[calleeNode] = await CalleesAsync(methodEntity, calleeNode, codeProvider);
            }
            return calleesPerEntity;
        }
    }

    internal static class MethodEntityFactory
    {
        public static bool UsingOrleans = false;
        public static IDictionary<MethodDescriptor, MethodEntityWithPropagator> methodEntities = new Dictionary<MethodDescriptor, MethodEntityWithPropagator>();
        internal static async Task<IMethodEntityWithPropagator> ObtainMethodEntityAsync(MethodEntityDescriptor entityDescriptor)
        {
            if (UsingOrleans)
            {
                var methodEntityGrainRef = await ObtainMethodEntityGrainAsync(entityDescriptor);
                return new MethodEntityGrainWrapper(methodEntityGrainRef);
            }
            else
            {
                MethodEntityWithPropagator methodEntityPropagator = null;
                lock (methodEntities)
                {
                    if (!methodEntities.TryGetValue(entityDescriptor.MethodDescriptor, out methodEntityPropagator))
                    {
                        methodEntityPropagator = new MethodEntityWithPropagator(entityDescriptor.MethodDescriptor);
                        methodEntities.Add(entityDescriptor.MethodDescriptor, methodEntityPropagator);
                    }
                }
                return methodEntityPropagator;
            }
        }
        internal static async Task<IMethodEntityGrain> ObtainMethodEntityGrainAsync(MethodEntityDescriptor entityDescriptor)
        {
            Logger.Instance.Log("AnalysisOrchestator", "CreateMethodEntityGrain", entityDescriptor);

            var methodEntityGrain = MethodEntityGrainFactory.GetGrain(entityDescriptor.MethodDescriptor.ToString());
            var methodEntity = await methodEntityGrain.GetMethodEntity();

            // check if the result is initialized
            if (methodEntity == null)
            {
                Logger.Instance.Log("AnalysisOrchestator", "CreateMethodEntityGrain", "MethodEntityGrain for {0} does not exist", entityDescriptor);
                Contract.Assert(entityDescriptor.MethodDescriptor != null);
                ////  methodEntity = await providerGrain.CreateMethodEntityAsync(grainDesc.MethodDescriptor);
                methodEntity = await MethodEntityFactory.CreateMethodEntityUsingGrainsAsync(entityDescriptor.MethodDescriptor);
                Contract.Assert(methodEntity != null);
                await methodEntityGrain.SetMethodEntityAsync(methodEntity, entityDescriptor);
                //await methodEntityGrain.SetDescriptor(entityDescriptor);
                return methodEntityGrain;
            }
            else
            {
                Logger.Instance.Log("AnalysisOrchestator", "CreateMethodEntityGrain", "MethodEntityGrain for {0} already exists", entityDescriptor);
                return methodEntityGrain;
            }
        }

        internal static async Task<MethodEntity> CreateMethodEntityUsingGrainsAsync(MethodDescriptor methodDescriptor)
        {
            Logger.Instance.Log("AnalysisOrchestator", "CreateMethodEntityUsingGrainsAsync", "Creating new MethodEntity for {0}", methodDescriptor);

            MethodEntity methodEntity = null;
            var solutionGrain = SolutionGrainFactory.GetGrain("Solution");
            IProjectCodeProviderGrain providerGrain = await solutionGrain.GetCodeProviderAsync(methodDescriptor);
            if (providerGrain == null)
            {
                var libraryMethodVisitor = new ReachingTypeAnalysis.Roslyn.LibraryMethodProcessor(methodDescriptor);
                methodEntity = libraryMethodVisitor.ParseLibraryMethod();
            }
            else
            {
                methodEntity = (MethodEntity)await providerGrain.CreateMethodEntityAsync(methodDescriptor);
            }
            return methodEntity;
        }
    }
}
