using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OrleansInterfaces;
using System.Diagnostics.Contracts;

namespace ReachingTypeAnalysis.Analysis
{
	internal class AnalysisStrategy : IAnalysisStrategy
	{
		private IDictionary<MethodDescriptor, IMethodEntityWithPropagator> methodEntities;

		public AnalysisStrategy()
		{
			this.methodEntities = new Dictionary<MethodDescriptor, IMethodEntityWithPropagator>();
		}

		public Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			IMethodEntityWithPropagator methodEntityPropagator = null;

			lock (methodEntities)
			{
				if (!methodEntities.TryGetValue(methodDescriptor, out methodEntityPropagator))
				{
					methodEntityPropagator = new MethodEntityWithPropagator(methodDescriptor);
					methodEntities.Add(methodDescriptor, methodEntityPropagator);
				}
			}

			return Task.FromResult(methodEntityPropagator);
		}
	}

	internal class OrleansAnalysisStrategy : IAnalysisStrategy
	{
		public async Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			var methodEntityGrain = await GetMethodEntityGrainAsync(methodDescriptor);
			return new MethodEntityGrainWrapper(methodEntityGrain);
		}

		private async Task<IMethodEntityGrain> GetMethodEntityGrainAsync(MethodDescriptor methodDescriptor)
		{
			Logger.Instance.Log("AnalysisOrchestator", "CreateMethodEntityGrain", methodDescriptor);

			var methodEntityGrain = MethodEntityGrainFactory.GetGrain(methodDescriptor.ToString());
			var methodEntity = await methodEntityGrain.GetMethodEntity();

			// check if the result is initialized
			if (methodEntity == null)
			{
				Logger.Instance.Log("AnalysisOrchestator", "CreateMethodEntityGrain", "MethodEntityGrain for {0} does not exist", methodDescriptor);
				Contract.Assert(methodDescriptor != null);
				////  methodEntity = await providerGrain.CreateMethodEntityAsync(grainDesc.MethodDescriptor);
				methodEntity = await CreateMethodEntityUsingGrainsAsync(methodDescriptor);
				Contract.Assert(methodEntity != null);
				await methodEntityGrain.SetMethodEntityAsync(methodEntity, methodDescriptor);
				//await methodEntityGrain.SetDescriptor(entityDescriptor);
				return methodEntityGrain;
			}
			else
			{
				Logger.Instance.Log("AnalysisOrchestator", "CreateMethodEntityGrain", "MethodEntityGrain for {0} already exists", methodDescriptor);
				return methodEntityGrain;
			}
		}

		private async Task<MethodEntity> CreateMethodEntityUsingGrainsAsync(MethodDescriptor methodDescriptor)
		{
			Logger.Instance.Log("AnalysisOrchestator", "CreateMethodEntityUsingGrainsAsync", "Creating new MethodEntity for {0}", methodDescriptor);

			MethodEntity methodEntity = null;
			var solutionGrain = SolutionGrainFactory.GetGrain("Solution");
			var providerGrain = await solutionGrain.GetCodeProviderAsync(methodDescriptor);
			
			if (providerGrain == null)
			{
				var libraryMethodVisitor = new ReachingTypeAnalysis.Roslyn.LibraryMethodProcessor(methodDescriptor);
				methodEntity = libraryMethodVisitor.ParseLibraryMethod();
			}
			else
			{
				methodEntity = (MethodEntity)await providerGrain.CreateMethodEntityAsync(methodDescriptor);
			}

			return methodEntity;
		}
	}
}
