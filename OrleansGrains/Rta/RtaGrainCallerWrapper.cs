using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Orleans;
using Orleans.Runtime;
using System.Threading;
using OrleansInterfaces;

namespace ReachingTypeAnalysis.Analysis
{
	internal class RtaGrainCallerWrapper : IRtaGrain
	{
		private IRtaGrain rtaGrain;

		internal RtaGrainCallerWrapper(IRtaGrain rtaGrain)
		{
			this.rtaGrain = rtaGrain;
		}

		private void SetRequestContext()
		{
			RequestContext.Set(StatsHelper.CALLER_ADDR_CONTEXT, StatsHelper.CreateMyIPAddrContext());
		}

		public Task AddInstantiatedTypesAsync(IEnumerable<TypeDescriptor> types)
		{
			this.SetRequestContext();
			return rtaGrain.AddInstantiatedTypesAsync(types);
		}

		public Task<ISet<TypeDescriptor>> GetInstantiatedTypesAsync()
		{
			this.SetRequestContext();
			return rtaGrain.GetInstantiatedTypesAsync();
		}	
	}
}
