// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Roslyn
{
    public struct ProjectMethod
    {
        public Project Project;
        public IMethodSymbol Method;
    }

    public static class RoslynSymbolFactory
    {
        public static IMethodSymbol FindMethodSymbolInSolution(Solution solution, MethodDescriptor methodDescriptor)
        {
            return FindMethodSymbolAndProjectInSolution(solution, methodDescriptor).Method;
        }

        public static ProjectMethod FindMethodSymbolAndProjectInSolution(Solution solution, MethodDescriptor methodDescriptor)
        {
            ProjectMethod res;
            res.Method = null;
            res.Project = null;

            foreach (var project in solution.Projects)
            {
                // Alternative method using SymbolFinder, it only works with methods in the solution 
                // Discarded by the moment
                //var methodDeclarations = SymbolFinder.FindDeclarationsAsync(project, methodDescriptor.MethodName,false).Result;
                //methodDeclarations = methodDeclarations.Where(ms => methodDescriptor.SameAsMethodSymbom(ms as IMethodSymbol));
                //if (methodDeclarations.Count() > 0)
                //{
                //    return methodDeclarations.First() as IMethodSymbol;
                //}
                //else
                //{
                // My own method
                var method = FindMethodSymbolInProject(methodDescriptor, project);
                if (method != null)
                {
                    res.Project = project;
                    res.Method = method;
                    return res;
                }
                //}
            }

            return res;
        }

        private static IMethodSymbol FindMethodSymbolInProject(MethodDescriptor methodDescriptor, Project project)
        {
            var compilation = project.GetCompilationAsync().Result;
            return FindMethodInCompilation(methodDescriptor, compilation);
        }

		private static INamedTypeSymbol GetSpecializedTypeByName(TypeDescriptor typeDescriptor, Compilation compilation)
		{
			var type = GetTypeByName(typeDescriptor, compilation);

			if (type != null)
			{
				if (type.IsGenericType)
				{
					var typeArguments = from t in typeDescriptor.TypeArguments
										select GetSpecializedTypeByName(t, compilation);

					type = type.Construct(typeArguments.ToArray());
				}
			}

			return type;
		}

		public static IMethodSymbol FindMethodInCompilation(MethodDescriptor methodDescriptor, Compilation compilation)
        {
			var type = GetTypeByName(methodDescriptor.ContainerType, compilation);
			//var type = GetSpecializedTypeByName(methodDescriptor.ContainerType, compilation);

			if (type != null)
            {
				//if (type.IsGenericType)
				//{
				//	var typeArguments = from t in methodDescriptor.ContainerType.TypeArguments
				//						select GetTypeByName(t, compilation);

				//	type = type.Construct(typeArguments.ToArray());
				//}

                var members = type.GetMembers(methodDescriptor.MethodName);

                if (members.Count() > 0)
                {
					var methods = from member in members
								  let methodSymbol = member as IMethodSymbol
								  let descriptor = Utils.CreateMethodDescriptor(methodSymbol)
								  where descriptor.EqualsIgnoringTypeArguments(methodDescriptor)
								  select methodSymbol;

					if (methods.Count() != 1)
					{
						Console.WriteLine("[Error] Couldn't FindMethodInCompilation '{0}'", methodDescriptor);
					}

					var method = methods.SingleOrDefault();
					return method;

					//var member = members.First() as IMethodSymbol;
					//return member;
				}
            }

            return null;
        }

        public static INamedTypeSymbol GetTypeByName(TypeDescriptor typeDescriptor, Compilation compilation)
        {
            //return GetTypeByName(typeDescriptor.TypeName, compilation);
			var type = compilation.GetTypeByMetadataName(typeDescriptor.QualifiedMetadataTypeName);
			
			// Another way to access the info
			if (type == null)
			{
				//var references = compilation.References
				//					.OfType<CompilationReference>()
				//					.ToDictionary(r => r.Compilation.AssemblyName, r => r.Compilation);

				//if (references.ContainsKey(typeDescriptor.AssemblyName))
				//{
				//	compilation = references[typeDescriptor.AssemblyName];
				//}

				Func<string, bool> pred = s => s.Equals(typeDescriptor.ClassName);
				var symbols = compilation.GetSymbolsWithName(pred, SymbolFilter.Type).OfType<INamedTypeSymbol>();

				if (symbols.Count() > 0)
				{
					type = symbols.SingleOrDefault(t => typeDescriptor.Equals(Utils.CreateTypeDescriptor(t)));
				}
			}

			return type;
        }

		public static INamedTypeSymbol GetTypeByName(string className, Compilation compilation)
        {
            var type = compilation.GetTypeByMetadataName(className);
            // Another way to access the info
            if (type == null)
            {
                Func<string, bool> pred = s => { return s.Equals(className); };
                var symbols = compilation.GetSymbolsWithName(pred, SymbolFilter.Type);
                if (symbols.Count() > 0)
                {
                    type = symbols.First() as INamedTypeSymbol;
                }
            }
            return type;
        }
    }
}
