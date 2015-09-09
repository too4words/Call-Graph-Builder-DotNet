using Orleans;
using OrleansInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Analysis
{
	internal class OrleansMethodEntity
	{
		public static IMethodEntityGrain GetMethodEntityGrain(IGrainFactory grainFactory, MethodDescriptor methodDescriptor)
		{
			var primaryKey = methodDescriptor.Marshall();
            var grain = grainFactory.GetGrain<IMethodEntityGrain>(primaryKey);

#if COMPUTE_STATS
			grain = new MethodEntityGrainCallerWrapper(grain);
#endif
			return grain;
		}
	}
}
