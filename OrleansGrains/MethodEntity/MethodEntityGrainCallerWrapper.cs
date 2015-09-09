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

namespace ReachingTypeAnalysis.Analysis
{
	[Serializable]
	internal class MethodEntityGrainCallerWrapper : IMethodEntityGrain
	{
		private IMethodEntityGrain methodEntityGrain;

		internal MethodEntityGrainCallerWrapper(IMethodEntityGrain methodEntityGrain)
		{
			this.methodEntityGrain = methodEntityGrain;
		}

		private void SetRequestContext()
		{
			RequestContext.Set(StatsHelper.CALLER_ADDR_CONTEXT, StatsHelper.CreateMyIPAddrContext());
		}

		public Task<PropagationEffects> PropagateAsync(PropagationKind propKind)
		{
			this.SetRequestContext();
			return methodEntityGrain.PropagateAsync(propKind);
		}

		public Task<PropagationEffects> PropagateAsync(PropagationKind propKind, IEnumerable<PropGraphNodeDescriptor> reWorkSet)
		{
			this.SetRequestContext();
			return methodEntityGrain.PropagateAsync(propKind, reWorkSet);
		}

		public Task<PropagationEffects> PropagateAsync(CallMessageInfo callMessageInfo)
		{
			this.SetRequestContext();
			return methodEntityGrain.PropagateAsync(callMessageInfo);
		}

		public Task<PropagationEffects> PropagateAsync(ReturnMessageInfo returnMessageInfo)
		{
			this.SetRequestContext();
			return methodEntityGrain.PropagateAsync(returnMessageInfo);
		}

		public Task<bool> IsInitializedAsync()
		{
			this.SetRequestContext();
			return methodEntityGrain.IsInitializedAsync();
		}

		public Task<IEnumerable<TypeDescriptor>> GetInstantiatedTypesAsync()
		{
			this.SetRequestContext();
			return methodEntityGrain.GetInstantiatedTypesAsync();
		}

		public Task<IEnumerable<CallContext>> GetCallersAsync()
		{
			this.SetRequestContext();
			return methodEntityGrain.GetCallersAsync();
		}

		public Task<ISet<MethodDescriptor>> GetCalleesAsync()
		{
			this.SetRequestContext();
			return methodEntityGrain.GetCalleesAsync();
		}

		public Task<IDictionary<AnalysisCallNode, ISet<MethodDescriptor>>> GetCalleesInfoAsync()
		{
			this.SetRequestContext();
			return methodEntityGrain.GetCalleesInfoAsync();
		}

		public Task<ISet<MethodDescriptor>> GetCalleesAsync(int invocationPosition)
		{
			this.SetRequestContext();
			return methodEntityGrain.GetCalleesAsync(invocationPosition);
		}

		public Task<int> GetInvocationCountAsync()
		{
			this.SetRequestContext();
			return methodEntityGrain.GetInvocationCountAsync();
		}

		public Task<SymbolReference> GetDeclarationInfoAsync()
		{
			this.SetRequestContext();
			return methodEntityGrain.GetDeclarationInfoAsync();
		}

		public Task<IEnumerable<SymbolReference>> GetCallersDeclarationInfoAsync()
		{
			this.SetRequestContext();
			return methodEntityGrain.GetCallersDeclarationInfoAsync();
		}

		public Task<IEnumerable<Annotation>> GetAnnotationsAsync()
		{
			this.SetRequestContext();
			return methodEntityGrain.GetAnnotationsAsync();
		}

		public Task<PropagationEffects> RemoveMethodAsync()
		{
			this.SetRequestContext();
			return methodEntityGrain.RemoveMethodAsync();
		}

		public Task UnregisterCallerAsync(CallContext callContext)
		{
			this.SetRequestContext();
			return methodEntityGrain.UnregisterCallerAsync(callContext);
		}

		public Task ForceDeactivationAsync()
		{
			this.SetRequestContext();
			return methodEntityGrain.ForceDeactivationAsync();
		}
	}
}
