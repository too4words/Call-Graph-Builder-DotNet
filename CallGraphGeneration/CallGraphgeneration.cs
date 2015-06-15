// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using ReachingTypeAnalysis;
using SolutionTraversal.Callgraph;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Configuration;

namespace CallGraphGeneration
{
    class CallGraphgeneration
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentException("Not enough parameters to main");
            }
            var solutionPath = args[0];
            var solution = ReadSolution(solutionPath);
            var callGraph = BuildCallGraph(solution);
            callGraph.Save("cg.dot");
        }

        public static Solution ReadSolution(string path)
        {
            var ws = MSBuildWorkspace.Create();

            var solution = ws.OpenSolutionAsync(path).Result;
            string pathNetFramework = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            string pathToDll = pathNetFramework + @"Facades\";
            //@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5.1\Facades\";
            // Didn't work 
            pathToDll = ConfigurationManager.AppSettings["PathToDLLs"];
            Contract.Assert(Directory.Exists(pathToDll));
            var metadataReferences = new string[] { 
                    "System.Runtime.dll", 
                    "System.Threading.Tasks.dll", 
                    "System.Reflection.dll",
                    "System.Text.Encoding.dll"}.Select(s => MetadataReference.CreateFromFile(pathToDll + s));
            var projectsID = solution.ProjectIds;
            foreach(var pID in projectsID)
            {
                solution = solution.AddMetadataReferences(pID, metadataReferences);
            }
            return solution;
        }

        //public static CallGraph<IMethodSymbol,Location> BuildCallGraph(Solution solution)
		public static CallGraph<MethodDescriptor, LocationDescriptor> BuildCallGraph(Solution solution)
        {
            var analyzer = new SolutionAnalyzer(solution);
            analyzer.Analyze();
            //analyzer.GenerateCallGraph();
            var callgraph = analyzer.GenerateCallGraph();
            Console.WriteLine("Reachable methods={0}", callgraph.GetReachableMethods().Count);
            return callgraph;
        }
    }
}
