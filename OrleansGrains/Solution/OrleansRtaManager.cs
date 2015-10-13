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

    internal class OrleansRtaManager : RtaManager
    {
        public static IRtaGrain GetRtaGrain(IGrainFactory grainFactory)
        {
            var grain = grainFactory.GetGrain<IRtaGrain>("RTA");

#if COMPUTE_STATS
			grain = new RtaGrainCallerWrapper(grain);
#endif
            return grain;
        }

    }
}
