// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System.IO;
using System.Configuration;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis
{
    public class Utils
	{
        public static MethodDescriptor CreateMethodDescriptor(IMethodSymbol method)
        {
            Contract.Assert(method != null);

            return new MethodDescriptor(
                method.ContainingNamespace.Name,
                method.ContainingType.Name, method.Name, method.IsStatic, 
                method.Parameters.Select(parmeter => Utils.CreateTypeDescriptor(parmeter.Type)),
                Utils.CreateTypeDescriptor(method.ReturnType)
				);            
        }

        public static TypeDescriptor CreateTypeDescriptor(ITypeSymbol type, bool isConcrete = true)
        {
            //public TypeDescriptor(string nameSpaceName, string className, bool isReferenceType = true, bool isConcrete = true)
            return new TypeDescriptor(type.MetadataName, type.IsReferenceType, Convert(type.TypeKind), isConcrete);
        }

        private static SerializableTypeKind Convert(Microsoft.CodeAnalysis.TypeKind kind) {
            switch (kind)
            {
                case Microsoft.CodeAnalysis.TypeKind.Class: return SerializableTypeKind.Class;
                case Microsoft.CodeAnalysis.TypeKind.Interface: return SerializableTypeKind.Interface;
                case Microsoft.CodeAnalysis.TypeKind.Delegate: return SerializableTypeKind.Delegate;
                case Microsoft.CodeAnalysis.TypeKind.TypeParameter: return SerializableTypeKind.TypeParameter;
                case Microsoft.CodeAnalysis.TypeKind.Array: return SerializableTypeKind.Array;
                case Microsoft.CodeAnalysis.TypeKind.Struct: return SerializableTypeKind.Struct;
				case Microsoft.CodeAnalysis.TypeKind.Module: return SerializableTypeKind.Module;
				case Microsoft.CodeAnalysis.TypeKind.Enum: return SerializableTypeKind.Enum;
				case Microsoft.CodeAnalysis.TypeKind.Pointer: return SerializableTypeKind.Pointer;
				case Microsoft.CodeAnalysis.TypeKind.Dynamic: return SerializableTypeKind.Dynamic;
				case Microsoft.CodeAnalysis.TypeKind.Error: return SerializableTypeKind.Error;
				case Microsoft.CodeAnalysis.TypeKind.Submission: return SerializableTypeKind.Submission;
				case Microsoft.CodeAnalysis.TypeKind.Unknown: return SerializableTypeKind.Unknown;
				default: throw new ArgumentException("Can't convert " + kind);
            }
        }

        internal static bool IsTypeForAnalysis(SemanticModel model, ExpressionSyntax node)
		{
			var type = model.GetTypeInfo(node).Type;
			return type != null && IsTypeForAnalysis(type);
		}

		internal static bool IsTypeForAnalysis(TypeDescriptor t)
		{
			Contract.Assert(t != null);

			return (t.IsReferenceType || t.Kind == SerializableTypeKind.TypeParameter);
		}
	
		internal static bool IsTypeForAnalysis(ITypeSymbol type)
		{
			var res = type != null && 
                (type.IsReferenceType || 
                    type.TypeKind == Microsoft.CodeAnalysis.TypeKind.TypeParameter);	// || t.SpecialType==SpecialType.System_Void);
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

		///// <summary>
		///// Find the node in the AST where the method was declared
		///// We use that to visit a callee method. 
		///// </summary>
		///// <param name="solution"></param>
		///// <param name="roslynMethod"></param>
		///// <returns></returns>
		//internal static SyntaxReference FindMethodDeclaration(IMethodSymbol roslynMethod)
		//{
		//	var nodes = roslynMethod.DeclaringSyntaxReferences;
		//	if (nodes.Count() > 0)
		//	{
		//		return nodes[0].GetSyntax();
		//	}
		//	else
		//	{
		//		return null;
		//	}

		//	//var position = roslynMethod.Locations.First().SourceSpan.Start;
		//	//var st = roslynMethod.Locations.First().SourceTree;
		//	//if (st != null)
		//	//{
		//	//    var node = st.GetRoot().FindToken(position).Parent.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>();
		//	//    return node;
		//	//}
		//	//else return null;
		//}

        internal static int GetInvocationNumber(IMethodSymbol roslynMethod, SyntaxNodeOrToken invocation)
        {
            // var roslynMethod = RoslynSymbolFactory.FindMethodSymbolInSolution(this.solution, locMethod.Value);
            var methodDeclarationSyntax = roslynMethod.DeclaringSyntaxReferences.First();
            //var syntaxTree = methodDeclarationSyntax.SyntaxTree;
            var invocations = methodDeclarationSyntax.GetSyntax().DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().ToArray();
            int count = 0;
            for (int i = 0; i < invocations.Length && !invocations[i].GetLocation().Equals(invocation.GetLocation()); i++)
            {
                count++;
            }

            return count;
        }

        internal static int GetStatementNumber(SyntaxNodeOrToken expression)
        {
            var methodDeclarationSyntax = expression.AsNode().Ancestors().OfType<MethodDeclarationSyntax>().First();
            //var syntaxTree = methodDeclarationSyntax.SyntaxTree;
            var invocations = methodDeclarationSyntax.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().ToArray();
            int count = 0;
            for (int i = 0; i < invocations.Length && !invocations[i].GetLocation().Equals(expression.GetLocation()); i++)
            {
                count++;
            }

            return count;
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
        public static async Task<Project> ReadProjectAsync(string path)
        {
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            return await workspace.OpenProjectAsync(path);

        }
        public static Solution ReadSolution(string path)
        {
            if (!File.Exists(path)) throw new ArgumentException("Missing " + path);
            var ws = MSBuildWorkspace.Create();

            var solution = ws.OpenSolutionAsync(path).Result;
            //string pathNetFramework = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            //string pathToDll = pathNetFramework + @"Facades\";        
            // Didn't work 
            // These ones works
            //pathToDll = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5.1\Facades\";
            string pathToDll = ConfigurationManager.AppSettings["PathToDLLs"];
            Contract.Assert(pathToDll != null && Directory.Exists(pathToDll));

            var metadataReferences = new string[] {
                    "System.Runtime.dll",
                    "System.Threading.Tasks.dll",
                    "System.Reflection.dll",
                    "System.Text.Encoding.dll"}.Select(s => MetadataReference.CreateFromFile(pathToDll + s));
            var pIds = solution.ProjectIds;
            foreach (var pId in pIds)
                solution = solution.AddMetadataReferences(pId, metadataReferences);
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
            return new MyIEnumerator(this.enumerator1, this.enumerator2);
			//throw new NotImplementedException();
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
	[Serializable]
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
				case Microsoft.CodeAnalysis.TypeKind.Class:
					for (ITypeSymbol t = type.BaseType; t != null; t = t.BaseType)
					{
						if (t.ToString().Equals(possibleBase.ToString()))
						{
							return true;
						}
					}

					return false;

				case Microsoft.CodeAnalysis.TypeKind.Interface:
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
				case Microsoft.CodeAnalysis.TypeKind.Class:
					for (ITypeSymbol t = type.BaseType; t != null; t = t.BaseType)
					{
						if (t.Equals(possibleBase))
						{
							return true;
						}
					}

					return false;

				case Microsoft.CodeAnalysis.TypeKind.Interface:
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
