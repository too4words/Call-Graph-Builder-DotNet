// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using OrleansInterfaces;
using ReachingTypeAnalysis.Communication;

namespace ReachingTypeAnalysis.Analysis
{
    public interface IOrleansEntityState: IGrainState
    {
        MethodDescriptor MethodDescriptor { get; set; }
    }

    [StorageProvider(ProviderName = "TestStore")]
    //[Reentrant]
    internal class MethodEntityGrain : Grain<IOrleansEntityState>, IMethodEntityGrain
    {
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

            solutionGrain = SolutionGrainFactory.GetGrain("Solution");
	        // Shold not be null..
            if (this.State.Etag!= null)
            {
				this.codeProviderGrain = await solutionGrain.GetCodeProviderAsync(this.State.MethodDescriptor);
                this.codeProvider = new ProjectGrainWrapper(codeProviderGrain);
                // TODO: do we need to check and restore methodEntity
                // To restore the full entity state we need to save propagation data
                // or repropagate
				this.methodEntity = (MethodEntity)await codeProviderGrain.CreateMethodEntityAsync(this.State.MethodDescriptor);
                await solutionGrain.AddInstantiatedTypes(this.methodEntity.InstantiatedTypes);
            }
        }

        public Task<ISet<MethodDescriptor>> GetCalleesAsync()
        {
            var codeProvider = this.codeProvider;
            Contract.Assert(codeProvider != null);

            return CallGraphQueryInterface.CalleesAsync(this.methodEntity, codeProvider);
        }

        public Task<IDictionary<AnalysisCallNode, ISet<MethodDescriptor>>> GetCalleesInfoAsync()
        {
            return CallGraphQueryInterface.GetCalleesInfo(this.methodEntity, this.codeProvider);
        }

