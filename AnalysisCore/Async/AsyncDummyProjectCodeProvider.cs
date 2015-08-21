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

namespace ReachingTypeAnalysis.Roslyn
{
	public class AsyncDummyProjectCodeProvider : DummyProjectCodeProvider
	{
		private IDictionary<MethodDescriptor, IMethodEntityWithPropagator> methodEntities;

		public AsyncDummyProjectCodeProvider()
        {
			this.methodEntities = new Dictionary<MethodDescriptor, IMethodEntityWithPropagator>();
		}

		public override async Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			IMethodEntityWithPropagator result;

			if (!this.methodEntities.TryGetValue(methodDescriptor, out result))
			{
				var methodEntity = await this.CreateMethodEntityAsync(methodDescriptor) as MethodEntity;

				result = new MethodEntityWithPropagator(methodEntity, this);
				this.methodEntities.Add(methodDescriptor, result);
			}

			return result;
		}
	}
}
