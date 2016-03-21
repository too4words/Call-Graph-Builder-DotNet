using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using ReachingTypeAnalysis.Communication;
using ReachingTypeAnalysis.Roslyn;
using System.Diagnostics.Contracts;
using CodeGraphModel;
using Orleans;

namespace ReachingTypeAnalysis.Analysis
{
    /// <summary>
    /// This Classs plays the role of the MethodEntityProcessor but it is used by the OndemandAnalysisAsync
    /// and OnDemandOrleans
    /// This should replace the MethodEntityProcessor when we get rid of the MethodEntityProccesor
    /// </summary>
    internal class MethodEntityWithPropagator : IMethodEntityWithPropagator
    {
        private List<int> UpdateHistory = new List<int>();
        private MethodEntity methodEntity;
        private IProjectCodeProvider codeProvider;
        //private Queue<PropagationEffects> propagationEffectsToSend;
        //private Orleans.Runtime.Logger logger = GrainClient.Logger;

		///// <summary>
		///// This build a MethodEntityPropagator with a solution
		///// The solution provides its CodeProvicer
		///// </summary>
		///// <param name="methodDescriptor"></param>
		///// <param name="solutionManager"></param>
		//public MethodEntityWithPropagator(MethodDescriptor methodDescriptor, IProjectCodeProvider codeProvider)
		//{
		//	var methodDescriptorToSearch = methodDescriptor.BaseDescriptor;
		//
		//	this.codeProvider = codeProvider;
		//	this.propagationEffectsToSend = new Queue<PropagationEffects>();
		//	this.methodEntity = (MethodEntity)this.codeProvider.CreateMethodEntityAsync(methodDescriptorToSearch).Result;
		//
		//	//var providerEntity = ProjectCodeProvider.FindCodeProviderAndEntity(methodDescriptorToSearch, solutionManager.Solution).Result;
		//	//this.methodEntity = providerEntity.Item2;
		//	//this.codeProvider = providerEntity.Item1;
		//
		//	if (methodDescriptor.IsAnonymousDescriptor)
		//	{
		//		this.methodEntity = this.methodEntity.GetAnonymousMethodEntity((AnonymousMethodDescriptor)methodDescriptor);
		//	}            
		//}

        /// <summary>
        /// Creates the Propagator using directly an entity and a provider
        /// This can be used by the MethodEntityGrain
        /// </summary>
        /// <param name="methodEntity"></param>
        /// <param name="provider"></param>
        public MethodEntityWithPropagator(MethodEntity methodEntity, IProjectCodeProvider provider)
        {
            this.codeProvider = provider;
            this.methodEntity = methodEntity;
			//this.propagationEffectsToSend = new Queue<PropagationEffects>();
        }

		public Task<PropagationEffects> PropagateAsync(PropagationKind propKind, IEnumerable<PropGraphNodeDescriptor> reWorkSet)
		{
			Contract.Requires(reWorkSet != null);

			foreach(var node in reWorkSet) 
			{
				methodEntity.PropGraph.AddToWorkList(node);
			}

			return PropagateAsync(propKind);
		}
        public Task<PropagationEffects> PropagateAsync(PropagationKind propKind)
        {
            this.methodEntity.PropGraph.ResetUpdateCount();
            return InternalPropagateAsync(propKind);
        }

