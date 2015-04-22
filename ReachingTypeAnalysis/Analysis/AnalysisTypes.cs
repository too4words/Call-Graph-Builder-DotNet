﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace ReachingTypeAnalysis
{
	/// <summary>
	/// This is essentially a node in the propagation graph 
	/// It represents either a variable, field, parameter or even a method invocation (that are specials)
	/// Currently they conrtain information about the corresponding Roslyn symbol they represent 
	/// But the idea is to get rid of roslyn into (maybe keeping only the syntax expression they denote)
	/// </summary>
	public class AnalysisNode
	{
		// For debugging
		public static bool UseType;
		public static bool UseSymbol;
		public static bool UseExpression;
		//
		private string name;

		internal static ExpressionSyntax declaredTypeExpression = SyntaxFactory.ParseExpression("*DT*");
		internal AnalysisLocation locationRef;

		protected AnalysisNode(ITypeSymbol declaredType)
		{
			if (declaredType != null)
			{
				this.name = declaredType.Name.ToString();
			}
			else
			{
				this.name = "Dummy";
			}
			this.DeclaredType = declaredType;
		}

		protected AnalysisNode(ITypeSymbol declaredType, SyntaxNodeOrToken syntaxExpression)
		{
			this.DeclaredType = declaredType;
			this.Expression = syntaxExpression;
		}
		protected AnalysisNode(ITypeSymbol declaredType, SyntaxNodeOrToken syntaxExpression, ISymbol symbol)
		{
			this.DeclaredType = declaredType;
			this.Expression = syntaxExpression;
			this.Symbol = symbol;
		}

		public static AnalysisNode Define(ITypeSymbol declaredType, SyntaxNodeOrToken syntaxExpression, ISymbol symbol)
		{
			if (Utils.IsTypeForAnalysis(declaredType))
				return new AnalysisNode(declaredType, syntaxExpression, symbol);
			return null;
		}

		public static AnalysisNode DeclaredUnsupportedTypedExpression(ITypeSymbol dt)
		{
			if (Utils.IsTypeForAnalysis(dt))
			{
				var an = new AnalysisNode(dt, declaredTypeExpression);
				//an.ana = new DeclaredType(dt);
				return an;
			}
			return new AnalysisNode(null);
		}

		internal SyntaxNodeOrToken Expression { get; private set; }

		internal ISymbol Symbol { get; private set; }

		internal ITypeSymbol DeclaredType { get; private set; }

		internal AnalysisLocation LocationReference
		{
			//get { return new ALocation(this.expression.GetLocation()); }
			get
			{
				if (locationRef == null)
				{
					locationRef = new AnalysisLocation(this.Expression);
				}

				return locationRef;
			}
		}

		private AnalysisType analysisType;
		internal AnalysisType AnalysisType
		{
			get
			{
				if (analysisType == null)
				{
					analysisType = new ConcreteType(this.DeclaredType);
				}
				return analysisType;
			}
		}

		/// <summary>
		///  To Do: Fix this. 
		///  I was playing with different notions of equality due to the use in roslyn of IDs
		///  This last attempt use locations
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			var node = (obj as AnalysisNode);
			return //((symbol == null || n.symbol == null) && this.Symbol.ToString() == this.SymbolToString())
				this.Expression.ToString() == node.Expression.ToString()
				 // this.expression.GetLocation().ToString() == n.expression.GetLocation().ToString()
				 && ((this.DeclaredType == null || node.DeclaredType == null) || this.DeclaredType.ToString() == node.DeclaredType.ToString());
			/*
            bool equalType = (declaredTyped == null || n.declaredTyped == null) || n.declaredTyped.Equals(declaredTyped);
            bool equalSymbol = (symbol == null || n.symbol == null) || n.symbol.Equals(symbol);
            bool equalExp = (expression == null || n.expression == null) || n.expression.ToString().Equals(expression.ToString());
            return equalType && equalSymbol && equalExp;
             */
		}

		public override int GetHashCode()
		{
			int hashType = this.DeclaredType == null ? 1 : this.DeclaredType.ToString().GetHashCode();

			return this.Expression.ToString().GetHashCode() + hashType;
			//return this.expression.GetLocation().ToString().GetHashCode() +hashType;
			//int hashType = declaredTyped == null ? 1 : this.DeclaredTyped.GetHashCode();
			//int hasSymbol = symbol == null ? 1 : this.Symbol.GetHashCode();
			//int hasE = expression == null ? 1 : this.Expression.ToString().GetHashCode();
			//return hashType + hasSymbol +hasE;
		}

		public override string ToString()
		{
			string tString = "Dummy";
			if (this.DeclaredType != null)
				tString = this.DeclaredType.Name.ToString();
			string lString = "";
			if (this.LocationReference != null)
				lString = "@" + this.LocationReference.ToString();
			string eString = "";
			if (this.Expression != null)
				eString = this.Expression.ToString();
			string eSymbol = "";
			if (this.Symbol != null && (this.Symbol.Kind == SymbolKind.Field))
				eSymbol = this.Symbol.ContainingType.Name.ToString() + ".";

			return eSymbol + eString + lString + ":" + tString;
		}
	}

	internal class ParameterNode : AnalysisNode
	{
		public int Position { get; private set; }
		public ParameterNode(int position, ITypeSymbol declaredType, SyntaxNodeOrToken syntaxExpression, ISymbol symbol) : base(declaredType, syntaxExpression, symbol)
		{
			this.Position = position;
		}
	}

	internal class ThisNode : AnalysisNode
	{
		public ThisNode(ITypeSymbol declaredType, SyntaxNodeOrToken syntaxExpression, ISymbol symbol) : base(declaredType, syntaxExpression, symbol)
		{
		}
	}

	internal class ReturnNode : AnalysisNode
	{
		public ReturnNode(ITypeSymbol declaredType, SyntaxNodeOrToken syntaxExpression, ISymbol symbol) : base(declaredType, syntaxExpression, symbol)
		{
		}
	}

	internal class VariableNode : AnalysisNode
	{
		public string VarName { get; private set; }
		public VariableNode(string name, ITypeSymbol declaredType, SyntaxNodeOrToken syntaxExpression, ISymbol symbol) : base(declaredType, syntaxExpression, symbol)
		{
			this.VarName = name;
		}
	}

	internal class FieldNode : AnalysisNode
	{
		public string ClassName { get; private set; }
		public string Field { get; private set; }
		public FieldNode(string className, string fname, ITypeSymbol declaredType, SyntaxNodeOrToken syntaxExpression, ISymbol symbol)
			: base(declaredType, syntaxExpression, symbol)
		{
			this.ClassName = className;
			this.Field = fname;
		}
	}

	internal class DelegateNode : AnalysisNode
	{
		protected DelegateNode(ITypeSymbol declaredType, SyntaxNodeOrToken ex)
			: base(declaredType, ex)
		{ }

		public static DelegateNode Define(ITypeSymbol declaredType, SyntaxNodeOrToken ex)
		{
			if (declaredType.TypeKind.Equals(TypeKind.Delegate))
			{
				return new DelegateNode(declaredType, ex);
			}
			else
			{
				return null;
			}
		}

		public virtual bool IsDelegate
		{
			get { return true; }
		}
	}

	internal class AnalysisCallNode : AnalysisNode
	{
		public AnalysisCallNode(ITypeSymbol declaredType, SyntaxNodeOrToken syntaxExpression)
			: base(declaredType, syntaxExpression)
		{ }

		/// <summary>
		/// The idea is to use the same hash and equals than ANode but also locations
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			var eqLoc = (obj as AnalysisCallNode).LocationReference.Equals(this.LocationReference);
			return base.Equals(obj) && eqLoc;
		}
		public override int GetHashCode()
		{
			return base.GetHashCode() + this.LocationReference.GetHashCode();
		}

		private static SyntaxNodeOrToken AddAnnotation(SyntaxNodeOrToken syntaxExpression)
		{
			var annotation = new SyntaxAnnotation(syntaxExpression.ToString());
			return syntaxExpression.WithAdditionalAnnotations(annotation);
		}
	}

	//public interface AnalysisType
	//{
	//    bool IsSubtype(AnalysisType t);
	//    bool IsConcreteType { get; }
	//    bool IsDelegate { get;  }
	//}

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

		public virtual bool IsDelegate
		{
			get { return RoslynType != null && RoslynType.TypeKind == TypeKind.Delegate; }
		}

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

	internal class AnalysisMethod
	{
		private IMethodSymbol method;

		internal IMethodSymbol RoslynMethod
		{
			get { return method; }
			private set { method = value; }
		}
		internal AnalysisMethod(IMethodSymbol method)
		{
			this.method = method;
		}

		internal AnalysisMethod(IPropertySymbol property)
		{
			this.method = property.GetMethod;
		}

		public override bool Equals(object obj)
		{
			AnalysisMethod m = obj as AnalysisMethod;
			return m != null && method.ToString().Equals(m.method.ToString());
		}

		public override int GetHashCode()
		{
			return method.ToString().GetHashCode();
		}

		public override string ToString()
		{
			return method.ToString();
		}

		internal AnalysisType ContainerType
		{
			// It is concrete or declared?
			get { return new ConcreteType(method.ContainingType); }
		}

		internal AnalysisMethod FindMethodImplementation(AnalysisType t)
		{
			AnalysisType aType = t as AnalysisType;
			var realCallee = Utils.FindMethodImplementation(this.RoslynMethod, aType.RoslynType);
			if (realCallee != null)
			{
				return new AnalysisMethod(realCallee);
			}
			return this;
		}

		internal MethodDescriptor MethodDescriptor
		{
			get { return new MethodDescriptor(this.RoslynMethod); }
		}
	}

	public class AnalysisLocation
	{
		private SyntaxNodeOrToken expression;
		private int relativeNumber;
		public Location Location { get; private set; }
		internal AnalysisLocation(Location location)
		{
			this.Location = location;
		}

		internal AnalysisLocation(SyntaxNodeOrToken syntax)
		{
			this.Location = syntax.GetLocation();
			this.expression = syntax;
		}

		public override bool Equals(object obj)
		{
			var loc = obj as AnalysisLocation;
			//return obj != null && expression.ToString().Equals(loc.expression.ToString());
			//return obj != null && Location.GetLineSpan().Equals(loc.Location.GetLineSpan());
			return obj != null && (Location == null && loc.Location == null) || Location.SourceSpan.Start.Equals(loc.Location.SourceSpan.Start);
		}

		public override int GetHashCode()
		{
			//return Location.SourceSpan.GetHashCode()+ Location.SourceSpan.Start;
			//return Location.GetHashCode();
			//return expression.ToString().GetHashCode();
			return Location != null ? Location.GetLineSpan().GetHashCode() : 1;
		}
	}
}