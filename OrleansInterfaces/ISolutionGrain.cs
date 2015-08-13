using Orleans;
using ReachingTypeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansInterfaces
{
    public interface ISolutionGrain : IGrainWithStringKey, ISolutionManager
    {
        Task SetSolutionPathAsync(string solutionPath);
        Task SetSolutionSourceAsync(string solutionSource);
		Task<IEnumerable<string>> GetDrives();
    }
}
