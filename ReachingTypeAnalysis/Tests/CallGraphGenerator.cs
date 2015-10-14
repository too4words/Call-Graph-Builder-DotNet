// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SolutionTraversal.CallGraph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Diagnostics.Contracts;
using System.IO;
using Microsoft.CodeAnalysis.MSBuild;
using System.Xml;
using System.Text;

namespace ReachingTypeAnalysis.Tests
{
    /// <summary>
    /// This will produce randomy generated callgraphs of a given size. 
    /// This class is primarily to be used for testing purposes.
    /// </summary>
    [TestClass]
    public class CallGraphGenerator
    {
        public static CallGraph<string, int> GenerateCallGraph(int n, bool addCallsFromMain = true, int multiplier = 2)
        {
			Trace.TraceInformation("Adding Nodes");
            var result = new CallGraph<string, int>();
            for (var i = 0; i < n; i++)
            {
                result.Add(string.Format("N{0}", i));
            }


			Trace.TraceInformation("Adding edges");
			// now generate the edges
            var rand = new Random();
            // multiplier determines how dense the graph is
            for (var i = 0; i < multiplier * n; i++)
            {
                var source = rand.Next(n - 1);
                var dest = rand.Next(n - 1);

				if(i % 500 == 0)
				{
					Trace.TraceInformation("Adding edge {0}", i);
				}

                result.AddCall(string.Format("N{0}", source), string.Format("N{0}", dest));
            }
            result.Add("Main");
            result.AddRootMethod("Main");

            var modulo = Math.Ceiling((decimal)n / 100);
            if (addCallsFromMain)
            {
                Trace.TraceInformation("Adding calls from main for every {0} method", modulo);
                var index = 0;
                foreach (var method in result.GetNodes())
                {
                    if (index % modulo == 0)
                    {
                        // avoid self-recursive calls
                        if (method != "Main")
                        {
                            result.AddCall("Main", method);
                        }
                    }
                    index++;
                }
            }

            result.Compress();

            return result;
        }

        public static CallGraph<string, int> GenerateConnectedCallGraph(int n)
        {
			Trace.TraceInformation("Adding Nodes");
            var result = new CallGraph<string, int>();
            for (var i = 0; i < n; i++)
            {
                result.Add(string.Format("N{0}", i));
            }

            result.Add("Main");
            result.AddRootMethod("Main");

            var current = "Main";
			Trace.TraceInformation("Adding edges");
			// now generate the edges
            var rand = new Random();
            var used = new HashSet<string>();
            used.Add("Main");
            // multiplier determines how dense the graph is
            //for (var i = 0; i < multiplier * n; i++)
            while (used.Count() < n)
            {
                var dest = string.Empty;
                do
                {
                    dest = string.Format("N{0}", rand.Next(n - 1));
                } while (used.Contains(dest));
                Contract.Assert(dest.Length > 0);
                used.Add(dest);
                // preserve connectivity
                result.AddCall(current, dest);

                for (var i = 0; i < 10; i++)
                {
                    var source = rand.Next(n - 1);

                    if (i % 500 == 0)
                    {
                        Trace.TraceInformation("Adding edge {0} -> {1}", string.Format("N{0}", source), dest);
                    }

                    Contract.Assert(dest != null);
                    result.AddCall(string.Format("N{0}", source), dest);
                }
                current = dest;
            } 

            result.Compress();

            return result;
        }


        public static Solution GenerateSolution(int nodes, int maxCalls)
        {
            string solutionPath = @"C:\Users\...\PathToSolution\MySolution.sln";
            var msWorkspace = MSBuildWorkspace.Create();
            var solution = msWorkspace.OpenSolutionAsync(solutionPath).Result;



            //IWorkspace workspace = Workspace.LoadSolution("MySolution.sln");
            //var originalSolution = workspace.CurrentSolution;
            //var project = originalSolution.GetProject(originalSolution.ProjectIds.First());
            //IDocument doc = project.AddDocument("index.html", "<html></html>");
            //workspace.ApplyChanges(originalSolution, doc.Project.Solution);
            throw new NotImplementedException();
        }

