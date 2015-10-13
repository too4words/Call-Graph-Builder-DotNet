using Orleans;
using ReachingTypeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansInterfaces
{
	public interface IRtaGrain : IGrainWithStringKey, IRtaManager
    {
		//Task<IEnumerable<string>> GetDrivesAsync();
	}
}
