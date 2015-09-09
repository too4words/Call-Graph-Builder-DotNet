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
	//internal class MethodEntityGrainCallerWrapper : IMethodEntityGrain
	//{
	//	public Task<PropagationEffects> PropagateAsync(PropagationKind propKind)
	//	{

	//	}

	//	public Task<PropagationEffects> PropagateAsync(PropagationKind propKind, IEnumerable<PropGraphNodeDescriptor> reWorkSet)
	//	{

	//	}

	//	public Task<PropagationEffects> PropagateAsync(CallMessageInfo callMessageInfo)
	//	{

	//	}

	//	public Task<PropagationEffects> PropagateAsync(ReturnMessageInfo returnMessageInfo)
	//	{

	//	}

	//	public Task<bool> IsInitializedAsync()
	//	{

	//	}

	//	public Task<IEnumerable<TypeDescriptor>> GetInstantiatedTypesAsync()
	//	{

	//	}

	//	public Task<IEnumerable<CallContext>> GetCallersAsync()
	//	{

	//	}

	//	public Task<ISet<MethodDescriptor>> GetCalleesAsync()
	//	{

	//	}

	//	public Task<IDictionary<AnalysisCallNode, ISet<MethodDescriptor>>> GetCalleesInfoAsync()
	//	{

	//	}

	//	public Task<ISet<MethodDescriptor>> GetCalleesAsync(int invocationPosition)
	//	{

	//	}

	//	public Task<int> GetInvocationCountAsync()
	//	{

	//	}

	//	public Task<SymbolReference> GetDeclarationInfoAsync()
	//	{

	//	}

	//	public Task<IEnumerable<SymbolReference>> GetCallersDeclarationInfoAsync()
	//	{

	//	}

	//	public Task<IEnumerable<Annotation>> GetAnnotationsAsync()
	//	{

	//	}

	//	public Task<PropagationEffects> RemoveMethodAsync()
	//	{

	//	}

	//	public Task UnregisterCallerAsync(CallContext callContext)
	//	{

	//	}

	//	public Task ForceDeactivationAsync()
	//	{

	//	}
	//}
}
