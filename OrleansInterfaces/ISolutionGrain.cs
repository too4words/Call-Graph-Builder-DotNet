using Orleans;
using ReachingTypeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansInterfaces
{
	public interface ISolutionGrain : IGrainWithStringKey, ISolutionManager, IObservableEntityGrain, IEntityGrainObserver
    {
        Task SetSolutionPathAsync(string solutionPath);
        Task SetSolutionSourceAsync(string solutionSource);
		Task SetSolutionFromTestAsync(string testName); 
		Task ForceDeactivationAsync();
        Task<MethodDescriptor> GetMethodDescriptorByIndexAsync(int index);
		Task<EntityGrainStatus> GetStatusAsync();

		//Task<IEnumerable<string>> GetDrivesAsync();
	}
}
