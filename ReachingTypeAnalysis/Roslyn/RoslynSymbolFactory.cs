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
            var type = compilation.GetTypeByMetadataName(methodDescriptor.ClassName);
            // Another way to access the info
            if (type == null)
            {
                Func<string, bool> pred = s => { return s.Equals(methodDescriptor.ClassName); };
                var symbols = compilation.GetSymbolsWithName(pred, SymbolFilter.Type);
                if (symbols.Count() > 0)
                {
                    type = symbols.First() as INamedTypeSymbol;
                }
            }
            if (type != null)
            {
                var members = type.GetMembers(methodDescriptor.MethodName);
                if (members.Count() > 0)
                {
                    var member = members.First() as IMethodSymbol;
                    return member;
                }
            }
            return null;
        }


    }
}
