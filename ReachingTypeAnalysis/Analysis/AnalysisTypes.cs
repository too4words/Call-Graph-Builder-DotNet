// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace ReachingTypeAnalysis
{
    /// <summary>
	/// This is essentially a node in the propagation graph 
	/// It represents either a variable, field, parameter or even a method invocation (that are specials)
	/// Currently they conrtain information about the corresponding Roslyn symbol they represent 
	/// But the idea is to get rid of roslyn into (maybe keeping only the syntax expression they denote)
	/// </summary> 
	internal abstract class AnalysisNode
	{
		public string Name { get; private set; }
		internal TypeDescriptor Type { get; private set; }

        protected AnalysisNode(string name, TypeDescriptor declaredType)
		{
			this.Type = declaredType;
			this.Name = name;
		}

        public override bool Equals(object obj)
        {
            var analysisNode = (obj as AnalysisNode);
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


        //internal AnalysisLocation LocationReference
        //{
        //    //get { return new ALocation(this.expression.GetLocation()); }
        //    get
        //    {
        //        if (locationRef == null)
        //        {
        //            locationRef = new AnalysisLocation(this.Expression);
        //        }

        //        return locationRef;
        //    }
        //}

	}

    [Serializable]
	internal class ParameterNode : VariableNode
	{
		public int Position { get; private set; }
		public ParameterNode(string name, int position, TypeDescriptor declaredType) :
            base(name + "_" + position, declaredType)
		{
			this.Position = position;
		}
	}

    [Serializable]
	internal class ThisNode : VariableNode
	{
		public ThisNode(TypeDescriptor declaredType) :
            base("this", declaredType)
		{

		}
	}

    [Serializable]
	internal class UnsupportedNode : AnalysisNode
	{
		public UnsupportedNode(TypeDescriptor declaredType) :
            base("unsupported", declaredType)
		{

		}
	}

    [Serializable]
	internal class ReturnNode : VariableNode
	{
		public ReturnNode(TypeDescriptor declaredType) :
            base("return", declaredType)
		{
		}
	}

    [Serializable]
	internal class VariableNode : AnalysisNode
	{
		public VariableNode(string name, TypeDescriptor declaredType) :
            base(name, declaredType)
		{

		}
	}
        
    [Serializable]
	internal class FieldNode : AnalysisNode
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
	internal class DelegateVariableNode : VariableNode
	{
		internal DelegateVariableNode(string name,TypeDescriptor declaredType)
			: base(string.Format("delegate {0}", name),declaredType)
		{ }
	}

    [Serializable]
	internal class AnalysisCallNode : AnalysisNode
	{
		public InvocationDescriptor LocationDescriptor { get; private set; }
        public int InMethodOrder { get; private set; }
		public AnalysisCallNode(TypeDescriptor declaredType, InvocationDescriptor location)
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
            return analysisCallNode!=null && analysisCallNode.LocationDescriptor.Equals(this.LocationDescriptor);
		}
		public override int GetHashCode()
		{
			return base.GetHashCode() + this.LocationDescriptor.GetHashCode();
		}
	}
/*
    internal abstract class AnalysisType
    {
        internal TypeDescriptor TypeDescriptor { get; private set; }

        internal AnalysisType(TypeDescriptor typeDescriptor)
        {
            this.TypeDescriptor = typeDescriptor;
        }
        internal abstract bool IsConcrete();
        public bool IsSubtype(AnalysisType analysisType)
        {
            throw new NotImplementedException();
            //var res = TypeHelper.InheritsByName(this.RoslynType, at.RoslynType);
            //return res;
        }
    }
    internal class ConcreteType: AnalysisType
    {
        public ConcreteType(TypeDescriptor typeDescriptor)
            :base(typeDescriptor)
        { 
        }
        internal override bool IsConcrete()
        {
            return true;
        }
    }
    internal class DeclaredType : AnalysisType
    {
        public DeclaredType(TypeDescriptor typeDescriptor)
            : base(typeDescriptor)
        {
        }
        internal override bool IsConcrete()
        {
            return false;
        }
    } 
 */
/*		
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
}