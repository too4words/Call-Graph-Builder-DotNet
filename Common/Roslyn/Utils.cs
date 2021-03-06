﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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

		internal static bool IsTypeForAnalysis(TypeDescriptor t)
		{
			Contract.Assert(t != null);

			return (t.IsReferenceType || t.Kind == TypeKind.TypeParameter);
		}
	
		internal static bool IsTypeForAnalysis(ITypeSymbol t)
		{
			var res = t != null && (t.IsReferenceType || t.TypeKind == TypeKind.TypeParameter);	// || t.SpecialType==SpecialType.System_Void);
			return res;
		}

        internal static MethodDescriptor FindMethodDescriptorForType(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
        {
            return new MethodDescriptor(typeDescriptor.TypeName, methodDescriptor.MethodName);
            // throw new NotImplementedException("To implement this method we need type resolution");
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