        private async Task<PropagationEffects> InternalPropagateAsync(PropagationKind propKind)
        {
			//Logger.LogS("MethodEntityProp", "PropagateAsync", "Propagation for {0} ", this.methodEntity.MethodDescriptor);
			Logger.Log("Propagating {0} to {1}", this.methodEntity.MethodDescriptor, propKind);
			
			// var codeProvider = await ProjectGrainWrapper.CreateProjectGrainWrapperAsync(this.methodEntity.MethodDescriptor);
			PropagationEffects propagationEffects = null;

			switch (propKind)
			{
				case PropagationKind.ADD_TYPES:
					propagationEffects = await this.methodEntity.PropGraph.PropagateAsync(codeProvider);
					break;

				case PropagationKind.REMOVE_TYPES:
					propagationEffects = await this.methodEntity.PropGraph.PropagateDeletionOfNodesAsync(codeProvider);
					break;

				default:
					throw new Exception("Unsupported propagation kind");
			}

            await this.PopulatePropagationEffectsInfo(propagationEffects, propKind);

            Logger.LogS("MethodEntityGrain", "PropagateAsync", "End Propagation for {0} ", this.methodEntity.MethodDescriptor);
            //this.methodEntity.Save(@"C:\Temp\"+this.methodEntity.MethodDescriptor.MethodName + @".dot");

            //if (propagationEffects.CalleesInfo.Count > 100)
            //{
            //	int index = 0;
            //	var count = propagationEffects.CalleesInfo.Count;
            //	var callessInfo = propagationEffects.CalleesInfo.ToList();
            //	propagationEffects.CalleesInfo = new HashSet<CallInfo>(callessInfo.GetRange(index, count > 100 ? 100 : count));
            //	propagationEffects.MoreEffectsToFetch = true;
            //
            //	while (count > 100)
            //	{
            //		count -= 100;
            //		index += 100;
            //
            //		var propEffect = new PropagationEffects(new HashSet<CallInfo>(callessInfo.GetRange(index, count > 100 ? 100 : count)), false);
            //
            //		if (count > 100)
            //		{
            //			propEffect.MoreEffectsToFetch = true;
            //		}
            //
            //		this.propagationEffectsToSend.Enqueue(propEffect);                   
            //	}
            //}
            //this.UpdateHistory.Add(this.methodEntity.PropGraph.UpdateCount);
            return propagationEffects;
        }

		private async Task PopulatePropagationEffectsInfo(PropagationEffects propagationEffects, PropagationKind propKind)
		{
            await this.PopulateCalleesInfo(propagationEffects.CalleesInfo);

			if (this.methodEntity.ReturnVariable != null || propKind == PropagationKind.REMOVE_TYPES)
			{
				this.PopulateCallersInfo(propagationEffects.CallersInfo);
			}
		}

        private async Task PopulateCalleesInfo(IEnumerable<CallInfo> calleesInfo)
        {
            foreach (var calleeInfo in calleesInfo)
            {
                //  Add instanciated types! 
                // Diego: Ben. This may not work well in parallel... 
                // We need a different way to update this info
                //calleeInfo.InstantiatedTypes = this.methodEntity.InstantiatedTypes;

                // TODO: This is because of the refactor
                if (calleeInfo is MethodCallInfo)
                {
                    var methodCallInfo = calleeInfo as MethodCallInfo;
					//methodCallInfo.ReceiverPossibleTypes = GetTypes(methodCallInfo.Receiver);;
					methodCallInfo.PossibleCallees = await this.GetPossibleCalleesForMethodCallAsync(methodCallInfo.Receiver, methodCallInfo.Method, codeProvider);
                }
                else if (calleeInfo is DelegateCallInfo)
                {
                    var delegateCalleeInfo = calleeInfo as DelegateCallInfo;
					//delegateCalleeInfo.ReceiverPossibleTypes = GetTypes(delegateCalleeInfo.Delegate);
					delegateCalleeInfo.PossibleCallees = await this.GetPossibleCalleesForDelegateCallAsync(delegateCalleeInfo.Delegate, codeProvider);
                }

				calleeInfo.ArgumentsPossibleTypes.Clear();

				for (int i = 0; i < calleeInfo.Arguments.Count; i++)
                {
                    var arg = calleeInfo.Arguments[i];
                    var potentialTypes = GetTypes(arg);
                    calleeInfo.ArgumentsPossibleTypes.Add(potentialTypes);
                }
            }
        }

		private void PopulateCallersInfo(ISet<ReturnInfo> callersInfo)
		{
			foreach (var callerContext in this.methodEntity.Callers)
			{
				var returnInfo = new ReturnInfo(this.methodEntity.MethodDescriptor, callerContext);
				returnInfo.ResultPossibleTypes = GetTypes(this.methodEntity.ReturnVariable);
				//returnInfo.InstantiatedTypes = this.methodEntity.InstantiatedTypes;

				callersInfo.Add(returnInfo);
			}
		}

