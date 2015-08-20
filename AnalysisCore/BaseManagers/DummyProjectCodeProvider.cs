using Microsoft.CodeAnalysis;
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
using AnalysisCore.Roslyn;
using ReachingTypeAnalysis.Roslyn;

namespace ReachingTypeAnalysis.Analysis
{
	public abstract class DummyProjectCodeProvider : IProjectCodeProvider
	{
		public Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
		{
			return Task.FromResult(true);
		}

		public Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
		{
			return Task.FromResult(methodDescriptor);
		}

		public Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			var libraryMethodVisitor = new LibraryMethodParser(methodDescriptor);
			var methodEntity = libraryMethodVisitor.ParseMethod();
			return Task.FromResult<IEntity>(methodEntity);
		}

		public Task<IEnumerable<MethodDescriptor>> GetRootsAsync()
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

		public abstract Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor);


		public Task<PropagationEffects> RemoveMethodAsync(MethodDescriptor methodToUpdate)
		{
			return Task.FromResult(new PropagationEffects(new HashSet<CallInfo>(), false));
		}
	}
}
