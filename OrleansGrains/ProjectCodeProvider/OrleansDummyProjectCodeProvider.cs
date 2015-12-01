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
using ReachingTypeAnalysis.Roslyn;
using Orleans;
using OrleansInterfaces;

namespace ReachingTypeAnalysis.Analysis
{
	public class OrleansDummyProjectCodeProvider : DummyProjectCodeProvider
	{
		private IGrainFactory grainFactory;
		private ISet<MethodDescriptor> reachableMethods;

		public OrleansDummyProjectCodeProvider(IGrainFactory grainFactory)
		{
			this.grainFactory = grainFactory;
			this.reachableMethods = new HashSet<MethodDescriptor>();
		}

		public override Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			reachableMethods.Add(methodDescriptor);
			return base.CreateMethodEntityAsync(methodDescriptor);
		}

		public override Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			var methodEntityGrain = OrleansMethodEntity.GetMethodEntityGrain(grainFactory, methodDescriptor);
			return Task.FromResult<IMethodEntityWithPropagator>(methodEntityGrain);
		}

		public override Task<IEnumerable<MethodDescriptor>> GetReachableMethodsAsync()
		{
			return Task.FromResult(reachableMethods.AsEnumerable());
		}

        public override Task<int> GetReachableMethodsCountAsync()
        {
            return Task.FromResult(reachableMethods.Count);
        }

		public override Task<MethodDescriptor> GetRandomMethodAsync()
		{
			var random = new Random();
			var randomIndex = random.Next(reachableMethods.Count);
			var method = reachableMethods.ElementAt(randomIndex);

			return Task.FromResult(method);
		}

		public override Task<bool> IsReachableAsync(MethodDescriptor methodDescriptor)
		{
			return Task.FromResult(reachableMethods.Contains(methodDescriptor));
		}
	}
}