		public async Task<PropagationEffects> PropagateAsync(CallMessageInfo callMessageInfo)
        {
            this.methodEntity.PropGraph.ResetUpdateCount();

            Logger.LogS("MethodEntityGrain", "PropagateAsync-call", "Propagation for {0} ", callMessageInfo.Callee);

			if (!this.methodEntity.CanBeAnalized)
			{
				var calleesInfo = new HashSet<CallInfo>();
                return new PropagationEffects(calleesInfo, false);
			}
			
            if (this.methodEntity.ThisRef != null)
            {
				var receiverPossibleTypes = new TypeDescriptor[] { callMessageInfo.ReceiverType };
                await this.methodEntity.PropGraph.DiffPropAsync(receiverPossibleTypes, this.methodEntity.ThisRef, callMessageInfo.PropagationKind);
            }

		    for (var i = 0; i < this.methodEntity.ParameterNodes.Count; i++)
            {
                var parameterNode = this.methodEntity.ParameterNodes[i];

                if (parameterNode != null)
                {
					//TODO: Hack. Remove later!
					ISet<TypeDescriptor> argumentPossibleTypes = null;

					if (i < callMessageInfo.ArgumentsPossibleTypes.Count)
					{
						argumentPossibleTypes = callMessageInfo.ArgumentsPossibleTypes[i];
					}
					else
					{
						argumentPossibleTypes = new HashSet<TypeDescriptor>();
						argumentPossibleTypes.Add(parameterNode.Type);
                    }

                    await this.methodEntity.PropGraph.DiffPropAsync(argumentPossibleTypes, parameterNode, callMessageInfo.PropagationKind);
                }
            }

            var context = new CallContext(callMessageInfo.Caller, callMessageInfo.LHS, callMessageInfo.CallNode);
            this.methodEntity.AddToCallers(context);

            var effects = await InternalPropagateAsync(callMessageInfo.PropagationKind);
            Logger.LogS("MethodEntityGrain", "PropagateAsync-call", "End Propagation for {0} ", callMessageInfo.Callee);
            return effects;
        }

        public async Task<PropagationEffects> PropagateAsync(ReturnMessageInfo returnMessageInfo)
        {
            this.methodEntity.PropGraph.ResetUpdateCount();

            Logger.LogS("MethodEntityGrain", "PropagateAsync-return", "Propagation for {0} ", returnMessageInfo.Caller);
            //PropGraph.Add(lhs, retValues);

            if (returnMessageInfo.LHS != null)
            {
                await this.methodEntity.PropGraph.DiffPropAsync(returnMessageInfo.ResultPossibleTypes, returnMessageInfo.LHS, returnMessageInfo.PropagationKind);
            }

            /// We need to recompute possible calless 
            var effects = await InternalPropagateAsync(returnMessageInfo.PropagationKind);
            Logger.LogS("MethodEntityGrain", "PropagateAsync-return", "End Propagation for {0} ", returnMessageInfo.Caller);

            if (returnMessageInfo.PropagationKind == PropagationKind.REMOVE_TYPES)
            {
                var invoInfo = from callNode in this.methodEntity.PropGraph.CallNodes
                               select this.methodEntity.PropGraph.GetInvocationInfo(callNode);

                await this.PopulateCalleesInfo(invoInfo);
            }

            return effects;
        }

        public async Task<ISet<MethodDescriptor>> GetCalleesAsync(int invocationPosition)
        {
            var invocationNode = this.GetCallSiteByOrdinal(invocationPosition);
            ISet<MethodDescriptor> result;
            var calleesForNode = new HashSet<MethodDescriptor>();
            var invExp = methodEntity.PropGraph.GetInvocationInfo(invocationNode);
            Contract.Assert(invExp != null);
            Contract.Assert(codeProvider != null);
            var calleeResult = await methodEntity.PropGraph.ComputeCalleesForNodeAsync(invExp, codeProvider);
            calleesForNode.UnionWith(calleeResult);
            result = calleesForNode;
            return result;
            // return await CallGraphQueryInterface.GetCalleesAsync(methodEntity, invocationNode, this.codeProvider);
        }

