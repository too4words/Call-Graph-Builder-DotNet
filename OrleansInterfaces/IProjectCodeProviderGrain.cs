using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using ReachingTypeAnalysis;
using ReachingTypeAnalysis.Communication;

namespace OrleansInterfaces
{
    public interface IProjectCodeProviderGrain : IGrainWithStringKey, IProjectCodeProvider, IObservableEntityGrain
    {
        //Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2);
        //Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor);
        //Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor);

        Task SetProjectPathAsync(string fullPath);
        Task SetProjectSourceAsync(string source);
		Task SetProjectFromTestAsync(string testName);
        Task ForceDeactivationAsync();
    }
}
