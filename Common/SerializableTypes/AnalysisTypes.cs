// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using CodeGraphModel;

namespace ReachingTypeAnalysis
{
	public static class TestConstants
	{
		public const string ProjectName = "MyProject";
		public const string ProjectAssemblyName = "MyProject";
		public const string DocumentName = "MyFile.cs";
		public const string DocumentPath = @"C:\MyFile.cs";
	}

	/// <summary>
	/// This is a string represenation of a method designed to be 
	/// put as keys in Dictionaries and used for comparison.
	/// </summary>
	[Serializable]
    public class MethodDescriptor
    {
		protected string name;

        public TypeDescriptor ContainerType { get; protected set; }
        public string MethodName { get; protected set; }
        public IList<TypeDescriptor> Parameters { get; protected set; }
        public TypeDescriptor ReturnType { get; protected set; }
        public bool IsStatic { get; protected set; }

        public string ClassName
        {
            get { return ContainerType.ClassName; }
        }

        public string NamespaceName
        {
            get { return ContainerType.NamespaceName; }
        }

		public string Name
		{
			get
			{
				if (string.IsNullOrEmpty(name))
				{
					var qualifiedName = new List<string>();

					if (!string.IsNullOrEmpty(this.NamespaceName))
					{
						qualifiedName.Add(this.NamespaceName);
					}

					qualifiedName.Add(this.ClassName);
					qualifiedName.Add(this.MethodName);
					name = string.Join(".", qualifiedName);
				}

				return name;
			}
		}

        public TypeDescriptor ThisType
        {
            get
            {
                return !IsStatic ? ContainerType : null;
            }
        }

        /// <summary>
        ///  I include this just to simplify 
        /// </summary>
        public virtual MethodDescriptor BaseDescriptor 
        { 
            get { return this;  }
            protected set { } 
        }
		
        public bool IsAnonymousDescriptor { get; protected set; }

		public MethodDescriptor() : this("","")
		{
		}

        public MethodDescriptor(TypeDescriptor typeDescriptor, string methodName,
                                    bool isStatic = false,
                                    IEnumerable<TypeDescriptor> parameters = null,
                                    TypeDescriptor returnType = null)
        {
            this.ContainerType = typeDescriptor;
            //this.NamespaceName = typeDescriptor.Namespace;
            //this.ClassName = typeDescriptor.ClassName;
            this.MethodName = methodName;
            this.IsStatic = isStatic;
            this.ReturnType = returnType;
            this.IsAnonymousDescriptor = false;

            if (parameters != null)
            {
                this.Parameters = new List<TypeDescriptor>(parameters);
            }
        }

		public MethodDescriptor(string namespaceName, string className, string methodName,
									bool isStatic = false,
									IEnumerable<TypeDescriptor> parameters = null,
									TypeDescriptor returnType = null)
		{
            this.ContainerType = new TypeDescriptor(namespaceName, className, isReferenceType: true);
            //this.NamespaceName = namespaceName;
            //this.ClassName = className;
			this.MethodName = methodName;
			this.IsStatic = isStatic;
			this.ReturnType = returnType;
			this.IsAnonymousDescriptor = false;

			if (parameters != null)
			{
				this.Parameters = new List<TypeDescriptor>(parameters);
			}            
		}

        public MethodDescriptor(string className, string methodName, bool isStatic = false)
			: this("", className, methodName, isStatic)
        {
            //this.Parameters = new List<TypeDescriptor>();
        }

        //public bool SameAsMethodSymbol(IMethodSymbol method)
        //{
        //    if (method == null)
        //    {
        //        return false;
        //    }
        //    else
        //    {
        //        bool result = method.Name.Equals(this.MethodName);
        //        result = result && method.ContainingType.Name.Equals(this.ClassName);
        //        result = result & (this.NamespaceName == null || method.ContainingNamespace.Name.Equals(this.NamespaceName));

        //        return result;
        //    }
        //}
        public MethodDescriptor(MethodDescriptor original)
        {
            this.ContainerType = original.ContainerType;
            //this.ClassName = original.ClassName;
            //this.NamespaceName = original.NamespaceName;
            this.MethodName = original.MethodName;
            this.Parameters = original.Parameters;
            this.ReturnType = original.ReturnType;
			this.IsStatic = original.IsStatic;
			// this.ThisType = original.ThisType;
		}

