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
    public interface IProjectCodeProviderGrain : IGrainWithStringKey, IProjectCodeProvider
    {
        //Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2);
        //Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor);
        //Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor);
        Task SetProjectPath(string fullPath);
        Task SetProjectSourceCode(string source);
        Task ForceDeactivationAsync();
    }
}
