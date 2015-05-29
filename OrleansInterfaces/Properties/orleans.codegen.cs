//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.0
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
#if !EXCLUDE_CODEGEN
#pragma warning disable 162
#pragma warning disable 219
#pragma warning disable 414
#pragma warning disable 649
#pragma warning disable 693
#pragma warning disable 1591
#pragma warning disable 1998

namespace OrleansInterfaces
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.IO;
    using System.Collections.Generic;
    using Orleans;
    using Orleans.Runtime;
    using ReachingTypeAnalysis;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.0.8.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public class OrleansEntityDescriptorFactory
    {
        

                        public static OrleansInterfaces.IOrleansEntityDescriptor GetGrain(System.Guid primaryKey)
                        {
                            return Cast(global::Orleans.CodeGeneration.GrainFactoryBase.MakeGrainReferenceInternal(typeof(OrleansInterfaces.IOrleansEntityDescriptor), 1545344342, primaryKey));
                        }

                        public static OrleansInterfaces.IOrleansEntityDescriptor GetGrain(System.Guid primaryKey, string grainClassNamePrefix)
                        {
                            return Cast(global::Orleans.CodeGeneration.GrainFactoryBase.MakeGrainReferenceInternal(typeof(OrleansInterfaces.IOrleansEntityDescriptor), 1545344342, primaryKey, grainClassNamePrefix));
                        }

            public static OrleansInterfaces.IOrleansEntityDescriptor Cast(global::Orleans.Runtime.IAddressable grainRef)
            {
                
                return OrleansEntityDescriptorReference.Cast(grainRef);
            }
        
        [System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.0.8.0")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
        [System.SerializableAttribute()]
        [global::Orleans.CodeGeneration.GrainReferenceAttribute("OrleansInterfaces.OrleansInterfaces.IOrleansEntityDescriptor")]
        internal class OrleansEntityDescriptorReference : global::Orleans.Runtime.GrainReference, global::Orleans.Runtime.IAddressable, OrleansInterfaces.IOrleansEntityDescriptor
        {
            

            public static OrleansInterfaces.IOrleansEntityDescriptor Cast(global::Orleans.Runtime.IAddressable grainRef)
            {
                
                return (OrleansInterfaces.IOrleansEntityDescriptor) global::Orleans.Runtime.GrainReference.CastInternal(typeof(OrleansInterfaces.IOrleansEntityDescriptor), (global::Orleans.Runtime.GrainReference gr) => { return new OrleansEntityDescriptorReference(gr);}, grainRef, 1545344342);
            }
            
            protected internal OrleansEntityDescriptorReference(global::Orleans.Runtime.GrainReference reference) : 
                    base(reference)
            {
            }
            
            protected internal OrleansEntityDescriptorReference(SerializationInfo info, StreamingContext context) : 
                    base(info, context)
            {
            }
            
            protected override int InterfaceId
            {
                get
                {
                    return 1545344342;
                }
            }
            
            public override string InterfaceName
            {
                get
                {
                    return "OrleansInterfaces.OrleansInterfaces.IOrleansEntityDescriptor";
                }
            }
            
            [global::Orleans.CodeGeneration.CopierMethodAttribute()]
            public static object _Copier(object original)
            {
                OrleansEntityDescriptorReference input = ((OrleansEntityDescriptorReference)(original));
                return ((OrleansEntityDescriptorReference)(global::Orleans.Runtime.GrainReference.CopyGrainReference(input)));
            }
            
            [global::Orleans.CodeGeneration.SerializerMethodAttribute()]
            public static void _Serializer(object original, global::Orleans.Serialization.BinaryTokenStreamWriter stream, System.Type expected)
            {
                OrleansEntityDescriptorReference input = ((OrleansEntityDescriptorReference)(original));
                global::Orleans.Runtime.GrainReference.SerializeGrainReference(input, stream, expected);
            }
            
            [global::Orleans.CodeGeneration.DeserializerMethodAttribute()]
            public static object _Deserializer(System.Type expected, global::Orleans.Serialization.BinaryTokenStreamReader stream)
            {
                return OrleansEntityDescriptorReference.Cast(((global::Orleans.Runtime.GrainReference)(global::Orleans.Runtime.GrainReference.DeserializeGrainReference(expected, stream))));
            }
            
            public override bool IsCompatible(int interfaceId)
            {
                return ((interfaceId == this.InterfaceId) 
                            || (interfaceId == -1097320095));
            }
            
            protected override string GetMethodName(int interfaceId, int methodId)
            {
                return OrleansEntityDescriptorMethodInvoker.GetMethodName(interfaceId, methodId);
            }
            
            System.Threading.Tasks.Task<System.Guid> OrleansInterfaces.IOrleansEntityDescriptor.GetGuid()
            {

                return base.InvokeMethodAsync<System.Guid>(679340007, null );
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.0.8.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    [global::Orleans.CodeGeneration.MethodInvokerAttribute("OrleansInterfaces.OrleansInterfaces.IOrleansEntityDescriptor", 1545344342)]
    internal class OrleansEntityDescriptorMethodInvoker : global::Orleans.CodeGeneration.IGrainMethodInvoker
    {
        
        int global::Orleans.CodeGeneration.IGrainMethodInvoker.InterfaceId
        {
            get
            {
                return 1545344342;
            }
        }
        
        global::System.Threading.Tasks.Task<object> global::Orleans.CodeGeneration.IGrainMethodInvoker.Invoke(global::Orleans.Runtime.IAddressable grain, int interfaceId, int methodId, object[] arguments)
        {

            try
            {                    if (grain == null) throw new System.ArgumentNullException("grain");
                switch (interfaceId)
                {
                    case 1545344342:  // IOrleansEntityDescriptor
                        switch (methodId)
                        {
                            case 679340007: 
                                return ((IOrleansEntityDescriptor)grain).GetGuid().ContinueWith(t => {if (t.Status == System.Threading.Tasks.TaskStatus.Faulted) throw t.Exception; return (object)t.Result; });
                            default: 
                            throw new NotImplementedException("interfaceId="+interfaceId+",methodId="+methodId);
                        }case -1097320095:  // IGrainWithGuidKey
                        switch (methodId)
                        {
                            default: 
                            throw new NotImplementedException("interfaceId="+interfaceId+",methodId="+methodId);
                        }
                    default:
                        throw new System.InvalidCastException("interfaceId="+interfaceId);
                }
            }
            catch(Exception ex)
            {
                var t = new System.Threading.Tasks.TaskCompletionSource<object>();
                t.SetException(ex);
                return t.Task;
            }
        }
        
        public static string GetMethodName(int interfaceId, int methodId)
        {

            switch (interfaceId)
            {
                
                case 1545344342:  // IOrleansEntityDescriptor
                    switch (methodId)
                    {
                        case 679340007:
                            return "GetGuid";
                    
                        default: 
                            throw new NotImplementedException("interfaceId="+interfaceId+",methodId="+methodId);
                    }
                case -1097320095:  // IGrainWithGuidKey
                    switch (methodId)
                    {
                        
                        default: 
                            throw new NotImplementedException("interfaceId="+interfaceId+",methodId="+methodId);
                    }

                default:
                    throw new System.InvalidCastException("interfaceId="+interfaceId);
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.0.8.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public class MethodEntityGrainFactory
    {
        

                        public static OrleansInterfaces.IMethodEntityGrain GetGrain(System.Guid primaryKey)
                        {
                            return Cast(global::Orleans.CodeGeneration.GrainFactoryBase.MakeGrainReferenceInternal(typeof(OrleansInterfaces.IMethodEntityGrain), 898358989, primaryKey));
                        }

                        public static OrleansInterfaces.IMethodEntityGrain GetGrain(System.Guid primaryKey, string grainClassNamePrefix)
                        {
                            return Cast(global::Orleans.CodeGeneration.GrainFactoryBase.MakeGrainReferenceInternal(typeof(OrleansInterfaces.IMethodEntityGrain), 898358989, primaryKey, grainClassNamePrefix));
                        }

            public static OrleansInterfaces.IMethodEntityGrain Cast(global::Orleans.Runtime.IAddressable grainRef)
            {
                
                return MethodEntityGrainReference.Cast(grainRef);
            }
        
        [System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.0.8.0")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
        [System.SerializableAttribute()]
        [global::Orleans.CodeGeneration.GrainReferenceAttribute("OrleansInterfaces.OrleansInterfaces.IMethodEntityGrain")]
        internal class MethodEntityGrainReference : global::Orleans.Runtime.GrainReference, global::Orleans.Runtime.IAddressable, OrleansInterfaces.IMethodEntityGrain
        {
            

            public static OrleansInterfaces.IMethodEntityGrain Cast(global::Orleans.Runtime.IAddressable grainRef)
            {
                
                return (OrleansInterfaces.IMethodEntityGrain) global::Orleans.Runtime.GrainReference.CastInternal(typeof(OrleansInterfaces.IMethodEntityGrain), (global::Orleans.Runtime.GrainReference gr) => { return new MethodEntityGrainReference(gr);}, grainRef, 898358989);
            }
            
            protected internal MethodEntityGrainReference(global::Orleans.Runtime.GrainReference reference) : 
                    base(reference)
            {
            }
            
            protected internal MethodEntityGrainReference(SerializationInfo info, StreamingContext context) : 
                    base(info, context)
            {
            }
            
            protected override int InterfaceId
            {
                get
                {
                    return 898358989;
                }
            }
            
            public override string InterfaceName
            {
                get
                {
                    return "OrleansInterfaces.OrleansInterfaces.IMethodEntityGrain";
                }
            }
            
            [global::Orleans.CodeGeneration.CopierMethodAttribute()]
            public static object _Copier(object original)
            {
                MethodEntityGrainReference input = ((MethodEntityGrainReference)(original));
                return ((MethodEntityGrainReference)(global::Orleans.Runtime.GrainReference.CopyGrainReference(input)));
            }
            
            [global::Orleans.CodeGeneration.SerializerMethodAttribute()]
            public static void _Serializer(object original, global::Orleans.Serialization.BinaryTokenStreamWriter stream, System.Type expected)
            {
                MethodEntityGrainReference input = ((MethodEntityGrainReference)(original));
                global::Orleans.Runtime.GrainReference.SerializeGrainReference(input, stream, expected);
            }
            
            [global::Orleans.CodeGeneration.DeserializerMethodAttribute()]
            public static object _Deserializer(System.Type expected, global::Orleans.Serialization.BinaryTokenStreamReader stream)
            {
                return MethodEntityGrainReference.Cast(((global::Orleans.Runtime.GrainReference)(global::Orleans.Runtime.GrainReference.DeserializeGrainReference(expected, stream))));
            }
            
            public override bool IsCompatible(int interfaceId)
            {
                return ((interfaceId == this.InterfaceId) 
                            || (interfaceId == -1097320095));
            }
            
            protected override string GetMethodName(int interfaceId, int methodId)
            {
                return MethodEntityGrainMethodInvoker.GetMethodName(interfaceId, methodId);
            }
            
            System.Threading.Tasks.Task<OrleansInterfaces.IOrleansEntityDescriptor> OrleansInterfaces.IMethodEntityGrain.GetDescriptor()
            {

                return base.InvokeMethodAsync<OrleansInterfaces.IOrleansEntityDescriptor>(331950094, null );
            }
            
            System.Threading.Tasks.Task OrleansInterfaces.IMethodEntityGrain.ReceiveMessageAsync(OrleansInterfaces.IOrleansEntityDescriptor @source, ReachingTypeAnalysis.IMessage @message)
            {

                return base.InvokeMethodAsync<object>(-1611393113, new object[] {@source is global::Orleans.Grain ? @source.AsReference<OrleansInterfaces.IOrleansEntityDescriptor>() : @source, @message} );
            }
            
            System.Threading.Tasks.Task<bool> OrleansInterfaces.IMethodEntityGrain.IsInitialized()
            {

                return base.InvokeMethodAsync<System.Boolean>(-1831544886, null );
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.0.8.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    [global::Orleans.CodeGeneration.MethodInvokerAttribute("OrleansInterfaces.OrleansInterfaces.IMethodEntityGrain", 898358989)]
    internal class MethodEntityGrainMethodInvoker : global::Orleans.CodeGeneration.IGrainMethodInvoker
    {
        
        int global::Orleans.CodeGeneration.IGrainMethodInvoker.InterfaceId
        {
            get
            {
                return 898358989;
            }
        }
        
        global::System.Threading.Tasks.Task<object> global::Orleans.CodeGeneration.IGrainMethodInvoker.Invoke(global::Orleans.Runtime.IAddressable grain, int interfaceId, int methodId, object[] arguments)
        {

            try
            {                    if (grain == null) throw new System.ArgumentNullException("grain");
                switch (interfaceId)
                {
                    case 898358989:  // IMethodEntityGrain
                        switch (methodId)
                        {
                            case 331950094: 
                                return ((IMethodEntityGrain)grain).GetDescriptor().ContinueWith(t => {if (t.Status == System.Threading.Tasks.TaskStatus.Faulted) throw t.Exception; return (object)t.Result; });
                            case -1611393113: 
                                return ((IMethodEntityGrain)grain).ReceiveMessageAsync((IOrleansEntityDescriptor)arguments[0], (IMessage)arguments[1]).ContinueWith(t => {if (t.Status == System.Threading.Tasks.TaskStatus.Faulted) throw t.Exception; return (object)null; });
                            case -1831544886: 
                                return ((IMethodEntityGrain)grain).IsInitialized().ContinueWith(t => {if (t.Status == System.Threading.Tasks.TaskStatus.Faulted) throw t.Exception; return (object)t.Result; });
                            default: 
                            throw new NotImplementedException("interfaceId="+interfaceId+",methodId="+methodId);
                        }case -1097320095:  // IGrainWithGuidKey
                        switch (methodId)
                        {
                            default: 
                            throw new NotImplementedException("interfaceId="+interfaceId+",methodId="+methodId);
                        }
                    default:
                        throw new System.InvalidCastException("interfaceId="+interfaceId);
                }
            }
            catch(Exception ex)
            {
                var t = new System.Threading.Tasks.TaskCompletionSource<object>();
                t.SetException(ex);
                return t.Task;
            }
        }
        
        public static string GetMethodName(int interfaceId, int methodId)
        {

            switch (interfaceId)
            {
                
                case 898358989:  // IMethodEntityGrain
                    switch (methodId)
                    {
                        case 331950094:
                            return "GetDescriptor";
                    case -1611393113:
                            return "ReceiveMessageAsync";
                    case -1831544886:
                            return "IsInitialized";
                    
                        default: 
                            throw new NotImplementedException("interfaceId="+interfaceId+",methodId="+methodId);
                    }
                case -1097320095:  // IGrainWithGuidKey
                    switch (methodId)
                    {
                        
                        default: 
                            throw new NotImplementedException("interfaceId="+interfaceId+",methodId="+methodId);
                    }

                default:
                    throw new System.InvalidCastException("interfaceId="+interfaceId);
            }
        }
    }
}
#pragma warning restore 162
#pragma warning restore 219
#pragma warning restore 414
#pragma warning restore 649
#pragma warning restore 693
#pragma warning restore 1591
#pragma warning restore 1998
#endif