		internal AnalysisCallNode GetCallSiteByOrdinal(int invocationPosition)
		{
			foreach (var callNode in this.methodEntity.PropGraph.CallNodes)
			{
				if (callNode.InMethodPosition == invocationPosition)
				{
					return callNode;
				}
			}

			throw new ArgumentException();
			//return null;
		}

        public Task<int> GetInvocationCountAsync()
        {
            return Task.FromResult(methodEntity.PropGraph.CallNodes.Count());
        }

        private async Task<ISet<ResolvedCallee>> GetPossibleCalleesForMethodCallAsync(PropGraphNodeDescriptor receiver, MethodDescriptor method, IProjectCodeProvider codeProvider)
        {
            var possibleCallees = new HashSet<ResolvedCallee>();

			// TODO: This is not good: one reason is that loads like b = this.f are not working
			// in a method m after call r.m() because only the value of r is passed and not all its structure (fields)

			//if (methodCallInfo.IsConstructor || methodCallInfo.Method.IsStatic)
			//if (methodCallInfo.Method.IsStatic)
			//if (methodCallInfo.Method.IsStatic || !methodCallInfo.Method.IsVirtual)
			if (method.IsStatic)
			{
				// Static method call
				var resolvedCallee = new ResolvedCallee(method);

				possibleCallees.Add(resolvedCallee);
            }
			else if (!method.IsVirtual)
			{
				// Non-virtual method call
				var receiverPossibleTypes = this.GetTypes(receiver);

				if (receiverPossibleTypes.Count > 0)
				{
					foreach (var receiverType in receiverPossibleTypes)
					{
						var resolvedCallee = new ResolvedCallee(receiverType, method);

						possibleCallees.Add(resolvedCallee);
					}
				}
			}
			else
            {
				// Instance method call

				//// I need to compute all the callees
				//// In case of a deletion we can discard the deleted callee

				//// If callInfo.ReceiverPossibleTypes == {} it means that some info in missing => we should be conservative and use the instantiated types (RTA) 
				//// TODO: I make this False for testing what happens if we remove this

				//if (conservativeWithTypes && methodCallInfo.ReceiverPossibleTypes.Count == 0)
				//{
				//	// TO-DO: Should I fix the node in the receiver to show that is not loaded. Ideally I should use the declared type. 
				//	// Here I will use the already instantiated types

				//	foreach (var candidateTypeDescriptor in methodCallInfo.InstantiatedTypes)
				//	{
				//		var isSubtype = await codeProvider.IsSubtypeAsync(candidateTypeDescriptor, methodCallInfo.Receiver.Type);

				//		if (isSubtype)
				//		{
				//			methodCallInfo.ReceiverPossibleTypes.Add(candidateTypeDescriptor);
				//		}
				//	}
				//}

				var receiverPossibleTypes = this.GetTypes(receiver);

				if (receiverPossibleTypes.Count > 0)
                {
                    foreach (var receiverType in receiverPossibleTypes)
                    {
                        // Given a method m and T find the most accurate implementation wrt to T
                        // it can be T.m or the first super class implementing m
                        var methodDescriptor = await codeProvider.FindMethodImplementationAsync(method, receiverType);
						var resolvedCallee = new ResolvedCallee(receiverType, methodDescriptor);

                        possibleCallees.Add(resolvedCallee);
                    }
                }
                //else
                //{
                //    // We don't have any possibleType for the receiver,
                //    // so we just use the receiver's declared type to
                //    // identify the calle method implementation
                //    possibleCallees.Add(methodCallInfo.Method);
                //}
            }

            return possibleCallees;
        }

