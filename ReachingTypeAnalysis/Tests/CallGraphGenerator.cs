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
        public static CallGraph<string, int> GenerateCallGraph(int n)
        {
            var result = new CallGraph<string, int>();
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

        public static SyntaxNode GenerateCode(CallGraph<string,int> callgraph)
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

        /// <summary>
        /// The idea here is to produce queries agains the call graph. The plan is to make these
        /// queries uniformly distributed against the methods of the call graph. 
        /// </summary>
        /// <param name="callgraph"></param>
        /// <param name="queryCount"></param>
        /// <returns></returns>
        public static CompilationUnitSyntax GenerateQueries(CallGraph<string, int> callgraph, int queryCount)
        {
            List<StatementSyntax> calls = new List<StatementSyntax>();
            var r = new Random();
            foreach (var vertex in callgraph.GetNodes())
            {
                var callees = callgraph.GetCallees(vertex);
                // query: GetCallees(random(1..callees.Count))
                // project: "MyProject"
                var ordinal = r.Next(callees.Count) + 1;
                //CallGraphQueryInterface.GetCalleesAsync(
                //    new MethodEntityDescriptor("C", vertex), ordinal, "MyProject");
                var invocation = GetQueryCall(vertex, ordinal);
                calls.Add(invocation);
            }
            
            return
                SyntaxFactory.CompilationUnit()
                .WithUsings(
                    SyntaxFactory.SingletonList<UsingDirectiveSyntax>(
                        SyntaxFactory.UsingDirective(
                            SyntaxFactory.QualifiedName(
                                SyntaxFactory.IdentifierName(
                                    @"System"),
                                SyntaxFactory.IdentifierName(
                                    @"Diagnostics"))
                            .WithDotToken(
                                SyntaxFactory.Token(
                                    SyntaxKind.DotToken)))
                        .WithUsingKeyword(
                            SyntaxFactory.Token(
                                SyntaxKind.UsingKeyword))
                        .WithSemicolonToken(
                            SyntaxFactory.Token(
                                SyntaxKind.SemicolonToken))))
                .WithMembers(
                    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                        SyntaxFactory.ClassDeclaration(
                            SyntaxFactory.Identifier(
                                SyntaxFactory.TriviaList(),
                                @"Q",
                                SyntaxFactory.TriviaList(
                                    SyntaxFactory.Space)))
                        .WithModifiers(
                            SyntaxFactory.TokenList(
                                SyntaxFactory.Token(
                                    SyntaxFactory.TriviaList(),
                                    SyntaxKind.PublicKeyword,
                                    SyntaxFactory.TriviaList(
                                        SyntaxFactory.Space)),
                                SyntaxFactory.Token(
                                    SyntaxFactory.TriviaList(),
                                    SyntaxKind.StaticKeyword,
                                    SyntaxFactory.TriviaList(
                                        SyntaxFactory.Space))))
                        .WithKeyword(
                            SyntaxFactory.Token(
                                SyntaxFactory.TriviaList(),
                                SyntaxKind.ClassKeyword,
                                SyntaxFactory.TriviaList(
                                    SyntaxFactory.Space)))
                        .WithOpenBraceToken(
                            SyntaxFactory.Token(
                                SyntaxFactory.TriviaList(),
                                SyntaxKind.OpenBraceToken,
                                SyntaxFactory.TriviaList(
                                    SyntaxFactory.LineFeed)))
                        .WithMembers(
                            SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                SyntaxFactory.MethodDeclaration(
                                    SyntaxFactory.GenericName(
                                        SyntaxFactory.Identifier(
                                            @"IEnumerable"))
                                    .WithTypeArgumentList(
                                        SyntaxFactory.TypeArgumentList(
                                            SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                                SyntaxFactory.IdentifierName(
                                                    @"StopWatch")))
                                        .WithLessThanToken(
                                            SyntaxFactory.Token(
                                                SyntaxKind.LessThanToken))
                                        .WithGreaterThanToken(
                                            SyntaxFactory.Token(
                                                SyntaxKind.GreaterThanToken))),
                                    SyntaxFactory.Identifier(
                                        @"DoQueries"))
                                .WithModifiers(
                                    SyntaxFactory.TokenList(
                                        new[]{
                                            SyntaxFactory.Token(
                                                SyntaxFactory.TriviaList(
                                                    SyntaxFactory.Whitespace(
                                                        @"    ")),
                                                SyntaxKind.PublicKeyword,
                                                SyntaxFactory.TriviaList(
                                                    SyntaxFactory.Space)),
                                            SyntaxFactory.Token(
                                                SyntaxFactory.TriviaList(),
                                                SyntaxKind.StaticKeyword,
                                                SyntaxFactory.TriviaList(
                                                    SyntaxFactory.Space))}))
                                .WithParameterList(
                                    SyntaxFactory.ParameterList()
                                    .WithOpenParenToken(
                                        SyntaxFactory.Token(
                                            SyntaxKind.OpenParenToken))
                                    .WithCloseParenToken(
                                        SyntaxFactory.Token(
                                            SyntaxKind.CloseParenToken)))
                                .WithBody(
                                    SyntaxFactory.Block(
                                        SyntaxFactory.List<StatementSyntax>(calls)))))))
                .NormalizeWhitespace();
        }

        private static StatementSyntax GetQueryCall(string methodName, int ordinal)
        {
            return
                SyntaxFactory.Block(
                    SyntaxFactory.List<StatementSyntax>(
                        new StatementSyntax[] 
                        {
                             SyntaxFactory.LocalDeclarationStatement(
                                            SyntaxFactory.VariableDeclaration(
                                                SyntaxFactory.IdentifierName(
                                                    @"var"))
                                            .WithVariables(
                                                SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                                                    SyntaxFactory.VariableDeclarator(
                                                        SyntaxFactory.Identifier(
                                                            @"sw"))
                                                    .WithInitializer(
                                                        SyntaxFactory.EqualsValueClause(
                                                            SyntaxFactory.InvocationExpression(
                                                                SyntaxFactory.MemberAccessExpression(
                                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                                    SyntaxFactory.IdentifierName(
                                                                        @"Stopwatch"),
                                                                    SyntaxFactory.IdentifierName(
                                                                        @"StartNew"))
                                                                .WithOperatorToken(
                                                                    SyntaxFactory.Token(
                                                                        SyntaxKind.DotToken)))
                                                            .WithArgumentList(
                                                                SyntaxFactory.ArgumentList()
                                                                .WithOpenParenToken(
                                                                    SyntaxFactory.Token(
                                                                        SyntaxKind.OpenParenToken))
                                                                .WithCloseParenToken(
                                                                    SyntaxFactory.Token(
                                                                        SyntaxKind.CloseParenToken))))
                                                        .WithEqualsToken(
                                                            SyntaxFactory.Token(
                                                                SyntaxKind.EqualsToken))))))
                                        .WithSemicolonToken(
                                            SyntaxFactory.Token(
                                                SyntaxKind.SemicolonToken)),
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName(
                                            SyntaxFactory.Identifier(
                                                SyntaxFactory.TriviaList(
                                                    SyntaxFactory.Whitespace(
                                                        @"    ")),
                                                @"CallGraphQueryInterface",
                                                SyntaxFactory.TriviaList())),
                                        SyntaxFactory.IdentifierName(
                                            @"GetCalleesAsync"))
                                    .WithOperatorToken(
                                        SyntaxFactory.Token(
                                            SyntaxKind.DotToken)))
                                .WithArgumentList(
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[]{
                                                SyntaxFactory.Argument(
                                                    SyntaxFactory.ObjectCreationExpression(
                                                        SyntaxFactory.IdentifierName(
                                                            @"MethodDescriptor"))
                                                    .WithNewKeyword(
                                                        SyntaxFactory.Token(
                                                            SyntaxFactory.TriviaList(
                                                                SyntaxFactory.Whitespace(
                                                                    @"        ")),
                                                            SyntaxKind.NewKeyword,
                                                            SyntaxFactory.TriviaList(
                                                                SyntaxFactory.Space)))
                                                    .WithArgumentList(
                                                        SyntaxFactory.ArgumentList(
                                                            SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                                new SyntaxNodeOrToken[]{
                                                                    SyntaxFactory.Argument(
                                                                        SyntaxFactory.LiteralExpression(
                                                                            SyntaxKind.StringLiteralExpression,
                                                                            SyntaxFactory.Literal(
                                                                                SyntaxFactory.TriviaList(),
                                                                                @"""C""",
                                                                                @"""C""",
                                                                                SyntaxFactory.TriviaList()))),
                                                                    SyntaxFactory.Token(
                                                                        SyntaxFactory.TriviaList(),
                                                                        SyntaxKind.CommaToken,
                                                                        SyntaxFactory.TriviaList(
                                                                            SyntaxFactory.Space)),
                                                                    SyntaxFactory.Argument(
                                                                        SyntaxFactory.LiteralExpression(
                                                                            SyntaxKind.StringLiteralExpression,
                                                                            SyntaxFactory.Literal(
                                                                                SyntaxFactory.TriviaList(),
                                                                                "\"" + methodName + "\"",
                                                                                "\"" + methodName +"\"" ,
                                                                                SyntaxFactory.TriviaList()))
                                                                         )}))
                                                        .WithOpenParenToken(
                                                            SyntaxFactory.Token(
                                                                SyntaxKind.OpenParenToken))
                                                        .WithCloseParenToken(
                                                            SyntaxFactory.Token(
                                                                SyntaxKind.CloseParenToken)))),
                                                SyntaxFactory.Token(
                                                    SyntaxFactory.TriviaList(),
                                                    SyntaxKind.CommaToken,
                                                    SyntaxFactory.TriviaList(
                                                        SyntaxFactory.Space)),
                                                SyntaxFactory.Argument(
                                                    SyntaxFactory.LiteralExpression(
                                                                            SyntaxKind.NumericLiteralExpression,
                                                                            SyntaxFactory.Literal(
                                                                                SyntaxFactory.TriviaList(),
                                                                                ordinal.ToString(),
                                                                                ordinal,
                                                                                SyntaxFactory.TriviaList()))),
                                                SyntaxFactory.Token(
                                                    SyntaxFactory.TriviaList(),
                                                    SyntaxKind.CommaToken,
                                                    SyntaxFactory.TriviaList(
                                                        SyntaxFactory.Space)),
                                                SyntaxFactory.Argument(
                                                    SyntaxFactory.LiteralExpression(
                                                        SyntaxKind.StringLiteralExpression,
                                                        SyntaxFactory.Literal(
                                                            SyntaxFactory.TriviaList(),
                                                            @"""MyProject""",
                                                            @"""MyProject""",
                                                            SyntaxFactory.TriviaList())))}))
                                    .WithOpenParenToken(
                                        SyntaxFactory.Token(
                                            SyntaxFactory.TriviaList(),
                                            SyntaxKind.OpenParenToken,
                                            SyntaxFactory.TriviaList(
                                                SyntaxFactory.LineFeed)))
                                    .WithCloseParenToken(
                                        SyntaxFactory.Token(
                                            SyntaxKind.CloseParenToken))),
                                    SyntaxFactory.IdentifierName(
                                        @"Result"))
                                    .WithOperatorToken(
                                        SyntaxFactory.Token(
                                            SyntaxKind.DotToken)))
                                    .WithSemicolonToken(
                                            SyntaxFactory.Token(
                                                SyntaxKind.SemicolonToken)),
                                        SyntaxFactory.ExpressionStatement(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName(
                                                        @"sw"),
                                                    SyntaxFactory.IdentifierName(
                                                        @"Stop"))
                                                .WithOperatorToken(
                                                    SyntaxFactory.Token(
                                                        SyntaxKind.DotToken)))
                                            .WithArgumentList(
                                                SyntaxFactory.ArgumentList()
                                                .WithOpenParenToken(
                                                    SyntaxFactory.Token(
                                                        SyntaxKind.OpenParenToken))
                                                .WithCloseParenToken(
                                                    SyntaxFactory.Token(
                                                        SyntaxKind.CloseParenToken))))
                                        .WithSemicolonToken(
                                            SyntaxFactory.Token(
                                                SyntaxKind.SemicolonToken)),
                                        SyntaxFactory.YieldStatement(
                                            SyntaxKind.YieldReturnStatement,
                                            SyntaxFactory.IdentifierName(
                                                @"sw"))
                                        .WithYieldKeyword(
                                            SyntaxFactory.Token(
                                                SyntaxKind.YieldKeyword))
                                        .WithReturnOrBreakKeyword(
                                            SyntaxFactory.Token(
                                                SyntaxKind.ReturnKeyword))
                                        .WithSemicolonToken(
                                            SyntaxFactory.Token(
                                                SyntaxKind.SemicolonToken))
                        }));
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
            var queries = GenerateQueries(callgraph, 5);
            var code = syntax.ToFullString();
            var queryCode = queries.ToFullString();
            Logger.Instance.Log("CallGraphGenerator", "GenerateSimpleSolution", "source code: {0}", code);
            Logger.Instance.Log("CallGraphGenerator", "GenerateSimpleSolution", "query code: {0}", queryCode);
            var solution = ReachingTypeAnalysis.Utils.CreateSolution(code);
			Logger.Instance.Log("CallGraphGenerator", "GenerateSimpleSolution", "solution filename: {0}", solution.FilePath);
        }

        [TestMethod]
        [TestCategory("Generation")]
        public static void AnalyzeGenerationSolutionOnDemandAsync1()
        {
            AnalyzeGenerationSolution1(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Generation")]
        public static void AnalyzeGenerationSolutionOnDemandSync1()
        {
            AnalyzeGenerationSolution1(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        public static void AnalyzeGenerationSolution(AnalysisStrategyKind strategy, int size)
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

        public static void AnalyzeGenerationSolution1(AnalysisStrategyKind strategy)
        {
            AnalyzeGenerationSolution(strategy, 10);
        }

        [TestMethod]
        [TestCategory("Generation")]
        public static void AnalyzeGenerationSolutionOnDemandAsync2() {
            AnalyzeGenerationSolution2(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Generation")]
        public static void AnalyzeGenerationSolutionOnDemandSync2()
        {
            AnalyzeGenerationSolution2(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        public static void AnalyzeGenerationSolution2(AnalysisStrategyKind strategy)
        {
            AnalyzeGenerationSolution(strategy, 50);
        }

        [TestMethod]
        [TestCategory("Generation")]
        public static void AnalyzeGenerationSolutionOnDemandAsync3()
        {
            AnalyzeGenerationSolution3(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Generation")]
        public static void AnalyzeGenerationSolutionOnDemandSync3()
        {
            AnalyzeGenerationSolution3(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        public static void AnalyzeGenerationSolution3(AnalysisStrategyKind strategy)
        {
            AnalyzeGenerationSolution(strategy, 100);
        }

        [TestMethod]
        [TestCategory("Generation")]
        public static void AnalyzeGenerationSolutionOnDemandAsync4()
        {
            AnalyzeGenerationSolution4(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Generation")]
        public static void AnalyzeGenerationSolutionOnDemandSync4()
        {
            AnalyzeGenerationSolution4(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        public static void AnalyzeGenerationSolution4(AnalysisStrategyKind strategy)
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
        //[TestCategory("OnDemandOrleans")]
        public static void AnalyzeGenerationOnDemandOrleans1()
        {
            AnalyzeGenerationSolution(AnalysisStrategyKind.ONDEMAND_SYNC, 10);
        }
        [TestMethod]
        [TestCategory("Generation")]
        //[TestCategory("OnDemandOrleans")]
        public static void AnalyzeGenerationOnDemandOrleans2()
        {
            AnalyzeGenerationSolution(AnalysisStrategyKind.ONDEMAND_SYNC, 50);
        }
        [TestMethod]
        [TestCategory("Generation")]
        //[TestCategory("OnDemandOrleans")]
        public static void AnalyzeGenerationOnDemandOrleans3()
        {
            AnalyzeGenerationSolution(AnalysisStrategyKind.ONDEMAND_SYNC, 100);
        }
        [TestMethod]
        [TestCategory("Generation")]
        //[TestCategory("OnDemandOrleans")]
        public static void AnalyzeGenerationOnDemandOrleans4()
        {
            AnalyzeGenerationSolution(AnalysisStrategyKind.ONDEMAND_SYNC, 1000);
        }
        [TestMethod]
        [TestCategory("Generation")]
        //[TestCategory("OnDemandOrleans")]
        public static void AnalyzeGenerationOnDemandOrleans5()
        {
            AnalyzeGenerationSolution(AnalysisStrategyKind.ONDEMAND_SYNC, 10000);
        }
    }

}
