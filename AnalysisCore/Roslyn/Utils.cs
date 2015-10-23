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
using ReachingTypeAnalysis.Roslyn;
using System.Threading;

namespace ReachingTypeAnalysis
{
	public static class Utils
	{
		public static MethodDescriptor CreateMethodDescriptor(IMethodSymbol method)
		{
			Contract.Assert(method != null);

			if (method.IsGenericMethod)
			{
				method = method.OriginalDefinition;
			}

			var typeDescriptor = Utils.CreateTypeDescriptor(method.ContainingType);

			var result = new MethodDescriptor(typeDescriptor, method.Name, method.IsStatic,
				method.Parameters.Select(parmeter => Utils.CreateTypeDescriptor(parmeter.Type)),
				method.TypeParameters.Select(parameter => parameter.Name),
				Utils.CreateTypeDescriptor(method.ReturnType));

			return result;
		}

		public static IList<Project> FilterProjects(Solution solution)
		{
			var filteredProjects = from project in solution.Projects
								   where project.Language == LanguageNames.CSharp
								   select project;

			return new List<Project>(filteredProjects);
		}

		public static TypeDescriptor CreateTypeDescriptor(ITypeSymbol type, bool isConcrete = true)
		{
			Contract.Assert(type != null);
			var assemblyName = "Unknown";
			var namespaceName = "Unknown";
			var typeName = "Unknown";
			var kind = Utils.Convert(type.TypeKind);

			if (type is INamedTypeSymbol)
			{
				var namedType = type as INamedTypeSymbol;
				assemblyName = namedType.ContainingAssembly.Name;
				namespaceName = Utils.GetFullNamespaceName(namedType);
				typeName = namedType.Name;
			}
			else if (type is ITypeParameterSymbol)
			{
				var typeParameter = type as ITypeParameterSymbol;
				assemblyName = typeParameter.ContainingAssembly.Name;
				namespaceName = Utils.GetFullNamespaceName(typeParameter);
				typeName = typeParameter.Name;
			}
			else if (type is IArrayTypeSymbol)
			{
				var arrayType = type as IArrayTypeSymbol;

				while (arrayType.ElementType is IArrayTypeSymbol)
				{
					arrayType = arrayType.ElementType as IArrayTypeSymbol;
				}

				assemblyName = arrayType.ElementType.ContainingAssembly.Name;
				namespaceName = Utils.GetFullNamespaceName(arrayType.ElementType);
				typeName = arrayType.ElementType.Name;
			}
			else
			{
				var message = string.Format("Unsupported type: {0}", type);
				throw new Exception(message);
			}

			var result = new TypeDescriptor(namespaceName, typeName, assemblyName, type.IsReferenceType, kind, isConcrete);
			return result;
		}

		private static string GetFullNamespaceName(ISymbol symbol)
		{
			var namespaceName = string.Empty;

			if (symbol.ContainingNamespace != null && !symbol.ContainingNamespace.IsGlobalNamespace)
			{
				namespaceName = symbol.ContainingNamespace.ToDisplayString();
			}

			return namespaceName;
		}

		private static SerializableTypeKind Convert(TypeKind kind)
		{
			switch (kind)
			{
				case TypeKind.Class: return SerializableTypeKind.Class;
				case TypeKind.Interface: return SerializableTypeKind.Interface;
				case TypeKind.Delegate: return SerializableTypeKind.Delegate;
				case TypeKind.TypeParameter: return SerializableTypeKind.TypeParameter;
				case TypeKind.Array: return SerializableTypeKind.Array;
				case TypeKind.Struct: return SerializableTypeKind.Struct;
				case TypeKind.Module: return SerializableTypeKind.Module;
				case TypeKind.Enum: return SerializableTypeKind.Enum;
				case TypeKind.Pointer: return SerializableTypeKind.Pointer;
				case TypeKind.Dynamic: return SerializableTypeKind.Dynamic;
				case TypeKind.Error: return SerializableTypeKind.Error;
				case TypeKind.Submission: return SerializableTypeKind.Submission;
				case TypeKind.Unknown: return SerializableTypeKind.Unknown;
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
					type.TypeKind == Microsoft.CodeAnalysis.TypeKind.TypeParameter);    // || t.SpecialType==SpecialType.System_Void);
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
						if (candidate.Kind == SymbolKind.Property)
						{
							var property = (IPropertySymbol)candidate;
							result = method.MethodKind == MethodKind.PropertyGet ? property.GetMethod : property.SetMethod;
						}
						else
						{
							result = (IMethodSymbol)candidate;
						}

						if (result.Parameters.Count() == method.Parameters.Count())
						{
							break;
						}
						else
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
			//var methodDeclarationSyntax = expression.AsNode().Ancestors().OfType<BaseMethodDeclarationSyntax>().First();
			var methodDeclarationSyntax = expression.AsNode().Ancestors().OfType<MemberDeclarationSyntax>().First();
			//var syntaxTree = methodDeclarationSyntax.SyntaxTree;
			var invocations = methodDeclarationSyntax.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().ToArray();
			int count = 0;
			for (int i = 0; i < invocations.Length && !invocations[i].GetLocation().Equals(expression.GetLocation()); i++)
			{
				count++;
			}

			return count;
		}