        private async Task<ISet<ResolvedCallee>> GetPossibleCalleesForDelegateCallAsync(DelegateVariableNode @delegate, IProjectCodeProvider codeProvider)
        {
            var possibleCallees = new HashSet<ResolvedCallee>();
            var possibleDelegateMethods = this.GetPossibleMethodsForDelegate(@delegate);

            foreach (var method in possibleDelegateMethods)
            {
                if (method.IsStatic)
                {
					// Static method call
					var resolvedCallee = new ResolvedCallee(method);

					possibleCallees.Add(resolvedCallee);
				}
                else
                {
                    // Instance method call
					var receiverPossibleTypes = this.GetTypes(@delegate);

					if (receiverPossibleTypes.Count > 0)
                    {
                        foreach (var receiverType in receiverPossibleTypes)
                        {
                            //var aMethod = delegateInstance.FindMethodImplementation(t);
                            // Diego: Should I use : codeProvider.FindImplementation(delegateInstance, t);
                            var callee = await codeProvider.FindMethodImplementationAsync(method, receiverType);
							var resolvedCallee = new ResolvedCallee(receiverType, callee);

							possibleCallees.Add(resolvedCallee);
                        }
                    }
                    else
                    {
						// We don't have any possibleType for the receiver,
						// so we just use the receiver's declared type to
						// identify the calle method implementation

						// if Count is 0, it is a delegate that do not came form an instance variable
						var receiverType = @delegate.Type;
						var resolvedCallee = new ResolvedCallee(receiverType, method);

						possibleCallees.Add(resolvedCallee);
					}
                }
            }

            return possibleCallees;
        }

        private ISet<TypeDescriptor> GetTypes(PropGraphNodeDescriptor analysisNode)
        {
            if (analysisNode != null)
            {
                return this.methodEntity.PropGraph.GetTypes(analysisNode);
            }
            else
            {
                return new HashSet<TypeDescriptor>();
            }
        }

        private ISet<TypeDescriptor> GetTypes(PropGraphNodeDescriptor node, PropagationKind prop)
        {
            switch (prop)
            {
                case PropagationKind.ADD_TYPES:
                    return GetTypes(node);
                case PropagationKind.REMOVE_TYPES:
                    return GetDeletedTypes(node);
                default:
                    return GetTypes(node);
            }
        }

        internal ISet<TypeDescriptor> GetDeletedTypes(PropGraphNodeDescriptor node)
        {
            if (node != null)
            {
                return this.methodEntity.PropGraph.GetDeletedTypes(node);
            }
            else
            {
                return new HashSet<TypeDescriptor>();
            }
        }

        internal ISet<MethodDescriptor> GetPossibleMethodsForDelegate(DelegateVariableNode node)
        {
            return this.methodEntity.PropGraph.GetDelegates(node);
        }

        public Task<IEntity> GetMethodEntityAsync()
        {
            // Contract.Assert(this.methodEntity != null);
            return Task.FromResult<IEntity>(this.methodEntity);
        }

        public async Task<ISet<MethodDescriptor>> GetCalleesAsync()
        {
			var result = new HashSet<MethodDescriptor>();

			foreach (var callNode in methodEntity.PropGraph.CallNodes)
			{
				var callees = await GetCalleesAsync(callNode);
                result.UnionWith(callees);
			}

			return result;
            // return CallGraphQueryInterface.GetCalleesAsync(this.methodEntity, codeProvider);
        }

        public async Task<IDictionary<AnalysisCallNode, ISet<MethodDescriptor>>> GetCalleesInfoAsync()
        {
			var calleesPerEntity = new Dictionary<AnalysisCallNode, ISet<MethodDescriptor>>();

			foreach (var calleeNode in this.methodEntity.PropGraph.CallNodes)
			{
				calleesPerEntity[calleeNode] = await GetCalleesAsync(calleeNode);
			}

			return calleesPerEntity;
            // return CallGraphQueryInterface.GetCalleesInfo(this.methodEntity, this.codeProvider);
        }

		private async Task<ISet<MethodDescriptor>> GetCalleesAsync(AnalysisCallNode node)
        {
			var result = new HashSet<MethodDescriptor>();
            var invExp = methodEntity.PropGraph.GetInvocationInfo(node);

            var calleeResult = await methodEntity.PropGraph.ComputeCalleesForNodeAsync(invExp, codeProvider);

            result.UnionWith(calleeResult);
            return result;
        }

        public Task<bool> IsInitializedAsync()
        {
            return Task.FromResult(this.methodEntity != null);
        }

        public Task<IEnumerable<CallContext>> GetCallersAsync()
        {
            return Task.FromResult(this.methodEntity.Callers.AsEnumerable());
        }

