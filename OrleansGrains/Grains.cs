using OrleansInterfaces;
using ReachingTypeAnalysis;
using System;
using System.Threading.Tasks;

namespace OrleansGrains
{
    //public class OrleansEntityDescriptor : /*Orleans.Grain,*/ IOrleansEntityDescriptor
    //{
    //    public string Descriptor { get; private set; }
    //    private Guid guid;
    //    public OrleansEntityDescriptor(Guid guid)
    //    {
    //        this.guid = guid;
    //    }

    //    public OrleansEntityDescriptor(string descriptor)
    //    {
    //        this.Descriptor = descriptor;
    //    }

    //    public Task<Guid> GetGuid()
    //    {
    //        return Task.FromResult(this.guid);
    //    }

    //    public MethodDescriptor MethodDescriptor { get; set; }

    //    public override bool Equals(object obj)
    //    {
    //        OrleansEntityDescriptor entityDescriptor = (OrleansEntityDescriptor)obj;
    //        return this.MethodDescriptor.Equals(entityDescriptor.MethodDescriptor);
    //    }
    //    public override int GetHashCode()
    //    {
    //        return this.MethodDescriptor.GetHashCode();
    //    }
    //}
    class Grain { }
}
