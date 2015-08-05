using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;
using Microsoft.CodeAnalysis;
using Orleans;
using ReachingTypeAnalysis.Roslyn;

namespace ReachingTypeAnalysis.Analysis
{
	internal class OnDemandAsyncStrategy : IAnalysisStrategy
	{
		private IDictionary<MethodDescriptor, IMethodEntityWithPropagator> methodEntities;
        private ISolutionManager solutionManager;

		public OnDemandAsyncStrategy()
		{
			this.methodEntities = new Dictionary<MethodDescriptor, IMethodEntityWithPropagator>();            
		}

		public async Task<ISolutionManager> CreateFromSourceAsync(string source)
		{
			this.solutionManager = await AsyncSolutionManager.CreateFromSourceAsync(source);
			return this.solutionManager;
		}

		public async Task<ISolutionManager> CreateFromSolutionAsync(string solutionPath)
		{
			this.solutionManager = await AsyncSolutionManager.CreateFromSolutionAsync(solutionPath);
			return this.solutionManager;
		}

		public async Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			IMethodEntityWithPropagator methodEntityPropagator = null;

			lock (methodEntities)
			{
				if (!methodEntities.TryGetValue(methodDescriptor, out methodEntityPropagator))
				{
                    var codeProvider = solutionManager.GetProjectCodeProviderAsync(methodDescriptor.BaseDescriptor).Result;
					methodEntityPropagator = new MethodEntityWithPropagator(methodDescriptor, codeProvider);
					methodEntities.Add(methodDescriptor, methodEntityPropagator);
				}
			}

            await solutionManager.AddInstantiatedTypesAsync(await methodEntityPropagator.GetInstantiatedTypesAsync());
			return methodEntityPropagator;
		}
    }
}