		public Task<IEnumerable<SymbolReference>> GetCallersDeclarationInfoAsync()
		{
			// TODO: BUG! The declaration info should be of the caller.
			//var references = from caller in this.methodEntity.Callers
			//				 select CodeGraphHelper.GetMethodReferenceInfo(caller.CallNode, this.methodEntity.DeclarationInfo);

			//var result = references.ToList().AsEnumerable();
			//return Task.FromResult(result);
			throw new NotImplementedException();
		}

		public Task<IEnumerable<TypeDescriptor>> GetInstantiatedTypesAsync()
        {
            return Task.FromResult(this.methodEntity.InstantiatedTypes.AsEnumerable());
        }

		public Task<SymbolReference> GetDeclarationInfoAsync()
		{
			return Task.FromResult(this.methodEntity.ReferenceInfo);
		}

		public Task<IEnumerable<Annotation>> GetAnnotationsAsync()
		{
			var result = new List<CodeGraphModel.Annotation>();
			//result.Add(this.methodEntity.DeclarationInfo);

			foreach (var callNode in this.methodEntity.PropGraph.CallNodes)
			{
				var invocationInfo = Roslyn.CodeGraphHelper.GetMethodInvocationInfo(this.methodEntity.MethodDescriptor, callNode);
				result.Add(invocationInfo);
			}

			foreach(var anonymousEntity in this.methodEntity.GetAnonymousMethodEntities())
			{
				foreach (var callNode in anonymousEntity.PropGraph.CallNodes)
				{
					var invocationInfo = Roslyn.CodeGraphHelper.GetMethodInvocationInfo(anonymousEntity.MethodDescriptor, callNode);
					invocationInfo.range = CodeGraphHelper.GetAbsoluteRange(invocationInfo.range, anonymousEntity.DeclarationInfo.range);
					result.Add(invocationInfo);
				}
			}

			return Task.FromResult(result.AsEnumerable());
		}
		
		public async Task<PropagationEffects> RemoveMethodAsync()
		{
			var calleesInfo = from callNode in this.methodEntity.PropGraph.CallNodes
							  select this.methodEntity.PropGraph.GetInvocationInfo(callNode);

			var propagagationEffecs = new PropagationEffects(calleesInfo, true);
			await this.PopulatePropagationEffectsInfo(propagagationEffecs, PropagationKind.REMOVE_TYPES);
			return propagagationEffecs;
		}

		//public async Task<PropagationEffects> UpdateMethodAsync(ISet<ReturnInfo> callersToUpdate)
		//{
		//	var propagagationEffecs = new PropagationEffects(callersToUpdate);
		//	await this.PopulatePropagationEffectsInfo(propagagationEffecs, PropagationKind.ADD_TYPES);
		//	return propagagationEffecs;
		//}

		public Task UnregisterCallerAsync(CallContext callContex)
		{
			this.methodEntity.RemoveFromCallers(callContex);
			return TaskDone.Done;
		}

		public Task UseDeclaredTypesForParameters()
		{
			foreach (var parameterNode in this.methodEntity.ParameterNodes)
			{
                this.methodEntity.PropGraph.Add(parameterNode, parameterNode.Type);
            }

			if (this.methodEntity.ThisRef != null)
			{
				this.methodEntity.PropGraph.Add(this.methodEntity.ThisRef, this.methodEntity.ThisRef.Type);
			}

			return TaskDone.Done;
		}

		public Task<MethodCalleesInfo> FixUnknownCalleesAsync()
		{
			var resolvedCallees = new HashSet<MethodDescriptor>();
			var unknownCallees = new HashSet<MethodDescriptor>();

			var result = methodEntity.PropGraph.FixUnknownCalleesAsync(codeProvider);
            return result;
		}

		//public Task UnregisterCalleeAsync(CallContext callContext)
		//{
		//	var invoInfo = this.methodEntity.PropGraph.GetInvocationInfo(callContext.CallNode);
		//	var receiverTypes = this.GetTypes(invoInfo.Receiver);
		//	//this.methodEntity.PropGraph.CallNodes.Remove(callContext.CallNode);
		//	return TaskDone.Done;
		//}

		//public Task<PropagationEffects> GetMoreEffects()
		//{
		//	return Task.FromResult(this.propagationEffectsToSend.Dequeue());
		//}
	}
}