		public static LocationDescriptor CreateLocationDescriptor(int invocationPosition, SyntaxNodeOrToken syntaxNode, SyntaxNode declaratioNode)
		{
			var span = CodeGraphHelper.GetSpan(syntaxNode);
			var range = CodeGraphHelper.GetRange(span);
			var rangeDecRange = CodeGraphHelper.GetRange(CodeGraphHelper.GetSpan(declaratioNode));

			var filePath = span.Path;
			return new LocationDescriptor(invocationPosition, CodeGraphHelper.GetRelativeRange(range,rangeDecRange), filePath);
		}

		public static AnalysisCallNodeAdditionalInfo CreateAnalysisCallNodeAdditionalInfo(ISymbol symbol)
		{
			IMethodSymbol methodSymbol = null;

			if (symbol is IMethodSymbol)
			{
				methodSymbol = symbol as IMethodSymbol;
			}
			else if (symbol is IPropertySymbol)
			{
				// TODO: Hack! How do we know if this is a get or set property access?
				var propertySymbol = symbol as IPropertySymbol;
				methodSymbol = propertySymbol.GetMethod;
			}
			else
			{
				throw new Exception("Unknown symbol type");
			}

			var methodDescriptor = Utils.CreateMethodDescriptor(methodSymbol);
			var declarationPath = symbol.Locations.First().GetMappedLineSpan().Path;
			var displayString = symbol.ToDisplayString();
			return new AnalysisCallNodeAdditionalInfo(methodDescriptor, declarationPath, displayString);
        }

		public static Task<Project> ReadProjectAsync(string path)
		{
			var props = new Dictionary<string, string>()
			{
				{ "CheckForSystemRuntimeDependency", "true" },
				{ "DesignTimeBuild", "true" },
				{ "IntelliSenseBuild", "true" },
				{ "BuildingInsideVisualStudio", "true" }
			};

			var ws = MSBuildWorkspace.Create(props);
			return ws.OpenProjectAsync(path);
		}

		public static Task<Solution> ReadSolutionAsync(string path)
		{
			if (!File.Exists(path)) throw new ArgumentException("Missing " + path);

			var props = new Dictionary<string, string>()
			{
				{ "CheckForSystemRuntimeDependency", "true" },
				{ "DesignTimeBuild", "true" },
				{ "IntelliSenseBuild", "true" },
				{ "BuildingInsideVisualStudio", "true" }
			};

			var ws = MSBuildWorkspace.Create(props);
			return ws.OpenSolutionAsync(path);
		}

		public static Solution ReadSolution(string path)
		{
			var solution = Utils.ReadSolutionAsync(path).Result;
			return solution;
		}

		public static async Task<Compilation> CompileProjectAsync(Project project, CancellationToken cancellationToken)
		{
			//Console.WriteLine("Project language {0}", project.Language);
			var cancellationTokenSource = new CancellationTokenSource();
			var compilation = await project.GetCompilationAsync(cancellationTokenSource.Token);
			var diagnostics = compilation.GetDiagnostics();

			// print compilation errors and warnings
			var errors = from diagnostic in diagnostics
						 where diagnostic.Severity == DiagnosticSeverity.Error
						 select diagnostic.ToString();

			if (errors.Any())
			{
				var folderName = "Roslyn compilation errors";
				var fileName = string.Format("{0}.txt", project.Name);

				Directory.CreateDirectory(folderName);
				fileName = Path.Combine(folderName, fileName);

				Console.WriteLine("Failed to compile project {0}", project.Name);
				Console.WriteLine("To see the error log open file: {0}", fileName);

				File.WriteAllLines(fileName, errors);
			}
			else
			{
				Console.WriteLine("Project {0} compiled successfully", project.Name);
			}

			return compilation;
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
