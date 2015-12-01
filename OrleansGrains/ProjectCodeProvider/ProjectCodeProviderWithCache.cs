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
using System.Collections.Concurrent;

namespace ReachingTypeAnalysis.Analysis
{
	internal class ProjectCodeProviderWithCache : IProjectCodeProvider
	{
		private IProjectCodeProvider codeProvider;
		private static ConcurrentDictionary<TypeDescriptor, ISet<TypeDescriptor>> IsSubTypeReply = new ConcurrentDictionary<TypeDescriptor, ISet<TypeDescriptor>>();
		private static ConcurrentDictionary<TypeDescriptor, ISet<TypeDescriptor>> IsSubTypeNegativeReply = new ConcurrentDictionary<TypeDescriptor, ISet<TypeDescriptor>>();
		private static ConcurrentDictionary<Tuple<MethodDescriptor, TypeDescriptor>, MethodDescriptor> FindMethodReply = new ConcurrentDictionary<Tuple<MethodDescriptor, TypeDescriptor>, MethodDescriptor>();

        static ProjectCodeProviderWithCache()
        {
        }

        internal ProjectCodeProviderWithCache(IProjectCodeProvider codeProvider)
		{
			this.codeProvider = codeProvider;
		}

		public async Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
		{
			if (IsSubTypeReply.ContainsKey(typeDescriptor1)
					&& IsSubTypeReply[typeDescriptor1].Contains(typeDescriptor2))
				return true;
			if (IsSubTypeNegativeReply.ContainsKey(typeDescriptor1)
				&& IsSubTypeNegativeReply[typeDescriptor1].Contains(typeDescriptor2))
				return false;

			var isSubType = await codeProvider.IsSubtypeAsync(typeDescriptor1, typeDescriptor2);
			if (isSubType)
			{
				AddToSubTypeCache(IsSubTypeReply, typeDescriptor1, typeDescriptor2);
			}
			else
			{
				AddToSubTypeCache(IsSubTypeNegativeReply, typeDescriptor1, typeDescriptor2);
			}

			return isSubType;
		}

		private void AddToSubTypeCache(ConcurrentDictionary<TypeDescriptor, ISet<TypeDescriptor>> typeCache,
					TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
		{
			ISet<TypeDescriptor> subTypes;
			if (typeCache.TryGetValue(typeDescriptor1, out subTypes))
			{
				subTypes.Add(typeDescriptor2);
			}
			else
			{
				subTypes = new HashSet<TypeDescriptor>();
				subTypes.Add(typeDescriptor2);
				typeCache.TryAdd(typeDescriptor1, subTypes);
			}
		}

		public async Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
		{
			MethodDescriptor reply;
			var key = new Tuple<MethodDescriptor, TypeDescriptor>(methodDescriptor, typeDescriptor);
			if (FindMethodReply.TryGetValue(key, out reply))
			{
				return reply;
			}
			reply = await codeProvider.FindMethodImplementationAsync(methodDescriptor, typeDescriptor);
			FindMethodReply.TryAdd(key, reply);
			return reply;
		}

		public Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			return codeProvider.CreateMethodEntityAsync(methodDescriptor);
		}

		public Task<IEnumerable<MethodDescriptor>> GetRootsAsync(AnalysisRootKind rootKind = AnalysisRootKind.Default)
		{
			return codeProvider.GetRootsAsync(rootKind);
		}

		public Task<IEnumerable<MethodDescriptor>> GetReachableMethodsAsync()
		{
			return codeProvider.GetReachableMethodsAsync();
		}

        public Task<int> GetReachableMethodsCountAsync()
        {
            return codeProvider.GetReachableMethodsCountAsync();
        }

        public Task<IEnumerable<FileResponse>> GetDocumentsAsync()
		{
			return codeProvider.GetDocumentsAsync();
		}

		public Task<IEnumerable<FileResponse>> GetDocumentEntitiesAsync(string filePath)
		{
			return codeProvider.GetDocumentEntitiesAsync(filePath);
		}

		public Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			return codeProvider.GetMethodEntityAsync(methodDescriptor);
		}

		public Task<PropagationEffects> RemoveMethodAsync(MethodDescriptor methodDescriptor)
		{
			return codeProvider.RemoveMethodAsync(methodDescriptor);
		}

		public Task ReplaceDocumentSourceAsync(string source, string documentPath)
		{
			return codeProvider.ReplaceDocumentSourceAsync(source, documentPath);
		}

		public Task ReplaceDocumentAsync(string documentPath, string newDocumentPath = null)
		{
			return codeProvider.ReplaceDocumentAsync(documentPath, newDocumentPath);
		}

		public Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			return codeProvider.GetModificationsAsync(modifiedDocuments);
		}

		public Task ReloadAsync()
		{
			return codeProvider.ReloadAsync();
		}

		public Task<PropagationEffects> AddMethodAsync(MethodDescriptor methodToAdd)
		{
			return codeProvider.AddMethodAsync(methodToAdd);
		}

		public Task<SymbolReference> GetDeclarationInfoAsync(MethodDescriptor methodDescriptor)
		{
			return codeProvider.GetDeclarationInfoAsync(methodDescriptor);
		}

		public Task<SymbolReference> GetInvocationInfoAsync(CallContext callContext)
		{
			return codeProvider.GetInvocationInfoAsync(callContext);
		}

		public Task<IEnumerable<TypeDescriptor>> GetCompatibleInstantiatedTypesAsync(TypeDescriptor type)
		{
			return codeProvider.GetCompatibleInstantiatedTypesAsync(type);
		}

		public Task<MethodDescriptor> GetRandomMethodAsync()
		{
			return codeProvider.GetRandomMethodAsync();
		}

		public Task<bool> IsReachable(MethodDescriptor methodDescriptor)
		{
			return codeProvider.IsReachable(methodDescriptor);
		}
	}
}