        public override bool Equals(object obj)
        {
            var md = obj as MethodDescriptor;
			if (md == null) return false;

            //var nEq = this.NamespaceName == "" || md.NamespaceName == "" || this.NamespaceName.Equals(md.NamespaceName);
            //var cEq = this.ClassName.Equals(md.ClassName);
            var tEq = this.ContainerType.Equals(md.ContainerType);
            var mEq = this.MethodName.Equals(md.MethodName);
            var staticEq = this.IsStatic == md.IsStatic;
            var pEq = this.Parameters == null || md.Parameters == null || this.Parameters.SequenceEqual(md.Parameters);

            return /*nEq && cEq*/ tEq && mEq && staticEq && pEq;
        }

        //private static bool CompareParameters(IList<TypeDescriptor> params1, IList<TypeDescriptor> params2)
        //{
        //    if(params1.Count()!=params2.Count()) return false;
        //    for(var i = 0; i< params1.Count(); i++)
        //    {
        //        if(!params1[i].Equals(params2[i]))
        //            return false;
        //    }
        //    return true;
        //}

        public override int GetHashCode()
        {
            //return NamespaceName.GetHashCode() + ClassName.GetHashCode() + MethodName.GetHashCode();
            return ContainerType.GetHashCode() + MethodName.GetHashCode();
        }

        public override string ToString()
        {
            return this.Name;
        }

        public virtual string Marshall()
        {
			var result = new StringBuilder();

            //result.Append(this.NamespaceName);
            //result.Append("+");
            //result.Append(this.ClassName);
            result.Append(this.ContainerType.Marshall());
			result.Append("+");
			result.Append(this.MethodName);
			result.Append("+");
			result.Append(this.IsStatic);

			if (this.Parameters != null && this.Parameters.Count > 0)
			{
				foreach (var parameterType in this.Parameters)
				{
					result.Append("+");
					result.Append(parameterType.Marshall());
				}
			}

			return result.ToString();
        }

        public static MethodDescriptor DeMarsall(string md)
        {
            var anonymousMD = "";
            var i = md.IndexOf(':');
            if(i>=0)
            {
                anonymousMD = md.Substring(0, i);
                md = md.Substring(i+1);
            }

            var methodDescriptor = ParseMethodDescriptor(md);

            if(anonymousMD.Length>0)
            {
                var anonymousMDContent = ParseMethodDescriptor(anonymousMD);
                return new AnonymousMethodDescriptor(methodDescriptor, anonymousMDContent);
            }

			return methodDescriptor;
        }

        private static MethodDescriptor ParseMethodDescriptor(string md)
        {
            var tokens = md.Split('+');
            var containerType = TypeDescriptor.DeMarshall(tokens[0]);
            //var namespaceName = tokens[0];
            //var className = tokens[1];
            var methodName = tokens[1];
            var isStatic = Convert.ToBoolean(tokens[2]);
            var methodDescriptor = new MethodDescriptor(containerType, methodName, isStatic);

            if (tokens.Length > 3 && tokens[3].Length > 0)
            {
                methodDescriptor.Parameters = new List<TypeDescriptor>();

                for (var i = 3; i < tokens.Length; ++i)
                {
                    var typeName = tokens[i];
                    var typeDescriptor = TypeDescriptor.DeMarshall(typeName);
                    methodDescriptor.Parameters.Add(typeDescriptor);
                }
            }

            return methodDescriptor;
        }
    }

    [Serializable]
    public class AnonymousMethodDescriptor: MethodDescriptor
    {
       public override MethodDescriptor BaseDescriptor { get; protected set; }

       public AnonymousMethodDescriptor(MethodDescriptor baseMethodDescriptor, MethodDescriptor anonymousMethodDescriptor)
           : base(anonymousMethodDescriptor)
       {
           this.BaseDescriptor = baseMethodDescriptor;
           this.MethodName = "Anonymous";
           this.IsAnonymousDescriptor = true;
       }
	
