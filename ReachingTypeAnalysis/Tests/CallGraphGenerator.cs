// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReachingTypeAnalysis.Communication;
using SolutionTraversal.Callgraph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ReachingTypeAnalysis
{
    /// <summary>
    /// This will produce randomy generated callgraphs of a given size. 
    /// This class is primarily to be used for testing purposes.
    /// </summary>
    [TestClass]
    public class CallGraphGenerator
    {
        public CallGraphGenerator() { }

        internal static CallGraph<string,int> GenerateCallGraph(int n)
        {
            var result = new CallGraph<string,int>();
            for (var i = 0; i < n; i++)
            {
                result.Add(string.Format("N{0}", i));
            }

            // now generate the edges
            var rand = new Random();
            for (var i = 0; i < 5 * n; i++)
            {
                var source = rand.Next(n - 1);
                var dest = rand.Next(n - 1);

                result.AddCall(string.Format("N{0}", source), string.Format("N{0}", dest));
            }
            result.Add("Main");
            result.AddRootMethod("Main");
            foreach (var method in result.GetNodes())
            {
                result.AddCall("Main", method);
            }

            return result;
        }

        internal static SyntaxNode GenerateCode(CallGraph<string,int> callgraph)
        {
            List<MethodDeclarationSyntax> methods = new List<MethodDeclarationSyntax>();
            foreach (var vertex in callgraph.GetNodes())
            {
                methods.Add(GetMethod(vertex, callgraph.GetCallees(vertex)));
            }
            //methods.Add(GetMain(callgraph.GetNodes()));

            return
                SyntaxFactory.CompilationUnit()
                .WithMembers(
                    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                        SyntaxFactory.ClassDeclaration(
                            @"C")
                        .WithMembers(
                            SyntaxFactory.List<MemberDeclarationSyntax>(methods)
                        )))
                .NormalizeWhitespace();
        }

        private static MethodDeclarationSyntax GetMain(IEnumerable<string> methods)
        {
            return
                SyntaxFactory.MethodDeclaration(
                                SyntaxFactory.PredefinedType(
                                    SyntaxFactory.Token(
                                        SyntaxKind.VoidKeyword)),
                                    SyntaxFactory.Identifier("Main"))
                            .WithModifiers(
                                SyntaxFactory.TokenList(
                                    SyntaxFactory.Token(
                                        SyntaxKind.PublicKeyword),
                                    SyntaxFactory.Token(
                                        SyntaxKind.StaticKeyword)))
                            .WithBody(
                                SyntaxFactory.Block(
                                    SyntaxFactory.List<StatementSyntax>(
                                        GetCallees(methods)
                                   )));
        }

        private static MethodDeclarationSyntax GetMethod(string name, IEnumerable<string> callees)
        {
            return
                SyntaxFactory.MethodDeclaration(
                                SyntaxFactory.PredefinedType(
                                    SyntaxFactory.Token(
                                        SyntaxKind.VoidKeyword)),
                                SyntaxFactory.Identifier(name))
                            .WithModifiers(
                                SyntaxFactory.TokenList(
                                    SyntaxFactory.Token(
                                        SyntaxKind.PublicKeyword),
                                    SyntaxFactory.Token(
                                        SyntaxKind.StaticKeyword)))
                            .WithBody(
                                SyntaxFactory.Block(
                                    SyntaxFactory.List<StatementSyntax>(
                                        GetCallees(callees)
                                   )));
        }

        private static IEnumerable<StatementSyntax> GetCallees(IEnumerable<string> callees)
        {
            foreach (var callee in callees)
            {
                yield return
                    SyntaxFactory.ExpressionStatement(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.IdentifierName(
                                                    callee)));
            }
        }

        [TestMethod]
        [TestCategory("Generation")]
        public void GenerateSimpleCallGraph()
        {
            var callgraph = GenerateCallGraph(10);
            var syntax = GenerateCode(callgraph);
            var code = syntax.ToFullString();
            Console.WriteLine(code);
        }

        [TestMethod]
        [TestCategory("Generation")]
        public void GenerateSimpleSolution()
        {
            var callgraph = GenerateCallGraph(10);
            var syntax = GenerateCode(callgraph);
            var code = syntax.ToFullString();
			Logger.Instance.Log("CallGraphGenerator", "GenerateSimpleSolution", "source code: {0}", code);
			var solution = ReachingTypeAnalysis.Utils.CreateSolution(code);
			Logger.Instance.Log("CallGraphGenerator", "GenerateSimpleSolution", "solution filename: {0}", solution.FilePath);
        }

        [TestMethod]
        [TestCategory("Generation")]
        public void AnalyzeGenerationSolutionOnDemandAsync1()
        {
            AnalyzeGenerationSolution1(AnalysisStrategy.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Generation")]
        public void AnalyzeGenerationSolutionOnDemandSync1()
        {
            AnalyzeGenerationSolution1(AnalysisStrategy.ONDEMAND_SYNC);
        }

        public void AnalyzeGenerationSolution(AnalysisStrategy strategy, int size)
        {
            var callgraph = GenerateCallGraph(size);
            var syntax = GenerateCode(callgraph);
            var code = syntax.ToFullString();
            Logger.Instance.Log("CallGraphGenerator", "AnalyzeGenerationSolution1", "source code: {0}", code);
            //var solution = ReachingTypeAnalysis.Utils.CreateSolution(code);
            //Logger.Instance.Log("CallGraphGenerator", "AnalyzeGenerationSolution1", "solution filename: {0}", solution.FilePath);
            var solAnalyzer = new SolutionAnalyzer(code);
            var resolved = solAnalyzer.Analyze(strategy);
            //var resolved = solAnalyzer.GenerateCallGraph();
            var resolvedNodes = resolved.GetNodes().Count();
            var callgraphNodes = callgraph.GetNodes().Count();
            Assert.IsTrue(resolvedNodes == callgraphNodes);
            var resolveEdgeCount = resolved.GetEdges().Count();
            var callgraphEdgeCount = callgraph.GetEdges().Count();

            foreach (var node in resolved.GetNodes())
            {
                //var callees = callgraph.GetCallees(node.Name);
                var callees = callgraph.GetCallees(node.MethodName);
                var resolvedCallees = resolved.GetCallees(node);
                Assert.IsTrue(callees.Count == resolvedCallees.Count, "Mismatched callee counts for " + node);
            }
            Assert.IsTrue(resolved.GetEdges().Count() == callgraph.GetEdges().Count());
        }

        public void AnalyzeGenerationSolution1(AnalysisStrategy strategy)
        {
            AnalyzeGenerationSolution(strategy, 10);
        }

        [TestMethod]
        [TestCategory("Generation")]
        public void AnalyzeGenerationSolutionOnDemandAsync2() {
            AnalyzeGenerationSolution2(AnalysisStrategy.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Generation")]
        public void AnalyzeGenerationSolutionOnDemandSync2()
        {
            AnalyzeGenerationSolution2(AnalysisStrategy.ONDEMAND_SYNC);
        }

        public void AnalyzeGenerationSolution2(AnalysisStrategy strategy)
        {
            AnalyzeGenerationSolution(strategy, 50);
        }

        [TestMethod]
        [TestCategory("Generation")]
        public void AnalyzeGenerationSolutionOnDemandAsync3()
        {
            AnalyzeGenerationSolution3(AnalysisStrategy.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Generation")]
        public void AnalyzeGenerationSolutionOnDemandSync3()
        {
            AnalyzeGenerationSolution3(AnalysisStrategy.ONDEMAND_SYNC);
        }

        public void AnalyzeGenerationSolution3(AnalysisStrategy strategy)
        {
            AnalyzeGenerationSolution(strategy, 100);
        }

        [TestMethod]
        [TestCategory("Generation")]
        public void AnalyzeGenerationSolutionOnDemandAsync4()
        {
            AnalyzeGenerationSolution4(AnalysisStrategy.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Generation")]
        public void AnalyzeGenerationSolutionOnDemandSync4()
        {
            AnalyzeGenerationSolution4(AnalysisStrategy.ONDEMAND_SYNC);
        }

        public void AnalyzeGenerationSolution4(AnalysisStrategy strategy)
        {
            AnalyzeGenerationSolution(strategy, 1000);
        }

        /*
        [TestMethod]
        [TestCategory("Generation")]
        public void AnalyzeGenerationSolution5()
        {
            var callgraph = GenerateCallGraph(10000);
            var syntax = GenerateCode(callgraph);
            var code = syntax.ToFullString();
            //Logger.Instance.Log(code);
            var solution = ReachingTypeAnalysis.Roslyn.Utils.CreateSolution(code);
            Logger.Instance.Log(solution.FilePath);
            var solutionAnalyzer = new SolutionAnalyzer(solution);
            solutionAnalyzer.Analyze();
            var resolved = solutionAnalyzer.Callgraph;
            var resolvedNodes = resolved.GetNodes().Count();
            var callgraphNodes = callgraph.GetNodes().Count();
            Assert.IsTrue(resolvedNodes == callgraphNodes);
            var resolveEdgeCount = resolved.GetEdges().Count();
            var callgraphEdgeCount = callgraph.GetEdges().Count();

            foreach (var node in resolved.GetNodes())
            {
                var callees = callgraph.GetCallees(node.Name);
                var resolvedCallees = resolved.GetCallees(node);
                Assert.IsTrue(callees.Count == resolvedCallees.Count, "Mismatched callee counts for " + node);
            }

            Assert.IsTrue(resolved.GetEdges().Count() == callgraph.GetEdges().Count());
        }*/
        [TestMethod]
        [TestCategory("Generation")]
        [TestCategory("OnDemandOrleans")]
        public void AnalyzeGenerationOnDemandOrleans1()
        {
            AnalyzeGenerationSolution(AnalysisStrategy.ONDEMAND_SYNC, 10);
        }
        [TestMethod]
        [TestCategory("Generation")]
        [TestCategory("OnDemandOrleans")]
        public void AnalyzeGenerationOnDemandOrleans2()
        {
            AnalyzeGenerationSolution(AnalysisStrategy.ONDEMAND_SYNC, 50);
        }
        [TestMethod]
        [TestCategory("Generation")]
        [TestCategory("OnDemandOrleans")]
        public void AnalyzeGenerationOnDemandOrleans3()
        {
            AnalyzeGenerationSolution(AnalysisStrategy.ONDEMAND_SYNC, 100);
        }
        [TestMethod]
        [TestCategory("Generation")]
        [TestCategory("OnDemandOrleans")]
        public void AnalyzeGenerationOnDemandOrleans4()
        {
            AnalyzeGenerationSolution(AnalysisStrategy.ONDEMAND_SYNC, 1000);
        }
    }

}
