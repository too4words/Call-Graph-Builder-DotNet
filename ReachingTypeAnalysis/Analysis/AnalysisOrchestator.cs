using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using ReachingTypeAnalysis.Communication;
using System.Diagnostics.Contracts;
using OrleansInterfaces;

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
	class AnalysisOrchestator
	{
		public static async Task AnalyzeAsync(MethodDescriptor method)
		{
			Logger.Instance.Log("AnalysisOrchestator", "AnalyzeAsync", "Analyzing {0} ", method);

			var entityDescriptor = new OrleansEntityDescriptor(method);
			var methodEntityGrain = await OrleansDispatcher.CreateMethodEntityGrain(entityDescriptor);

			await DoPropagationOnGrain(method, methodEntityGrain, PropagationKind.ADD_TYPES);
		}

		private static async Task DoPropagationOnGrain(MethodDescriptor method, IMethodEntityGrain methodEntityGrain, PropagationKind propKind)
		{
			Logger.Instance.Log("AnalysisOrchestator", "DoPropagationOnGrain", "Analyzing {0} ", method);

			var propagationEffects = await methodEntityGrain.PropagateAsync(propKind);
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

			var source = new OrleansEntityDescriptor(callInfo.Caller);
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

			var entityDescriptor = new OrleansEntityDescriptor(callee);
			var methodEntityGrain = await OrleansDispatcher.CreateMethodEntityGrain(entityDescriptor);

			await methodEntityGrain.UpdateMethodArgumentsAsync(callerMessage.CallMessageInfo.ReceiverPossibleTypes, callerMessage.CallMessageInfo.ArgumentsPossibleTypes, propKind);
			await DoPropagationOnGrain(callee, methodEntityGrain, propKind);
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

			var source = new OrleansEntityDescriptor(returnInfo.Callee);
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

			var entityDescriptor = new OrleansEntityDescriptor(caller);
			var methodEntityGrain = await OrleansDispatcher.CreateMethodEntityGrain(entityDescriptor);

			await methodEntityGrain.UpdateMethodReturnAsync(calleeMessage.ReturnMessageInfo.ResultPossibleTypes, calleeMessage.ReturnMessageInfo.LHS, propKind);
			await DoPropagationOnGrain(caller, methodEntityGrain, propKind);
		}
	}
}