       public override bool Equals(object obj)
       {
           var other = (AnonymousMethodDescriptor)obj;
           return this.BaseDescriptor.Equals(other.BaseDescriptor) && this.MethodName.Equals(other.MethodName);
       }

       public override int GetHashCode()
       {
           return this.BaseDescriptor.GetHashCode() + this.MethodName.GetHashCode();
       }

       public override string Marshall()
       {
           return base.Marshall() + ":" + this.BaseDescriptor.Marshall();
       }
    }

    [Serializable]
    public enum SerializableTypeKind
    {
        Class,
        Interface,
        Delegate,
        TypeParameter,
        Array,
        Struct,
		Module,
		Enum,
		Pointer,
		Dynamic,
		Error,
		Submission,
		Unknown,
        Undefined
	}

    [Serializable]
    public class TypeDescriptor
    {
        public bool IsReferenceType { get; private set; }
        public SerializableTypeKind Kind { get; private set; }
        public bool IsConcreteType { get; private set; }
        public string NamespaceName { get; private set; }
        public string ClassName { get; private set; }
        public string AssemblyName { get; private set; }

        public TypeDescriptor(string namespaceName, string className, string assemblyName = TestConstants.ProjectAssemblyName, bool isReferenceType = true, SerializableTypeKind kind = SerializableTypeKind.Undefined, bool isConcrete = true)
        {
            this.NamespaceName = namespaceName;
            this.ClassName = className;
            this.IsReferenceType = isReferenceType;
            this.Kind = kind;
            this.IsConcreteType = isConcrete;
            this.AssemblyName = assemblyName;
        }
        public TypeDescriptor(TypeDescriptor typeDescriptor, bool isConcrete = true)
        {
            this.NamespaceName = typeDescriptor.NamespaceName;
            this.ClassName = typeDescriptor.ClassName;
            this.IsReferenceType = typeDescriptor.IsReferenceType;
            this.Kind = typeDescriptor.Kind;
            this.AssemblyName = typeDescriptor.AssemblyName;
            this.IsConcreteType = isConcrete;
        }

        public string TypeName
        {
            get 
            {
                var typeName = this.ClassName;
                if(!String.IsNullOrEmpty(this.NamespaceName))
                    typeName = this.NamespaceName + '.' + this.ClassName;
                return  typeName; 
            }
        }

        public string FullTypeName
        {
            get { return this.AssemblyName + "::" + this.TypeName; }
        }

        // TODO: Fix the equals, but we need to resolve the default values
        public override bool Equals(object obj)
        {
            var typeDescriptor = (TypeDescriptor)obj;
            var eqKind = typeDescriptor.Kind.Equals(SerializableTypeKind.Undefined) ||
                          this.Kind.Equals(SerializableTypeKind.Undefined) ||
                          this.Kind.Equals(typeDescriptor.Kind);
            var eqRef = this.IsReferenceType == typeDescriptor.IsReferenceType;
            var eqConcrete = this.IsConcreteType == typeDescriptor.IsConcreteType;

            return this.FullTypeName.Equals(typeDescriptor.FullTypeName)
             //       && eqRef && eqConcrete
                    && eqKind;
        }

        // TODO: Fix the equals, but we need to resolve the default values
        public override int GetHashCode()
        {
            return this.FullTypeName.GetHashCode()
                + (this.Kind.Equals(SerializableTypeKind.Undefined) ? 0 : this.Kind.GetHashCode());
        }

        public override string ToString()
        {
            return this.FullTypeName.ToString();
        }

        public bool IsDelegate
        {
            get
            {
                return this.Kind.Equals(SerializableTypeKind.Delegate);
            }
        }

        internal string Marshall()
        {
            var result = new StringBuilder();
            result.Append(this.AssemblyName);
            result.Append("=");
            result.Append(this.NamespaceName);
            result.Append("=");
            result.Append(this.ClassName);

            return result.ToString();
        }
        internal static TypeDescriptor DeMarshall(string typeString)
        {
            var tokens = typeString.Split('=');
            var assemblyName = tokens[0];
            var namespaceName = tokens[1];
            var className = tokens[2];
            return new TypeDescriptor(namespaceName, className, assemblyName, true, SerializableTypeKind.Undefined, true);
        }
    }