        public override Task OnDeactivateAsync()
        {
            Logger.Log(this.GetLogger(),"MethodEntityGrain", "OnDeactivate", "Deactivation for {0} ", this.GetPrimaryKeyString());
            this.methodEntity = null;
            return TaskDone.Done;
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
			await State.WriteStateAsync();
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

        public  Task<IEntity> GetMethodEntity()
        {
            // Contract.Assert(this.methodEntity != null);
            return Task.FromResult<IEntity>(this.methodEntity);
        }

        public async Task<PropagationEffects> PropagateAsync(PropagationKind propKind)
        {
            Logger.Log(this.GetLogger(),"MethodEntityGrain", "PropagateAsync", "Propagation for {0} ", this.methodEntity.MethodDescriptor);

            //var codeProvider = await ProjectGrainWrapper.CreateProjectGrainWrapperAsync(this.methodEntity.MethodDescriptor);
            var propagationEffects = await this.methodEntity.PropGraph.PropagateAsync(codeProvider);

            foreach (var calleeInfo in propagationEffects.CalleesInfo)
            {
                //  Add instanciated types! 
                /// Diego: Ben. This may not work well in parallel... 
                /// We need a different way to update this info
                calleeInfo.InstantiatedTypes = this.methodEntity.InstantiatedTypes;

                // TODO: This is because of the refactor
                if (calleeInfo is MethodCallInfo)
                {
					var methodCallInfo = calleeInfo as MethodCallInfo;
					methodCallInfo.ReceiverPossibleTypes = GetTypes(methodCallInfo.Receiver);
					methodCallInfo.PossibleCallees = await GetPossibleCalleesForMethodCallAsync(methodCallInfo, codeProvider);
				}
                else if (calleeInfo is DelegateCallInfo)
                {
                    var delegateCalleeInfo = calleeInfo as DelegateCallInfo;
					delegateCalleeInfo.ReceiverPossibleTypes = GetTypes(delegateCalleeInfo.Delegate);
                    delegateCalleeInfo.PossibleCallees = await GetPossibleCalleesForDelegateCallAsync(delegateCalleeInfo, codeProvider);
                }

                for (int i = 0; i < calleeInfo.Arguments.Count; i++)
                {
                    var arg = calleeInfo.Arguments[i];
                    var potentialTypes = arg != null ? GetTypes(arg, propKind) : new HashSet<TypeDescriptor>();
                    calleeInfo.ArgumentsPossibleTypes.Add(potentialTypes);
                }
            }

			if (this.methodEntity.ReturnVariable != null)
			{
				foreach (var callerContext in this.methodEntity.Callers)
				{
					var returnInfo = new ReturnInfo(this.methodEntity.MethodDescriptor, callerContext);
					returnInfo.ResultPossibleTypes = GetTypes(this.methodEntity.ReturnVariable);
					returnInfo.InstantiatedTypes = this.methodEntity.InstantiatedTypes;

					propagationEffects.CallersInfo.Add(returnInfo);
				}
			}
            Logger.Log(this.GetLogger(),"MethodEntityGrain", "PropagateAsync", "End Propagation for {0} ", this.methodEntity.MethodDescriptor);
            //this.methodEntity.Save(@"C:\Temp\"+this.methodEntity.MethodDescriptor.MethodName + @".dot");
            return propagationEffects;
        }

        public async Task<PropagationEffects> PropagateAsync(CallMessageInfo callMessageInfo)
        {
            if (!this.methodEntity.CanBeAnalized) return new PropagationEffects(new HashSet<CallInfo>(), false);

            if (this.methodEntity.ThisRef != null)
            {
                await this.methodEntity.PropGraph.DiffPropAsync(callMessageInfo.ReceiverPossibleTypes, this.methodEntity.ThisRef, callMessageInfo.PropagationKind);
            }

            for (var i = 0; i < this.methodEntity.ParameterNodes.Count; i++)
            {
                var parameterNode = this.methodEntity.ParameterNodes[i];

                if (parameterNode != null)
                {
                    await this.methodEntity.PropGraph.DiffPropAsync(callMessageInfo.ArgumentsPossibleTypes[i], parameterNode, callMessageInfo.PropagationKind);
                }
            }

            var context = new CallContext(callMessageInfo.Caller, callMessageInfo.LHS, callMessageInfo.CallNode);
            this.methodEntity.AddToCallers(context);


            var effects = await PropagateAsync(callMessageInfo.PropagationKind);
            return effects;
        }

        public async Task<PropagationEffects> PropagateAsync(ReturnMessageInfo returnMessageInfo)
        {
            //PropGraph.Add(lhs, retValues);
            await this.methodEntity.PropGraph.DiffPropAsync(returnMessageInfo.ResultPossibleTypes,returnMessageInfo.LHS, returnMessageInfo.PropagationKind);
            var effects = await PropagateAsync(returnMessageInfo.PropagationKind);
            return effects;
        }

		private async Task<ISet<MethodDescriptor>> GetPossibleCalleesForMethodCallAsync(MethodCallInfo methodCallInfo, ICodeProvider codeProvider)
		{
			var possibleCallees = new HashSet<MethodDescriptor>();

			// TODO: This is not good: one reason is that loads like b = this.f are not working
			// in a method m after call r.m() because only the value of r is passed and not all its structure (fields)

			if (methodCallInfo.Method.IsStatic)
			{
				// Static method call
				possibleCallees.Add(methodCallInfo.Method);
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

				if (methodCallInfo.ReceiverPossibleTypes.Count > 0)
				{
					foreach (var receiverType in methodCallInfo.ReceiverPossibleTypes)
					{
						// Given a method m and T find the most accurate implementation wrt to T
						// it can be T.m or the first super class implementing m
						var methodDescriptor = await codeProvider.FindMethodImplementationAsync(methodCallInfo.Method, receiverType);
						possibleCallees.Add(methodDescriptor);
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

		private async Task<ISet<MethodDescriptor>> GetPossibleCalleesForDelegateCallAsync(DelegateCallInfo delegateCallInfo, ICodeProvider codeProvider)
        {
            var possibleCallees = new HashSet<MethodDescriptor>();
			var possibleDelegateMethods = GetPossibleMethodsForDelegate(delegateCallInfo.Delegate);

			foreach (var method in possibleDelegateMethods)
            {
				if (method.IsStatic)
				{
					// Static method call
					possibleCallees.Add(method);
				}
				else
				{
					// Instance method call

					if (delegateCallInfo.ReceiverPossibleTypes.Count > 0)
					{
						foreach (var receiverType in delegateCallInfo.ReceiverPossibleTypes)
						{
							//var aMethod = delegateInstance.FindMethodImplementation(t);
							// Diego: Should I use : codeProvider.FindImplementation(delegateInstance, t);
							var callee = await codeProvider.FindMethodImplementationAsync(method, receiverType);
							possibleCallees.Add(callee);
						}
					}
					else
					{
						// We don't have any possibleType for the receiver,
						// so we just use the receiver's declared type to
						// identify the calle method implementation

						// if Count is 0, it is a delegate that do not came form an instance variable
						possibleCallees.Add(method);
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

        public Task<bool> IsInitialized()
        {
            return Task.FromResult(this.methodEntity != null);
        }
    }

    public class MethodEntityGrainWrapper : IMethodEntityWithPropagator
    {
        IMethodEntityGrain grainRef;
        public MethodEntityGrainWrapper(IMethodEntityGrain grainRef)
        {
            this.grainRef = grainRef;
        }
        public Task<PropagationEffects> PropagateAsync(PropagationKind propKind)
        {
            return this.grainRef.PropagateAsync(propKind);
        }

        public Task<PropagationEffects> PropagateAsync(CallMessageInfo callMessageInfo)
        {
            return this.grainRef.PropagateAsync(callMessageInfo);
        }

        public Task<PropagationEffects> PropagateAsync(ReturnMessageInfo returnMessageInfo)
        {
            return this.grainRef.PropagateAsync(returnMessageInfo);
        }

        public Task<bool> IsInitializedAsync()
        {
            return this.grainRef.IsInitialized();
        }

        public Task<IEntity> GetMethodEntityAsync()
        {
            return this.grainRef.GetMethodEntity();
        }

        public Task<ISet<MethodDescriptor>> GetCalleesAsync()
        {
            return this.grainRef.GetCalleesAsync();
        }

        public Task<IDictionary<AnalysisCallNode, ISet<MethodDescriptor>>> GetCalleesInfoAsync()
        {
            return this.grainRef.GetCalleesInfoAsync();
        }
    }
}
