// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace ReachingTypeAnalysis
{
    /// <summary>
    /// This is a string represenation of a method designed to be 
    /// put as keys in Dictionaries and used for comparison.
    /// </summary>
    [Serializable]
    public class MethodDescriptor
    {
        public string ClassName { get; private set; }
        public string MethodName { get; private set; }
        public string NamespaceName { get; private set; }

        private string name;
        private TypeDescriptor containerType;

        public IList<TypeDescriptor> Parameters { get; private set; }
        public TypeDescriptor ReturnType { get; private set; }

        public bool IsStatic { get; private set; }


        public string Name
        {
            get
            {
                if (this.name == null)
                {
                    this.name = ClassName + "." + MethodName;
                    if (this.NamespaceName != string.Empty)
                    {
                        this.name = NamespaceName + "." + this.name;
                    }
                }
                return name;
            }
        }

        public TypeDescriptor ContainerType
        {
            get
            {
                if (containerType == null)
                {
                    containerType = new TypeDescriptor(this.NamespaceName, this.ClassName);
                }
                return containerType;
            }
        }

        public TypeDescriptor ThisType
        {
            get
            {
                return (!IsStatic)? ContainerType: null;
            }
        }

		public MethodDescriptor() : this("","")
		{
		}

		public MethodDescriptor(string namespaceName, string className, string methodName,
									bool isStatic = false,
									IEnumerable<TypeDescriptor> parameters = null,
									TypeDescriptor returnType = null)
		{
			this.NamespaceName = namespaceName;
			this.ClassName = className;
			this.MethodName = methodName;
			this.name = className + "." + methodName;
			this.IsStatic = isStatic;
			this.ReturnType = returnType;

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

        public override bool Equals(object obj)
        {
            var md = obj as MethodDescriptor;
			if (md == null) return false;

            var nEq = this.NamespaceName == "" || md.NamespaceName == "" || this.NamespaceName.Equals(md.NamespaceName);
            var cEq = this.ClassName.Equals(md.ClassName);
            var mEq = this.MethodName.Equals(md.MethodName);
            var staticEq = this.IsStatic == md.IsStatic;
            var pEq = this.Parameters == null || md.Parameters == null || this.Parameters.SequenceEqual(md.Parameters);

            return nEq && cEq && mEq && staticEq && pEq;
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
            return NamespaceName.GetHashCode() + ClassName.GetHashCode() + MethodName.GetHashCode();
        }

        public override string ToString()
        {
            return this.Name;
        }
        public string Marshall()
        {
			var result = new StringBuilder();

			result.Append(this.NamespaceName);
			result.Append("+");
			result.Append(this.ClassName);
			result.Append("+");
			result.Append(this.MethodName);
			result.Append("+");
			result.Append(this.IsStatic);

			if (this.Parameters != null && this.Parameters.Count > 0)
			{
				foreach (var parameterType in this.Parameters)
				{
					result.Append("+");
					result.Append(parameterType.TypeName);
				}
			}

			return result.ToString();
        }

        public static MethodDescriptor DeMarsall(string md)
        {
            var tokens = md.Split('+');

			var namespaceName = tokens[0];
			var className = tokens[1];
			var methodName = tokens[2];
			var isStatic = Convert.ToBoolean(tokens[3]);
			var methodDescriptor = new MethodDescriptor(namespaceName, className, methodName, isStatic);

            if (tokens.Length > 4 && tokens[4].Length>0)
            {
                methodDescriptor.Parameters = new List<TypeDescriptor>();
                for (var i = 4; i < tokens.Length; ++i)
                {
                    var typeName = tokens[i];
                    var typeDescriptor = new TypeDescriptor(typeName);

                    methodDescriptor.Parameters.Add(typeDescriptor);
                }
            }

			return methodDescriptor;
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
        public string TypeName { get; private set; }
        public bool IsConcreteType { get; private set; }

        public TypeDescriptor(string namespaceName, string className, bool isReferenceType = true, SerializableTypeKind kind = SerializableTypeKind.Undefined, bool isConcrete = true)
        {
            this.TypeName = namespaceName + '.' + className;
            this.IsReferenceType = isReferenceType;
            this.Kind = kind;
            this.IsConcreteType = isConcrete;
        }

        public TypeDescriptor(string typeName, bool isReferenceType = true, SerializableTypeKind kind = SerializableTypeKind.Undefined, bool isConcrete = true)
        {
            this.TypeName = typeName;
            this.IsReferenceType = isReferenceType;
            this.Kind = kind;
            this.IsConcreteType = isConcrete;
        }

        public TypeDescriptor(TypeDescriptor typeDescriptor, bool isConcrete = true)
        {
            this.TypeName = typeDescriptor.TypeName;
            this.IsReferenceType = typeDescriptor.IsReferenceType;
            this.Kind = typeDescriptor.Kind;
            this.IsConcreteType = isConcrete;
        }

        // TODO: Fix the equals, but we need to resolve the default values
        public override bool Equals(object obj)
        {
            TypeDescriptor typeDescriptor = (TypeDescriptor)obj;
            bool eqKind = typeDescriptor.Kind.Equals(SerializableTypeKind.Undefined) ||
                          this.Kind.Equals(SerializableTypeKind.Undefined) ||
                          this.Kind.Equals(typeDescriptor.Kind);
            bool eqRef = this.IsReferenceType == typeDescriptor.IsReferenceType;
            bool eqConcrete = this.IsConcreteType == typeDescriptor.IsConcreteType;

            return this.TypeName.Equals(typeDescriptor.TypeName)
             //       && eqRef && eqConcrete
                    && eqKind;
        }
        // TODO: Fix the equals, but we need to resolve the default values
        public override int GetHashCode()
        {
            return this.TypeName.GetHashCode() + this.Kind.GetHashCode();
        }

        public override string ToString()
        {
            return this.TypeName.ToString();
        }

        public bool IsDelegate
        {
            get
            {
                return this.Kind.Equals(SerializableTypeKind.Delegate);
            }
        }
    }

    [Serializable]
    public class LocationDescriptor
    {
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
        public LocationDescriptor(int inMethodOrder)
        {
            this.InMethodOrder = inMethodOrder;
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
 		public LocationDescriptor LocationDescriptor { get; private set; }
        public int InMethodPosition { get; private set; }
		public AnalysisCallNode(string methodName, TypeDescriptor declaredType, LocationDescriptor location)
            : base(methodName, declaredType)
		{
			this.LocationDescriptor = location;
            this.InMethodPosition = location.InMethodOrder;
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