    [Serializable]
    public class LocationDescriptor
    {
        public string FilePath { get; private set; }
        public Range Range { get; private set; }
        //public Location Location { get; private set; }
        public int InMethodOrder { get; private set; }
        //public LocationDescriptor(Location location)
        //{
        //    Contract.Assert(location != null);
        //    this.Location = location;
        //    InMethodOrder = 0; // need to search for the statement # in that location
        //}

        public int Location 
        { 
            get { return InMethodOrder;  } 
        } 
        public LocationDescriptor(int inMethodOrder, Range range, string filePath)
        {
            this.InMethodOrder = inMethodOrder;
            this.Range = range;
            this.FilePath = filePath;
        }

        public override string ToString()
        {
            return this.Location.ToString();
        }
        public override bool Equals(object obj)
        {
            LocationDescriptor locRef = (LocationDescriptor)obj;
            return this.Location.Equals(locRef.Location);
        }
        public override int GetHashCode()
        {
            return this.Location.GetHashCode();
        }
    }

	[Serializable]
	public class AnalysisCallNodeAdditionalInfo
	{
		public string DisplayString { get; private set; }
		public string StaticMethodDeclarationPath { get; private set; }
		public MethodDescriptor StaticMethodDescriptor { get; private set; }

		public AnalysisCallNodeAdditionalInfo(MethodDescriptor staticMethodDescriptor, string staticMethodDeclarationPath, string displayString)
		{
			this.StaticMethodDescriptor = staticMethodDescriptor;            
			this.StaticMethodDeclarationPath = staticMethodDeclarationPath;
			this.DisplayString = displayString;
		}
	}

	/// <summary>
	/// This is essentially a node in the propagation graph 
	/// It represents either a variable, field, parameter or even a method invocation (that are specials)
	/// Currently they conrtain information about the corresponding Roslyn symbol they represent 
	/// But the idea is to get rid of roslyn into (maybe keeping only the syntax expression they denote)
	/// </summary> 
	[Serializable]
    public abstract class PropGraphNodeDescriptor
	{
		public string Name { get; private set; }
		public TypeDescriptor Type { get; private set; }

        protected PropGraphNodeDescriptor(string name, TypeDescriptor declaredType)
		{
			this.Type = declaredType;
			this.Name = name;
 		}

        public override bool Equals(object obj)
        {
            var analysisNode = (obj as PropGraphNodeDescriptor);
            return (analysisNode != null && analysisNode.Name.Equals(this.Name) 
                    && analysisNode.Type.Equals(this.Type));
        }
 
		public override int GetHashCode()
		{
            return this.Name.GetHashCode()+this.Type.GetHashCode();
		}

		public override string ToString()
		{
            return string.Format("{0}:{1}", this.Name, this.Type);
            //string tString = "Dummy";
            //if (this.DeclaredType != null)
            //    tString = this.DeclaredType.TypeName;
            //string lString = "";
            //if (this.LocationReference != null)
            //    lString = "@" + this.LocationReference.ToString();
            //string eString = "";
            //if (this.Expression != null)
            //    eString = this.Expression.ToString();
            //string eSymbol = "";
            //if (this.Symbol != null && (this.Symbol.Kind == SymbolKind.Field))
            //    eSymbol = this.Symbol.ContainingType.Name.ToString() + ".";

            //return eSymbol + eString + lString + ":" + tString;
		}

        //public static AnalysisNode Define(TypeDescriptor declaredType)
        //{
        //	if (Utils.IsTypeForAnalysis(declaredType))
        //	{
        //		return new AnalysisNode(declaredType);
        //	}
        //	else
        //	{
        //		return null;
        //	}
        //}

        //internal SyntaxNodeOrToken Expression { get; private set; }
        //internal VariableDescriptor Symbol { get; private set; }

	}

    [Serializable]
    public class VariableNode : PropGraphNodeDescriptor
    {
        public VariableNode(string name, TypeDescriptor declaredType) :
            base(name, declaredType)
        {

        }
    }