		public static SyntaxNode GenerateCode(int nodes, int maxCalls)
		{
			Trace.TraceInformation("Adding Methods");
			var methods = new List<MethodDeclarationSyntax>();
			var mainCallees = new List<string>();
			
            for (var i = 0; i < nodes; i++)
            {
				if (i % 500 == 0)
				{
					Trace.TraceInformation("Adding method {0}", i);
				}
				var method = string.Format("N{0}", i);
				methods.Add(GetMethod(method, GenerateCalles(nodes,maxCalls)));
				mainCallees.Add(method);
            }
		
			methods.Add(GetMethod("Main", mainCallees));

			Trace.TraceInformation("Generating AST");
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

		private static IEnumerable<string> GenerateCalles(int nodes, int calleeRange)
		{
			var rand = new Random();

			var calles = new List<string>();

            var numberOfCallees = rand.Next(1, calleeRange);

			for (var i = 0; i < numberOfCallees; i++)
			{
				var dest = rand.Next(nodes - 1);

				calles.Add(string.Format("N{0}", dest));
			}
			return calles;
		}

        public static CompilationUnitSyntax GenerateCode(CallGraph<string, int> callgraph)
        {
			Trace.TraceInformation("Adding Methods");
            var methods = new List<MethodDeclarationSyntax>();
			int i = 0;
            foreach (var vertex in callgraph.GetNodes())
            {
				i++;
				if (i % 500 == 0)
				{
					Trace.TraceInformation("Adding method {0}", i);
				}
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
        /// The resulting structure has one file per project and one class per file.
        /// </summary>
        /// <param name="callgraph"></param>
        /// <param name="projectCount"></param>
        /// <returns></returns>
        public static IEnumerable<CompilationUnitSyntax> GenerateCodeWithDifferentProjects(CallGraph<string, int> callgraph, int projectCount)
        {
			Trace.TraceInformation("Adding Methods");
			int i = 0;
            var calleeCache = new Dictionary<string, IEnumerable<string>>();
            var fileList = new List<MethodDeclarationSyntax>[projectCount];
            for (var index = 0; index < projectCount; index++)
            {
                fileList[index] = new List<MethodDeclarationSyntax>();
            }
            foreach (var vertex in callgraph.GetNodes())
            {
				i++;
				if (i % 500 == 0)
				{
					Trace.TraceInformation("Adding method {0}", i);
				}
                var hash = vertex.ToString().GetHashCode();
                var fileForThisMethod = (vertex != "Main") ?
                        (Math.Abs(hash) % projectCount) :
                        projectCount - 1;
                IEnumerable<string> callees = null;
                if (!calleeCache.TryGetValue(vertex, out callees))
                {
                    callees = callgraph.GetCallees(vertex).ToList();
                    calleeCache.Add(vertex, callees);
                }
                else {
                    // cache hit
                    Trace.TraceInformation("Cache hit for {0}", vertex); 
                }
                var filteredCallees =
                    callees
                    // only call things in classes numbered lower than ours to avoid circular depdendencies
                    .Where(m => Math.Abs(m.GetHashCode()) % projectCount <= fileForThisMethod)
                    .Select(
                        m =>
                        string.Format("C{0}.{1}", Math.Abs(m.GetHashCode()) % projectCount, m));
                var method = GetMethod(vertex, filteredCallees);
                
                
                Contract.Assert(fileForThisMethod >= 0);
                Contract.Assert(fileForThisMethod < projectCount);

                fileList[fileForThisMethod].Add(method);
            }
            //methods.Add(GetMain(callgraph.GetNodes()));

            for (var index = 0; index < fileList.Count(); index++)
            {
                yield return
                    SyntaxFactory.CompilationUnit()                    
                    .WithMembers(
                        SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                             SyntaxFactory.NamespaceDeclaration(
                                SyntaxFactory.IdentifierName(
                                    TestConstants.TemporaryNamespace))
                            .WithNamespaceKeyword(
                                SyntaxFactory.Token(
                                    SyntaxKind.NamespaceKeyword))
                            .WithOpenBraceToken(
                                SyntaxFactory.Token(
                                    SyntaxKind.OpenBraceToken))
                            .WithMembers(
                                SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                    SyntaxFactory.ClassDeclaration(
                                        string.Format("C{0}", index))
                                    .WithModifiers(
                                        SyntaxFactory.TokenList(
                                            SyntaxFactory.Token(
                                                SyntaxKind.PublicKeyword)))
                                    .WithMembers(
                                        SyntaxFactory.List<MemberDeclarationSyntax>(fileList[index])
                                    )))))
                    .NormalizeWhitespace();
            }
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
            var nodes = callgraph.GetNodes();
            var nodeCount = nodes.Count();
            // we need to sample from the nodes to get queryCount as a result
            var prob = (1.0*queryCount)/(1.0*nodeCount);
            if (prob > 1.0) { prob = 1.0; }

            Debug.WriteLine("Sampling with probability {0}", prob);

            foreach (var vertex in nodes)
            {
                var callees = callgraph.GetCallees(vertex);
                // query: GetCallees(random(1..callees.Count))
                // project: "MyProject"
                var ordinal = r.Next(callees.Count()) + 1;
                if (r.NextDouble() < prob)
                {
                    var invocation = GetQueryCall(vertex, ordinal);
                    calls.Add(invocation);
                }
            }
            if (calls.Count > queryCount)
            {
                // truncate
                calls.RemoveRange(queryCount, calls.Count - queryCount);
                Contract.Assert(calls.Count == queryCount);
            }

            return
                SyntaxFactory.CompilationUnit()
                .WithUsings(
                    SyntaxFactory.List<UsingDirectiveSyntax>(
                        new[] 
                        {
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
                                    SyntaxKind.SemicolonToken)),
                            SyntaxFactory.UsingDirective(
                                    SyntaxFactory.IdentifierName(
                                        @"ReachingTypeAnalysis"))
                            .WithUsingKeyword(
                                SyntaxFactory.Token(
                                    SyntaxKind.UsingKeyword))
                            .WithSemicolonToken(
                                SyntaxFactory.Token(
                                    SyntaxKind.SemicolonToken))
                        }
                ))
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
                                                    @"Stopwatch")))
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
                                    SyntaxFactory.ParameterList(
                                        SyntaxFactory.SingletonSeparatedList<ParameterSyntax>(
                                            SyntaxFactory.Parameter(
                                                SyntaxFactory.Identifier(
                                                    @"analysisStrategy"))
                                            .WithType(
                                                SyntaxFactory.IdentifierName(
                                                    @"IAnalysisStrategy"))))
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
            var s1 = // initialize the stopwatch 
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
                            SyntaxKind.SemicolonToken));

            var s2 = // call the query interface and discard the result 
                    SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName(
                            @"var"))
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>
                        (
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(
                                    @"_"))
                            .WithInitializer(
                                SyntaxFactory.EqualsValueClause(
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
                            new SyntaxNodeOrToken[]
                            {
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName(@"analysisStrategy")
                                    ),
                                SyntaxFactory.Token(
                                    SyntaxFactory.TriviaList(),
                                    SyntaxKind.CommaToken,
                                    SyntaxFactory.TriviaList(
                                        SyntaxFactory.Space)),
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
                                                            ), 
                                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                                    SyntaxFactory.Argument(
                                                                        SyntaxFactory.LiteralExpression(
                                                                            SyntaxKind.TrueLiteralExpression)
                                                                        .WithToken(
                                                                            SyntaxFactory.Token(
                                                                                SyntaxKind.TrueKeyword)))
                                                } ) )
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
                                            SyntaxFactory.TriviaList())))
                            }   // arguments
                        ))
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
                ))))
                .WithSemicolonToken(
                        SyntaxFactory.Token(
                            SyntaxKind.SemicolonToken));

            var s3 = // stop the stopwatch
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
                        SyntaxKind.SemicolonToken));

            var s4 = // yield return the stopwatch
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
                        SyntaxKind.SemicolonToken));

            return
                SyntaxFactory.Block(
                    SyntaxFactory.List<StatementSyntax>(
                        new StatementSyntax[]
                        {
                            s1,
                            s2,
                            s3,
                            s4
                        }
                    ));
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

        public void GenerateSimpleCallGraphWithMultipleFiles()
        {
            try
            {
                if (Directory.Exists(TestConstants.TestDirectory))
                {
                    Directory.Delete(TestConstants.TestDirectory, true);
                }
                Directory.CreateDirectory(TestConstants.TestDirectory);

                var writingTo = Path.Combine(Directory.GetCurrentDirectory(), TestConstants.TestDirectory);
                Trace.TraceInformation("Writing to {0}", writingTo);

                var callgraph = GenerateCallGraph(10);
                var syntaxes = GenerateCodeWithDifferentProjects(callgraph, 3);
                int index = 0;
                foreach (var syntax in syntaxes)
                {
                    var code = syntax.ToFullString();

                    var fileName = string.Format("test\\file{0}.cs", index);
                    Trace.TraceInformation(fileName);
                    Trace.TraceInformation(code);
                    File.WriteAllText(fileName, code);
                    index++;
                }
                //Console.WriteLine(code);
            }
            finally
            {
                //Directory.Delete(TestConstants.TestDirectory, true);
            }
        }

        [TestMethod]
        [TestCategory("Generation")]
        
        public void GenerateSimpleCallGraphWithMultipleProjects()
        {
            try
            {
                if (Directory.Exists(TestConstants.TestDirectory))
                {
                    Directory.Delete(TestConstants.TestDirectory, true);
                }
                Directory.CreateDirectory(TestConstants.TestDirectory);

                var writingTo = Path.Combine(Directory.GetCurrentDirectory(), TestConstants.TestDirectory);
                Trace.TraceInformation("Writing to {0}", writingTo);

                var callgraph = GenerateCallGraph(100);
                var numProjects = 3;
                var syntaxes = GenerateCodeWithDifferentProjects(callgraph, numProjects);
                int index = 0;
                var descriptors = new List<ProjectDescriptor>();
                var projectNames = new List<ProjectDescriptor>();
                for (index = 0; index < numProjects; index++)
                {
                    projectNames.Add(new ProjectDescriptor {
                        Name = string.Format("P{0}", index),
                        AbsolutePath = string.Format("P{0}.csproj", index),
                        Type = (index == numProjects - 1) ? ProjectType.Executable : ProjectType.Library,
                    });
                }
                foreach (var syntax in syntaxes)
                {
                    var code = syntax.ToFullString();

                    var fileName = string.Format("file{0}.cs", index);
                    Trace.TraceInformation(fileName);
                    Trace.TraceInformation(code);
                    File.WriteAllText(fileName, code);
                    descriptors.Add(new ProjectDescriptor
                    {
                        AbsolutePath = Path.Combine(writingTo, string.Format("P{0}.csproj", index)),
                        Name = string.Format("P{0}", index),
                        ProjectGuid = Guid.NewGuid().ToString(),
                        Files = new[] { fileName },
                        Type = (index == numProjects - 1) ? ProjectType.Executable : ProjectType.Library,
                        // TODO: we may need to worry about project 
                        // dependencies because it will likely fail to compile without them
                        Dependencies = projectNames,
                    });
                    index++;
                }
                var solution = SolutionFileGenerator.GenerateSolutionWithProjects(TestConstants.SolutionPath, descriptors);
                Assert.IsTrue(solution != null);
                //Console.WriteLine(code);
            }
            finally
            {
                //Directory.Delete(TestConstants.TestDirectory, true);
            }
        }


		[TestMethod]
		[TestCategory("Generation")]
		public void GenerateLong5Test()
		{
			Trace.TraceInformation("Generating code");
			var syntax = GenerateCode(50000, 10);
			// var code = syntax.ToFullString();
			Trace.TraceInformation("Saving code to file");
			using (var writer = File.CreateText("LongTest5.cs"))
			{
				syntax.WriteTo(writer);
			}
		}

		[TestMethod]
		[TestCategory("Generation")]
		public void GenerateLong6Test()
		{
			Trace.TraceInformation("Generating code");
			var syntax = GenerateCode(100000, 10);
			// var code = syntax.ToFullString();
			Trace.TraceInformation("Saving code to file");
			using (var writer = File.CreateText("LongTest6.cs"))
			{
				syntax.WriteTo(writer);
			}
		}

        [TestMethod]
        [TestCategory("Generation")]
        public void TestSimpleSolutionGeneration()
        {
            var callgraph = GenerateCallGraph(10);
            var syntax = GenerateCode(callgraph);
            var queries = GenerateQueries(callgraph, 5);
            var code = syntax.ToFullString();
            var queryCode = queries.ToFullString();
            Logger.Instance.Log("CallGraphGenerator", "GenerateSimpleSolution", "source code: {0}", code);
            Logger.Instance.Log("CallGraphGenerator", "GenerateSimpleSolution", "query code: {0}", queryCode);
            var solution = SolutionFileGenerator.CreateSolution(code);
			Logger.Instance.Log("CallGraphGenerator", "GenerateSimpleSolution", "solution filename: {0}", solution.FilePath);
        }


        [TestMethod]
        [TestCategory("Generation")]
        public void TestMoreComplexSolutionGeneration()
        {
            if (Directory.Exists(TestConstants.TestDirectory))
            {
                Directory.Delete(TestConstants.TestDirectory, true);
            }
            Directory.CreateDirectory(TestConstants.TestDirectory);

            var text = SolutionFileGenerator.GenerateSolutionText(new[]
            {
                new ProjectDescriptor
                {
                    Name = TestConstants.ProjectName,
                    AbsolutePath = Path.Combine(TestConstants.TestDirectory, "test.csproj"),
                    Dependencies = new ProjectDescriptor[] {  },
                    ProjectGuid  = Guid.NewGuid().ToString(),
                    Files = new []
                    {
                        "a.cs", "b.cs"
                    }
                }
            });
            Debug.WriteLine(text);

            Assert.IsTrue(text != null);
        }

        [TestMethod]
        [TestCategory("Generation")]
        public void TestSolutionAndProjectFileGeneration()
        {
            if (Directory.Exists(TestConstants.TestDirectory))
            {
                Directory.Delete(TestConstants.TestDirectory, true);
            }
            var solution = SolutionFileGenerator.GenerateSolutionWithProjects(
                TestConstants.SolutionPath,
                new[]
                {
                    new ProjectDescriptor
                    {
                        Name = TestConstants.ProjectName,
                        AbsolutePath = Path.Combine(TestConstants.TestDirectory, "test.csproj"),
                        Dependencies = new ProjectDescriptor[] {  },
                        ProjectGuid  = Guid.NewGuid().ToString(),
                        Files = new []
                        {
                            "a.cs", "b.cs"
                        }
                    }
                });
            Assert.IsTrue(solution != null);
        }

        [TestMethod]
        [TestCategory("Generation")]
        public void TestZipSolutionGeneration()
        {
              var zipFile = SolutionFileGenerator.GenerateSolutionWithProjectsAsAZip(
                TestConstants.SolutionPath,
                new[]
                {
                    new ProjectDescriptor
                    {
                        Name = TestConstants.ProjectName,
                        AbsolutePath = Path.Combine(TestConstants.TestDirectory, "test.csproj"),
                        Dependencies = new ProjectDescriptor[] {  },
                        ProjectGuid  = Guid.NewGuid().ToString(),
                        Files = new []
                        {
                            "a.cs", "b.cs"
                        }
                    }
                });
            Assert.IsNotNull(zipFile);
        }

        [TestMethod]
        [TestCategory("Generation")]
        public void TestZipSolutionGenerationWithComplexProjects()
        {
            try
            {
                if (Directory.Exists(TestConstants.TestDirectory))
                {
                    Directory.Delete(TestConstants.TestDirectory, true);
                }
                Directory.CreateDirectory(TestConstants.TestDirectory);

                var writingTo = Path.Combine(Directory.GetCurrentDirectory(), TestConstants.TestDirectory);
                Trace.TraceInformation("Writing to {0}", writingTo);

                var callgraph = GenerateConnectedCallGraph(1000);
                var numProjects = 30;
                var syntaxes = GenerateCodeWithDifferentProjects(callgraph, numProjects);
                int index = 0;
                var descriptors = new List<ProjectDescriptor>();
                //var projectNames = new List<ProjectDescriptor>();
                //for (index = 0; index < numProjects; index++)
                //{
                //    projectNames.Add(new ProjectDescriptor
                //    {
                //        Name = string.Format("P{0}", index),
                //        AbsolutePath = string.Format("P{1}.csproj", TestConstants.TestDirectory, index),
                //    });
                //}
                foreach (var syntax in syntaxes)
                {
                    var code = syntax.ToFullString();

                    var fileName = string.Format("file{0}.cs", index);
                    Trace.TraceInformation(fileName);
                    Directory.CreateDirectory(TestConstants.TemporarySolutionDirectory);
                    Directory.CreateDirectory(Path.Combine(TestConstants.TemporarySolutionDirectory, TestConstants.TestDirectory));

                    var csFileName = Path.Combine(
                            Path.Combine(TestConstants.TemporarySolutionDirectory, TestConstants.TestDirectory),
                            fileName);
                    File.WriteAllText(csFileName, code);
                    Trace.TraceInformation("Writing to {0}: {1}", csFileName, code);

                    descriptors.Add(new ProjectDescriptor
                    {
                        AbsolutePath = string.Format("{0}\\P{1}.csproj", TestConstants.TestDirectory, index),
                        Name = string.Format("P{0}", index),
                        ProjectGuid = Guid.NewGuid().ToString(),
                        Files = new[] { fileName },
                        Type = (index == numProjects - 1) ? ProjectType.Executable : ProjectType.Library,
                        // TODO: we may need to worry about project 
                        // dependencies because it will likely fail to compile without them
                        Dependencies = descriptors.Take(index),
                    });
                    index++;
                }
                var zipFile = SolutionFileGenerator.GenerateSolutionWithProjectsAsAZip(TestConstants.SolutionPath, descriptors);
              Assert.IsTrue(zipFile != null);
                //Console.WriteLine(code);
            }
            finally
            {
                //Directory.Delete(TestConstants.TestDirectory, true);
            }
        }

        [TestMethod]
        [TestCategory("Generation")]
        public void TestZipSolutionGenerationWithIncreasingSizes()
        {
            // increasing sizes of solutions
            //int[] sizes = { 100, 1000, 10000, 10000, 100000, 1000000 };
            int[] sizes = { 500000 };
            foreach (var solutionSize in sizes)
            {
                Trace.TraceInformation("Generating a new solution for size {0}", solutionSize);
                try
                {
                    if (Directory.Exists(TestConstants.TestDirectory))
                    {
                        Directory.Delete(TestConstants.TestDirectory, true);
                    }
                    Directory.CreateDirectory(TestConstants.TestDirectory);

                    var writingTo = Path.Combine(Directory.GetCurrentDirectory(), TestConstants.TestDirectory);
                    Trace.TraceInformation("Writing to {0}", writingTo);

                    var callgraph = GenerateConnectedCallGraph(solutionSize);
                    Trace.TraceInformation("Call graph generation succeeded.");
                    var numProjects = (int)Math.Ceiling((decimal)solutionSize / 1000);
                    Assert.IsTrue(numProjects > 0);
                    var syntaxes = GenerateCodeWithDifferentProjects(callgraph, numProjects);
                    int index = 0;
                    var descriptors = new List<ProjectDescriptor>();
                    foreach (var syntax in syntaxes)
                    {
                        var code = syntax.ToFullString();

                        var fileName = string.Format("file{0}.cs", index);
                        Trace.TraceInformation(fileName);
                        Directory.CreateDirectory(TestConstants.TemporarySolutionDirectory);
                        Directory.CreateDirectory(Path.Combine(TestConstants.TemporarySolutionDirectory, TestConstants.TestDirectory));

                        var csFileName = Path.Combine(
                                Path.Combine(TestConstants.TemporarySolutionDirectory, TestConstants.TestDirectory),
                                fileName);
                        File.WriteAllText(csFileName, code);
                        //Trace.TraceInformation("Writing to {0}: {1}", csFileName, code);

                        descriptors.Add(new ProjectDescriptor
                        {
                            AbsolutePath = string.Format("{0}\\P{1}.csproj", TestConstants.TestDirectory, index),
                            Name = string.Format("P{0}", index),
                            ProjectGuid = Guid.NewGuid().ToString(),
                            Files = new[] { fileName },
                            Type = (index == numProjects - 1) ? ProjectType.Executable : ProjectType.Library,
                            // TODO: we may need to worry about project 
                            // dependencies because it will likely fail to compile without them
                            Dependencies = descriptors.Take(index),
                        });
                        index++;
                    }
                    var zipFile = SolutionFileGenerator.GenerateSolutionWithProjectsAsAZip(
                        TestConstants.SolutionPath, descriptors, string.Format("synthetic-{0}", solutionSize));
                    Assert.IsTrue(zipFile != null);
                    Trace.TraceInformation("Writing a solution of size {0} with {1} projects to {1}",
                        solutionSize, numProjects, zipFile);
                    //Console.WriteLine(code);
                }
                finally
                {
                    //Directory.Delete(TestConstants.TestDirectory, true);
                }
            }
        }
        
        [TestMethod]
        [TestCategory("Generation")]
        public void TestSolutionGenerationAndReading()
        {
            string solutionPath = @"test.sln";
            string directoryName = "test";
            try
            {

                if (Directory.Exists(TestConstants.TestDirectory))
                {
                    Directory.Delete(TestConstants.TestDirectory, true);
                }
                Directory.CreateDirectory(TestConstants.TestDirectory);

                var project = new ProjectDescriptor
                {
                    Name = TestConstants.ProjectName,
                    AbsolutePath = Path.Combine(TestConstants.TestDirectory, "test.csproj"),
                    Dependencies = new ProjectDescriptor[] { },
                    ProjectGuid = Guid.NewGuid().ToString(),
                    Files = new[]
                    {
                        "a.cs",
                        "b.cs"
                    }
                };
                var text = SolutionFileGenerator.GenerateSolutionText(new[] { project });
                Assert.IsTrue(text != null);
                Trace.TraceInformation(text);
                File.WriteAllText(solutionPath, text);
                Directory.CreateDirectory(directoryName);
                var projectContents = SolutionFileGenerator.CreateProjectFile(project);
                File.WriteAllText("test\\test.csproj", projectContents);
                Trace.TraceInformation(projectContents);

                var msWorkspace = MSBuildWorkspace.Create();
                var solution = msWorkspace.OpenSolutionAsync(solutionPath).Result;
                Trace.TraceInformation("Opened {0} projects", solution.Projects.Count());
                Assert.IsTrue(solution != null);
                Assert.IsTrue(solution.Projects.Count() > 0);
            }
            finally
            {
                // cleanup
                File.Delete(solutionPath);
                Directory.Delete(directoryName, true);
            }
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
            var source = syntax.ToFullString();
            Logger.Instance.Log("CallGraphGenerator", "AnalyzeGenerationSolution1", "source code: {0}", source);
            //var solution = ReachingTypeAnalysis.Utils.CreateSolution(source);
            //Logger.Instance.Log("CallGraphGenerator", "AnalyzeGenerationSolution1", "solution filename: {0}", solution.FilePath);
            var solAnalyzer = SolutionAnalyzer.CreateFromSource(source);
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
                Assert.IsTrue(callees.Count() == resolvedCallees.Count(), "Mismatched callee counts for " + node);
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
