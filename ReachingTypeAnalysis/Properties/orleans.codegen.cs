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

namespace ReachingTypeAnalysis.Analysis
{
    using System;
    using ReachingTypeAnalysis;
    using Orleans.CodeGeneration;
    using Orleans;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.0.8.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    [SerializableAttribute()]
    [global::Orleans.CodeGeneration.GrainStateAttribute("ReachingTypeAnalysis.Analysis.ReachingTypeAnalysis.Analysis.MethodEntityGrain")]
    public class MethodEntityGrainState : global::Orleans.CodeGeneration.GrainState, IOrleansEntityState
    {
        

            public Guid @Guid { get; set; }

            public MethodDescriptor @MethodDescriptor { get; set; }

            public override void SetAll(System.Collections.Generic.IDictionary<string,object> values)
            {   
                object value;
                if (values == null) { InitStateFields(); return; }
                if (values.TryGetValue("Guid", out value)) @Guid = (Guid) value;
                if (values.TryGetValue("MethodDescriptor", out value)) @MethodDescriptor = (MethodDescriptor) value;
            }

            public override System.String ToString()
            {
                return System.String.Format("MethodEntityGrainState( Guid={0} MethodDescriptor={1} )", @Guid, @MethodDescriptor);
            }
        
        public MethodEntityGrainState() : 
                base("ReachingTypeAnalysis.Analysis.MethodEntityGrain")
        {
            this.InitStateFields();
        }
        
        public override System.Collections.Generic.IDictionary<string, object> AsDictionary()
        {
            System.Collections.Generic.Dictionary<string, object> result = new System.Collections.Generic.Dictionary<string, object>();
            result["Guid"] = this.Guid;
            result["MethodDescriptor"] = this.MethodDescriptor;
            return result;
        }
        
        private void InitStateFields()
        {
            this.Guid = default(Guid);
            this.MethodDescriptor = default(MethodDescriptor);
        }
        
        [global::Orleans.CodeGeneration.CopierMethodAttribute()]
        public static object _Copier(object original)
        {
            MethodEntityGrainState input = ((MethodEntityGrainState)(original));
            return input.DeepCopy();
        }
        
        [global::Orleans.CodeGeneration.SerializerMethodAttribute()]
        public static void _Serializer(object original, global::Orleans.Serialization.BinaryTokenStreamWriter stream, System.Type expected)
        {
            MethodEntityGrainState input = ((MethodEntityGrainState)(original));
            input.SerializeTo(stream);
        }
        
        [global::Orleans.CodeGeneration.DeserializerMethodAttribute()]
        public static object _Deserializer(System.Type expected, global::Orleans.Serialization.BinaryTokenStreamReader stream)
        {
            MethodEntityGrainState result = new MethodEntityGrainState();
            result.DeserializeFrom(stream);
            return result;
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
