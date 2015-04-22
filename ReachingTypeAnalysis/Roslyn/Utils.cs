// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReachingTypeAnalysis
{
	public class Utils
	{
		internal static bool IsTypeForAnalysis(SemanticModel model, ExpressionSyntax node)
		{
			var type = model.GetTypeInfo(node).Type;
			return type != null && IsTypeForAnalysis(type);
		}
		internal static bool IsTypeForAnalysis(AnalysisType analysisType)
		{
			var res = IsTypeForAnalysis(analysisType.RoslynType);
			return res;
		}
		internal static bool IsTypeForAnalysis(ITypeSymbol t)
		{
			var res = t != null && (t.IsReferenceType || t.TypeKind == TypeKind.TypeParameter);	// || t.SpecialType==SpecialType.System_Void);
			return res;
		}

		internal static IMethodSymbol FindMethodImplementation(IMethodSymbol method, ITypeSymbol rType)
		{
			IMethodSymbol result = null;

			var methodOrProperty = method.AssociatedSymbol != null ? method.AssociatedSymbol : method;

			// Diego: Need to provide the complete signature
			//var candidates = rType.GetMembers().Where(s => s.Name.Equals(method.Name));
			do
			{
				var candidates = rType.GetMembers(methodOrProperty.Name);
				//var m2 = method.ReduceExtensionMethod(rType);
				if (candidates.Count() > 0)
				{
					foreach (var candidate in candidates)
					{
						var candidateMethodOrProperty = candidates.First();
						if (candidateMethodOrProperty.Kind == SymbolKind.Property)
						{
							result = ((IPropertySymbol)candidateMethodOrProperty).GetMethod;
						}
						else
						{
							result = (IMethodSymbol)candidateMethodOrProperty;
						}
						// This is rough, just to test 
						if (result.Parameters.Count() != method.Parameters.Count())
						{
							result = null;
						}
					}

				}
				rType = rType.BaseType;
			} while (result == null && rType != null);
			return result;
		}

		/// <summary>
		/// Find the node in the AST where the method was declared
		/// We use that to visit a callee method. 
		/// </summary>
		/// <param name="solution"></param>
		/// <param name="roslynMethod"></param>
		/// <returns></returns>
		internal static SyntaxNode FindMethodDeclaration(IMethodSymbol roslynMethod)
		{
			var nodes = roslynMethod.DeclaringSyntaxReferences;
			if (nodes.Count() > 0)
			{
				return nodes[0].GetSyntax();
			}
			else
			{
				return null;
			}

			//var position = roslynMethod.Locations.First().SourceSpan.Start;
			//var st = roslynMethod.Locations.First().SourceTree;
			//if (st != null)
			//{
			//    var node = st.GetRoot().FindToken(position).Parent.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>();
			//    return node;
			//}
			//else return null;
		}

		private static MetadataReference mscorlib;

		internal static MetadataReference Mscorlib
		{
			get
			{
				if (mscorlib == null)
				{
					mscorlib = MetadataReference.CreateFromAssembly(typeof(object).Assembly);
				}

				return mscorlib;
			}
		}

		internal static Solution CreateSolution(string source)
		{
			var projectId = ProjectId.CreateNewId();
			var documentId = DocumentId.CreateNewId(projectId);
			var tree = SyntaxFactory.ParseSyntaxTree(source);

			var ws = MSBuildWorkspace.Create();
			var solution = ws.CurrentSolution
				.AddProject(projectId, "MyProject", "MyProject", LanguageNames.CSharp)
				.AddMetadataReference(projectId, Mscorlib)
				.AddDocument(documentId, "MyFile.cs", source);
			return solution;
		}
	}

	public class PairIterator<T1, T2> : IEnumerable<Tuple<T1, T2>>
	{
		private IEnumerable<T1> enumerator1;
		private IEnumerable<T2> enumerator2;

		public PairIterator(IEnumerable<T1> enumerator1, IEnumerable<T2> enumerator2)
		{
			this.enumerator1 = enumerator1;
			this.enumerator2 = enumerator2;
		}

		public IEnumerator<Tuple<T1, T2>> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return new MyIEnumerator(this.enumerator1, this.enumerator2);
		}

		internal class MyIEnumerator : IEnumerator<Tuple<T1, T2>>
		{
			private IEnumerator<T1> enumerator1;
			private IEnumerator<T2> enumerator2;

			public MyIEnumerator(IEnumerable<T1> ienumerable1, IEnumerable<T2> ienumerable2)
			{
				this.enumerator1 = ienumerable1.GetEnumerator();
				this.enumerator2 = ienumerable2.GetEnumerator();
			}

			public Tuple<T1, T2> Current
			{
				get
				{
					return new Tuple<T1, T2>(enumerator1.Current, enumerator2.Current);
				}
			}

			object System.Collections.IEnumerator.Current
			{
				get
				{
					return new Tuple<T1, T2>(enumerator1.Current, enumerator2.Current);
				}
			}

			public void Dispose()
			{
				this.enumerator1.Dispose();
				this.enumerator2.Dispose();
			}

			public bool MoveNext()
			{
				return this.enumerator1.MoveNext() && this.enumerator2.MoveNext();
			}

			public void Reset()
			{
				this.enumerator1.Reset();
				this.enumerator2.Reset();
			}
		}
	}

	/// <summary>
	/// We use this Bag for 'counting' number of references of concrete types.
	/// Actually we want to keep track of the number of paths with a concrete type leading to a node
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class Bag<T>
	{
		private int count = 0;
		private IDictionary<T, int> multiset = new Dictionary<T, int>();
		private HashSet<T> internalSet = new HashSet<T>();
		internal int Add(T e)
		{
			int c;
			if (!multiset.TryGetValue(e, out c))
			{
				c = 0;
				internalSet.Add(e);
			}
			multiset[e] = c + 1;
			count++;

			return c + 1;
		}
		public int Occurrences(T e)
		{
			int c;
			if (multiset.TryGetValue(e, out c))
			{
				return c;
			}
			return 0;
		}
		public void UnionWith(IEnumerable<T> set)
		{
			foreach (var e in set)
			{
				Add(e);
			}
		}
		public bool Remove(T e)
		{
			int c;
			if (!multiset.TryGetValue(e, out c) || c <= 0)
			{
				return false;
			}
			else
			{
				if (c == 1)
				{
					internalSet.Remove(e);
					multiset.Remove(e);
				}
				else
					multiset[e] = c - 1;
				count--;
			}
			return true;
		}


		public ISet<T> ExceptWith(IEnumerable<T> set)
		{
			var res = new HashSet<T>();
			foreach (var e in set)
			{
				if (Remove(e))
					res.Add(e);
			}
			return res;
		}
		public bool Contains(T e)
		{
			return Occurrences(e) > 0;
		}
		public ISet<T> AsSet()
		{
			if (internalSet.Count > this.Count)
			{
				return new HashSet<T>(multiset.Keys);
			}
			return internalSet;
		}
		public int Count
		{
			get { return count; }
		}
	}


	public static class TypeHelper
	{
		public static bool InheritsByName(this ITypeSymbol type, ITypeSymbol possibleBase)
		{
			if (type == null || possibleBase == null)
			{
				return false;
			}

			if (type.ToString().Equals(possibleBase.ToString()))
			{
				return true;
			}

			switch (possibleBase.TypeKind)
			{
				case TypeKind.Class:
					for (ITypeSymbol t = type.BaseType; t != null; t = t.BaseType)
					{
						if (t.ToString().Equals(possibleBase.ToString()))
						{
							return true;
						}
					}

					return false;

				case TypeKind.Interface:
					foreach (var i in type.AllInterfaces)
					{
						if (i.ToString().Equals(possibleBase.ToString()))
						{
							return true;
						}
					}

					return false;

				default:
					return false;
			}
		}

		public static bool Inherits(this ITypeSymbol type, ITypeSymbol possibleBase)
		{
			if (type == null || possibleBase == null)
			{
				return false;
			}

			if (type.Equals(possibleBase))
			{
				return true;
			}

			switch (possibleBase.TypeKind)
			{
				case TypeKind.Class:
					for (ITypeSymbol t = type.BaseType; t != null; t = t.BaseType)
					{
						if (t.Equals(possibleBase))
						{
							return true;
						}
					}

					return false;

				case TypeKind.Interface:
					foreach (var i in type.AllInterfaces)
					{
						if (i.Equals(possibleBase))
						{
							return true;
						}
					}

					return false;

				default:
					return false;
			}
		}

		public static IEnumerable<INamedTypeSymbol> GetBaseTypes(this ITypeSymbol type)
		{
			var current = type.BaseType;
			while (current != null)
			{
				yield return current;
				current = current.BaseType;
			}
		}
	}
}
