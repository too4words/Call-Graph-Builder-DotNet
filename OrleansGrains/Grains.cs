using OrleansInterfaces;
using ReachingTypeAnalysis;
using System;
using System.Threading.Tasks;

namespace OrleansGrains
{
    public class OrleansEntityDescriptor : Orleans.Grain, IOrleansEntityDescriptor
    {
        private Guid guid;
        public OrleansEntityDescriptor(Guid guid)
        {
            this.guid = guid;
        }

        public Task<Guid> GetGuid()
        {
            return Task.FromResult(this.guid);
        }

        public MethodDescriptor MethodDescriptor { get; set; }
    }
}