    [Serializable]
	public class ParameterNode : VariableNode
	{
		public int Position { get; private set; }
		public ParameterNode(string name, int position, TypeDescriptor declaredType) :
            base(name, declaredType)
		{
			this.Position = position;
		}
        public override string ToString()
        {
            return base.ToString()+"_"+Position.ToString();
        }
	}

    [Serializable]
	public class ThisNode : VariableNode
	{
		public ThisNode(TypeDescriptor declaredType) :
            base("this", declaredType)
		{

		}
	}

    [Serializable]
	public class UnsupportedNode : PropGraphNodeDescriptor
	{
		public UnsupportedNode(TypeDescriptor declaredType) :
            base("unsupported", declaredType)
		{

		}
	}

    [Serializable]
	public class ReturnNode : VariableNode
	{
		public ReturnNode(TypeDescriptor declaredType) :
            base("return", declaredType)
		{
		}
	}

        
    [Serializable]
	public class FieldNode : VariableNode
	{
		public string ClassName { get; private set; }
		public string Field { get; private set; }
		public FieldNode(string className, string fieldName, TypeDescriptor declaredType)
            : base(string.Format("{0}.{1}", className, fieldName), declaredType)
		{
			this.ClassName = className;
			this.Field = fieldName;
		}
	}

    [Serializable]
	public class DelegateVariableNode : VariableNode
	{
		public DelegateVariableNode(string name,TypeDescriptor declaredType)
			: base(name,declaredType)
		{ }
        public override string ToString()
        {
 	         return "Delegate: " + base.ToString();
        }
	}

    [Serializable]
    public class PropertyVariableNode : VariableNode
    {
        AnalysisCallNode ProperyMethod { get; set; }
        public PropertyVariableNode(string name, TypeDescriptor declaredType,
            AnalysisCallNode propertyMethod)
            : base(name, declaredType)
        {
            this.ProperyMethod = propertyMethod;
        }
        public override string ToString()
        {
            return "property: " + base.ToString();
        }
    }

    [Serializable]
	public class AnalysisCallNode : PropGraphNodeDescriptor
	{
		public AnalysisCallNodeAdditionalInfo AdditionalInfo { get; private set; }
 		public LocationDescriptor LocationDescriptor { get; private set; }
        public int InMethodPosition { get; private set; }
		public AnalysisCallNode(string methodName, TypeDescriptor declaredType, LocationDescriptor location, AnalysisCallNodeAdditionalInfo additionalInfo)
            : base(methodName, declaredType)
		{
			this.LocationDescriptor = location;
            this.InMethodPosition = location.InMethodOrder;
			this.AdditionalInfo = additionalInfo;
        }

		/// <summary>
		/// The idea is to use the same hash and equals than ANode but also locations
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			AnalysisCallNode analysisCallNode = (obj as AnalysisCallNode);
            return base.Equals(obj) 
                    && analysisCallNode != null && base.Equals(analysisCallNode) 
                    && analysisCallNode.LocationDescriptor.Equals(this.LocationDescriptor);
		}
		public override int GetHashCode()
		{
			return base.GetHashCode() + this.LocationDescriptor.GetHashCode();
		}
	}

