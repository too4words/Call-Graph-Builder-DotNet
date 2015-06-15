using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansInterfaces
{
    public interface ISolutionGrain : IGrainWithStringKey
    {
        Task SetSolution(string solutionPath);
        Task<IProjectCodeProviderGrain> GetCodeProviderAsync(ReachingTypeAnalysis.MethodDescriptor methodDescriptor);
    }
}
