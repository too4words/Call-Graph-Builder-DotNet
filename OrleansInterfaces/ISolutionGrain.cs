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
        Task SetSolutionPath(string solutionPath);
        Task SetSolutionSource(string solutionSource);
        //Task<IProjectCodeProviderGrain> GetCodeProviderAsync(ReachingTypeAnalysis.MethodDescriptor methodDescriptor);
        //
        //Task<IEnumerable<MethodDescriptor>> GetRoots();
        //Task AddInstantiatedTypes(IEnumerable<TypeDescriptor> types);
        //Task<ISet<TypeDescriptor>> InstantiatedTypes();
		Task<IEnumerable<string>> GetDrives();
    }
}