/*	REMOVED: AnalysisType	
	internal abstract class AnalysisType
	{
		protected bool concrete = true;

		internal string Name { get; private set; }

		internal AnalysisType(ITypeSymbol rt)
		{
			this.Name = rt.Name.ToString();
			this.RoslynType = rt;
		}

		public bool IsSubtype(AnalysisType t)
		{
			AnalysisType at = t as AnalysisType;
			var res = TypeHelper.InheritsByName(this.RoslynType, at.RoslynType);
			return res;
		}

		//public bool IsAssignable(AnalysisType t)
		//{
		//    AType at = t as AType;
		//    var res = TypeHelper.Inherits(this.RoslynType, at.RoslynType);
		//    return res;
		//}

		internal ITypeSymbol RoslynType { get; private set; }

		public override string ToString()
		{
			return this.Name.ToString();
		}

		public override bool Equals(object obj)
		{
			var at = obj as AnalysisType;
			return at != null && this.RoslynType.ToString() == this.RoslynType.ToString();
			//return at != null && this.RoslynType.Equals(at.RoslynType);
		}

		public override int GetHashCode()
		{
			return RoslynType.ToString().GetHashCode();
		}

		public virtual bool IsConcreteType
		{
			get { return concrete; }
		}

		//public virtual bool IsDelegate
		//{
		//	get { return RoslynType != null && RoslynType.TypeKind == TypeKind.Delegate; }
		//}

		internal void SetDeclaredType()
		{
			this.concrete = true;
		}
	}

	internal class ConcreteType : AnalysisType
	{
		internal ConcreteType(ITypeSymbol rt) : base(rt)
		{

		}
	}

	internal class DeclaredType : AnalysisType
	{
		internal DeclaredType(ITypeSymbol rt) : base(rt)
		{ }
		internal DeclaredType(AnalysisType aType) : base(aType.RoslynType)
		{

		}
	}

	//public interface AMethod
	//{
	//    AMethod FindMethodImplementation(AnalysisType t);
	//    AnalysisType ContainerType { get; }
	//    MethodDescriptor MethodDescriptor { get; }
	//}
*/
/*      REMOVED: AnalysisMethod
	internal class AnalysisMethod
	{
        internal MethodDescriptor MethodDescriptor { get; private set; }
        internal TypeDescriptor ContainerType { get; private set; }
        //private IMethodSymbol method;
        //internal IMethodSymbol RoslynMethod
        //{
        //    get { return method; }
        //    private set { method = value; }
        //}
        //internal AnalysisMethod(IMethodSymbol method)
        //{
        //    this.method = method;
        //}
        //internal AnalysisMethod(IPropertySymbol property)
        //{
        //    this.method = property.GetMethod;
        //}
        //internal MethodDescriptor MethodDescriptor
        //{
        //    get { return new MethodDescriptor(this.RoslynMethod); }
        //}

        internal AnalysisMethod(MethodDescriptor methodDescriptor, TypeDescriptor containerType)
        {
            this.MethodDescriptor = methodDescriptor;
            this.ContainerType = containerType;
        }

		public override bool Equals(object obj)
		{
            //AnalysisMethod m = obj as AnalysisMethod;
            //return m != null && method.ToString().Equals(m.method.ToString());
            AnalysisMethod analysisMethod = (AnalysisMethod)obj;
            return this.ContainerType.Equals(analysisMethod.ContainerType)
                    && this.MethodDescriptor.Equals(analysisMethod.MethodDescriptor);
		}

		public override int GetHashCode()
		{
			return this.MethodDescriptor.GetHashCode()+this.ContainerType.GetHashCode();
		}

		public override string ToString()
		{
			return MethodDescriptor.ToString();
		}

        //internal AnalysisMethod FindMethodImplementation(AnalysisType t)
        //{
        //    throw new NotImplementedException();

        //    //AnalysisType aType = t as AnalysisType;
        //    //var realCallee = Utils.FindMethodImplementation(this.RoslynMethod, aType.RoslynType);
        //    //if (realCallee != null)
        //    //{
        //    //    return new AnalysisMethod(realCallee);
        //    //}
        //    //return this;
        //}

	}

	public class AnalysisLocation
	{
		public Location Location { get; private set; }
		internal AnalysisLocation(Location location)
		{
			this.Location = location;
		}
 
		public override bool Equals(object obj)
		{
			AnalysisLocation analysisLocation = obj as AnalysisLocation;
			return obj != null && (this.Location == null && analysisLocation.Location == null) 
                || this.Location.SourceSpan.Start.Equals(analysisLocation.Location.SourceSpan.Start);
		}

		public override int GetHashCode()
		{
			return this.Location != null ? this.Location.GetLineSpan().GetHashCode() : 1;
		}
	}
     */

    //[Serializable]
    //public class VariableDescriptor : INodeDescriptor
    //{
    //    public string Name { get; private set; }
    //    public TypeDescriptor Type { get; private set; }

    //    public VariableDescriptor(string name, TypeDescriptor type)
    //    {
    //        this.Name = name;
    //        this.Type = type;
    //    }
    //}
}