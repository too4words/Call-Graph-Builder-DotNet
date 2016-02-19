﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using ReachingTypeAnalysis.Analysis;
using System.IO;
using ReachingTypeAnalysis.Roslyn;
using Orleans;

namespace ReachingTypeAnalysis.Analysis
{
	public abstract class DummyProjectCodeProvider : IProjectCodeProvider
	{
		protected IOrchestratorManager orchestratorManager;

		protected DummyProjectCodeProvider(IOrchestratorManager orchestratorManager)
		{
			this.orchestratorManager = orchestratorManager;
		}

		public Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
		{
			return Task.FromResult(true);
		}

		public Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
		{
			return Task.FromResult(methodDescriptor);
		}

		public virtual Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			var libraryMethodVisitor = new LibraryMethodParser(methodDescriptor);
			var methodEntity = libraryMethodVisitor.ParseMethod();
			return Task.FromResult<IEntity>(methodEntity);
		}

		public Task<IEnumerable<MethodDescriptor>> GetRootsAsync(AnalysisRootKind rootKind = AnalysisRootKind.Default)
		{
			var result = new HashSet<MethodDescriptor>();
			return Task.FromResult(result.AsEnumerable());
		}

		public Task<IEnumerable<CodeGraphModel.FileResponse>> GetDocumentsAsync()
		{
			var result = new HashSet<CodeGraphModel.FileResponse>();
			return Task.FromResult(result.AsEnumerable());
		}

		public Task<IEnumerable<CodeGraphModel.FileResponse>> GetDocumentEntitiesAsync(string documentPath)
		{
			var result = new HashSet<CodeGraphModel.FileResponse>();
			return Task.FromResult(result.AsEnumerable());
		}

		public Task<CodeGraphModel.SymbolReference> GetDeclarationInfoAsync(MethodDescriptor methodDescriptor)
		{
			return Task.FromResult<CodeGraphModel.SymbolReference>(null);
		}

		public Task<CodeGraphModel.SymbolReference> GetInvocationInfoAsync(CallContext callContext)
		{
			return Task.FromResult<CodeGraphModel.SymbolReference>(null);
		}

		public abstract Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor);

		public abstract Task<IEnumerable<MethodDescriptor>> GetReachableMethodsAsync();

        public abstract Task<int> GetReachableMethodsCountAsync();

		public abstract Task<MethodDescriptor> GetRandomMethodAsync();

		public abstract Task<bool> IsReachableAsync(MethodDescriptor methodDescriptor);

		public Task<PropagationEffects> RemoveMethodAsync(MethodDescriptor methodToUpdate)
		{
			return Task.FromResult(new PropagationEffects(new HashSet<CallInfo>(), false));
		}

		public Task<PropagationEffects> AddMethodAsync(MethodDescriptor methodToAdd)
		{
			return Task.FromResult(new PropagationEffects(new HashSet<ReturnInfo>()));
		}

		public Task ReplaceDocumentSourceAsync(string source, string documentPath)
		{
			return TaskDone.Done;
		}

		public Task ReplaceDocumentAsync(string documentPath, string newDocumentPath = null)
		{
			return TaskDone.Done;
		}

		public Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			var result = new List<MethodModification>();
            return Task.FromResult(result.AsEnumerable());
        }

		public Task ReloadAsync()
		{
			return TaskDone.Done;
		}

		public Task<IEnumerable<TypeDescriptor>> GetCompatibleInstantiatedTypesAsync(TypeDescriptor type)
		{
			var result = new HashSet<TypeDescriptor>();
			result.Add(type);
			return Task.FromResult(result.AsEnumerable());
		}
	}
}
