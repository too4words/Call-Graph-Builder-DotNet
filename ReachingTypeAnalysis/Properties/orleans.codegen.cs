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
    using ReachingTypeAnalysis;
    using Orleans.CodeGeneration;
    using Orleans;
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Collections.Generic;
    using System.Collections;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.0.8.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    [SerializableAttribute()]
    [global::Orleans.CodeGeneration.GrainStateAttribute("ReachingTypeAnalysis.Analysis.ReachingTypeAnalysis.Analysis.MethodEntityGrain")]
    public class MethodEntityGrainState : global::Orleans.CodeGeneration.GrainState, IOrleansEntityState
    {
        

            public MethodDescriptor @MethodDescriptor { get; set; }

            public override void SetAll(System.Collections.Generic.IDictionary<string,object> values)
            {   
                object value;
                if (values == null) { InitStateFields(); return; }
                if (values.TryGetValue("MethodDescriptor", out value)) @MethodDescriptor = (MethodDescriptor) value;
            }

            public override System.String ToString()
            {
                return System.String.Format("MethodEntityGrainState( MethodDescriptor={0} )", @MethodDescriptor);
            }
        
        public MethodEntityGrainState() : 
                base("ReachingTypeAnalysis.Analysis.MethodEntityGrain")
        {
            this.InitStateFields();
        }
        
        public override System.Collections.Generic.IDictionary<string, object> AsDictionary()
        {
            System.Collections.Generic.Dictionary<string, object> result = new System.Collections.Generic.Dictionary<string, object>();
            result["MethodDescriptor"] = this.MethodDescriptor;
            return result;
        }
        
        private void InitStateFields()
        {
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
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.0.8.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    [SerializableAttribute()]
    [global::Orleans.CodeGeneration.GrainStateAttribute("ReachingTypeAnalysis.Analysis.ReachingTypeAnalysis.Analysis.ProjectCodeProviderGr" +
        "ain")]
    public class ProjectCodeProviderGrainState : global::Orleans.CodeGeneration.GrainState, IProjectState
    {
        

            public String @FullPath { get; set; }

            public String @Name { get; set; }

            public String @SourceCode { get; set; }

            public override void SetAll(System.Collections.Generic.IDictionary<string,object> values)
            {   
                object value;
                if (values == null) { InitStateFields(); return; }
                if (values.TryGetValue("FullPath", out value)) @FullPath = (String) value;
                if (values.TryGetValue("Name", out value)) @Name = (String) value;
                if (values.TryGetValue("SourceCode", out value)) @SourceCode = (String) value;
            }

            public override System.String ToString()
            {
                return System.String.Format("ProjectCodeProviderGrainState( FullPath={0} Name={1} SourceCode={2} )", @FullPath, @Name, @SourceCode);
            }
        
        public ProjectCodeProviderGrainState() : 
                base("ReachingTypeAnalysis.Analysis.ProjectCodeProviderGrain")
        {
            this.InitStateFields();
        }
        
        public override System.Collections.Generic.IDictionary<string, object> AsDictionary()
        {
            System.Collections.Generic.Dictionary<string, object> result = new System.Collections.Generic.Dictionary<string, object>();
            result["FullPath"] = this.FullPath;
            result["Name"] = this.Name;
            result["SourceCode"] = this.SourceCode;
            return result;
        }
        
        private void InitStateFields()
        {
            this.FullPath = default(String);
            this.Name = default(String);
            this.SourceCode = default(String);
        }
        
        [global::Orleans.CodeGeneration.CopierMethodAttribute()]
        public static object _Copier(object original)
        {
            ProjectCodeProviderGrainState input = ((ProjectCodeProviderGrainState)(original));
            return input.DeepCopy();
        }
        
        [global::Orleans.CodeGeneration.SerializerMethodAttribute()]
        public static void _Serializer(object original, global::Orleans.Serialization.BinaryTokenStreamWriter stream, System.Type expected)
        {
            ProjectCodeProviderGrainState input = ((ProjectCodeProviderGrainState)(original));
            input.SerializeTo(stream);
        }
        
        [global::Orleans.CodeGeneration.DeserializerMethodAttribute()]
        public static object _Deserializer(System.Type expected, global::Orleans.Serialization.BinaryTokenStreamReader stream)
        {
            ProjectCodeProviderGrainState result = new ProjectCodeProviderGrainState();
            result.DeserializeFrom(stream);
            return result;
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.0.8.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    [SerializableAttribute()]
    [global::Orleans.CodeGeneration.GrainStateAttribute("ReachingTypeAnalysis.Analysis.ReachingTypeAnalysis.Analysis.SolutionGrain")]
    public class SolutionGrainState : global::Orleans.CodeGeneration.GrainState, ISolutionState
    {
        

            public String @SolutionFullPath { get; set; }

            public String @SourceCode { get; set; }

            public override void SetAll(System.Collections.Generic.IDictionary<string,object> values)
            {   
                object value;
                if (values == null) { InitStateFields(); return; }
                if (values.TryGetValue("SolutionFullPath", out value)) @SolutionFullPath = (String) value;
                if (values.TryGetValue("SourceCode", out value)) @SourceCode = (String) value;
            }

            public override System.String ToString()
            {
                return System.String.Format("SolutionGrainState( SolutionFullPath={0} SourceCode={1} )", @SolutionFullPath, @SourceCode);
            }
        
        public SolutionGrainState() : 
                base("ReachingTypeAnalysis.Analysis.SolutionGrain")
        {
            this.InitStateFields();
        }
        
        public override System.Collections.Generic.IDictionary<string, object> AsDictionary()
        {
            System.Collections.Generic.Dictionary<string, object> result = new System.Collections.Generic.Dictionary<string, object>();
            result["SolutionFullPath"] = this.SolutionFullPath;
            result["SourceCode"] = this.SourceCode;
            return result;
        }
        
        private void InitStateFields()
        {
            this.SolutionFullPath = default(String);
            this.SourceCode = default(String);
        }
        
        [global::Orleans.CodeGeneration.CopierMethodAttribute()]
        public static object _Copier(object original)
        {
            SolutionGrainState input = ((SolutionGrainState)(original));
            return input.DeepCopy();
        }
        
        [global::Orleans.CodeGeneration.SerializerMethodAttribute()]
        public static void _Serializer(object original, global::Orleans.Serialization.BinaryTokenStreamWriter stream, System.Type expected)
        {
            SolutionGrainState input = ((SolutionGrainState)(original));
            input.SerializeTo(stream);
        }
        
        [global::Orleans.CodeGeneration.DeserializerMethodAttribute()]
        public static object _Deserializer(System.Type expected, global::Orleans.Serialization.BinaryTokenStreamReader stream)
        {
            SolutionGrainState result = new SolutionGrainState();
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
