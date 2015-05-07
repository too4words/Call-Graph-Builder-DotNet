// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

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

        public MethodDescriptor(string classname, string methodName)
        {
            this.NamespaceName = "";
            this.ClassName = classname;
            this.MethodName = methodName;
            this.name = classname + "." + methodName;
        }

        public MethodDescriptor(string namespaceName, string classname, string methodName)
        {
            this.NamespaceName = namespaceName;
            this.ClassName = classname;
            this.MethodName = methodName;
        }

        public MethodDescriptor(IMethodSymbol method)
        {
            Contract.Assert(method != null);

            this.MethodName = method.Name;
            this.ClassName = method.ContainingType.Name;
            this.NamespaceName = method.ContainingNamespace.Name;
            this.containerType = new TypeDescriptor(method.ContainingType);
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
            bool nEq = (this.NamespaceName == "" || md.NamespaceName == "") || this.NamespaceName.Equals(md.NamespaceName);
            bool cEq = this.ClassName.Equals(md.ClassName);
            bool mEq = this.MethodName.Equals(md.MethodName);

            return nEq && cEq && mEq;
        }

        public override int GetHashCode()
        {
            return NamespaceName.GetHashCode() + ClassName.GetHashCode() + MethodName.GetHashCode();
        }

        public override string ToString()
        {
            return this.Name;
        }
    }

    [Serializable]
    public class TypeDescriptor
    {
        public bool IsReferenceType { get; private set; }
        public TypeKind Kind { get; private set; }
        public string TypeName { get; private set; }
        public bool IsConcreteType { get; private set; }

        public TypeDescriptor(ITypeSymbol type, bool isConcrete = true)
        {
            this.TypeName = type.ToDisplayString();
            this.IsReferenceType = type.IsReferenceType;
            this.Kind = type.TypeKind;
            this.IsConcreteType = isConcrete;
        }
        public TypeDescriptor(string nameSpaceName, string className, bool isReferenceType = true, bool isConcrete = true)
        {
            this.TypeName = nameSpaceName + '.' + className;
            this.IsReferenceType = IsReferenceType;
            this.Kind = TypeKind.Class;
            this.IsConcreteType = isConcrete;
        }
        public TypeDescriptor(TypeDescriptor typeDescriptor, bool isConcrete = true)
        {
            this.TypeName = typeDescriptor.TypeName;
            this.IsReferenceType = typeDescriptor.IsReferenceType;
            this.Kind = typeDescriptor.Kind;
            this.IsConcreteType = isConcrete;
        }

        public override bool Equals(object obj)
        {
            TypeDescriptor typeDescriptor = (TypeDescriptor)obj;
            return this.TypeName.Equals(typeDescriptor.TypeName)
                    && this.IsReferenceType == typeDescriptor.IsReferenceType
                    && this.IsConcreteType == typeDescriptor.IsConcreteType
                    && this.Kind.Equals(typeDescriptor.Kind);
        }
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
                return this.Kind.Equals(TypeKind.Delegate);
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
 	         return "Delegate:" + base.ToString();
        }
	}

    [Serializable]
    public class PropertyVariableNode : VariableNode
    {
        AnalysisCallNode properyMethod;
        public PropertyVariableNode(string name, TypeDescriptor declaredType, AnalysisCallNode propertyMethod)
            : base(string.Format("delegate {0}", name), declaredType)
        {
            this.properyMethod = properyMethod;
        }
    }

    [Serializable]
	public class AnalysisCallNode : PropGraphNodeDescriptor
	{
		public LocationDescriptor LocationDescriptor { get; private set; }
        public int InMethodOrder { get; private set; }
		public AnalysisCallNode(TypeDescriptor declaredType, LocationDescriptor location)
            : base(string.Format("call {0}", location.ToString()), declaredType)
		{
			this.LocationDescriptor = location;
            this.InMethodOrder = location.InMethodOrder;
        }

		/// <summary>
		/// The idea is to use the same hash and equals than ANode but also locations
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			AnalysisCallNode analysisCallNode = (obj as AnalysisCallNode);
            return analysisCallNode != null && base.Equals(analysisCallNode) 
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