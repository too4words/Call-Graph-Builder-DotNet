// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Roslyn
{
    /// <summary>
    /// This is just a test when we tried to make a simpler version of AST
    /// Still pretty imcomplete and is maybe not necessary
    /// This test version is iterative version and implemented just for invocations and allocations 
    /// A new version should be recursive and simplify member access expresions and other complex expressions
    /// </summary>
    class MethodSimpifier
    {
        public static SyntaxNode Test(SyntaxNode methodNode)
        {
            var instrumenter = new MyInvocRewriter();
            var simplifiedMethod = instrumenter.Visit(methodNode);
            Console.WriteLine("=====  new code =====");
            Console.WriteLine(simplifiedMethod);
            Console.WriteLine("==========");
            //Console.ReadLine();
            return simplifiedMethod;
        }
        internal class MyInvocRewriter : CSharpSyntaxRewriter
        {
            
            public override SyntaxNode VisitBlock(BlockSyntax node)
            {
                BlockSyntax block = (BlockSyntax)base.VisitBlock(node);
                SyntaxList<StatementSyntax> curList = new SyntaxList<StatementSyntax>();
                Dictionary<string, SyntaxNode> replacements = new Dictionary<string, SyntaxNode>();

                int numbering = 1;
                foreach (var stmt in block.Statements)
                {
                    SyntaxList<StatementSyntax> preList = new SyntaxList<StatementSyntax>();
                    var stm = stmt.ReplaceNodes(nodes: stmt.DescendantNodes().Reverse(), computeReplacementNode: (original, origWithReplacedDesc) =>
                    {
                        Console.WriteLine(origWithReplacedDesc.GetType() + ": " + origWithReplacedDesc);

                        if (origWithReplacedDesc.IsKind(SyntaxKind.InvocationExpression)
                            || origWithReplacedDesc.IsKind(SyntaxKind.ObjectCreationExpression))
                        {
                            return SimplifyMethodAndConstructorInvocation(ref numbering, ref preList, original, origWithReplacedDesc);
                        }
                        return origWithReplacedDesc;
                    });
                    curList = curList.AddRange(preList);
                    curList = curList.Add(stm);
                }
                return block.WithStatements(curList);
            }

            private static SyntaxNode SimplifyMethodAndConstructorInvocation(ref int numbering, ref SyntaxList<StatementSyntax> preList, SyntaxNode original, SyntaxNode origWithReplacedDesc)
            {
                SeparatedSyntaxList<ArgumentSyntax> slst = new SeparatedSyntaxList<ArgumentSyntax>();
                SeparatedSyntaxList<ArgumentSyntax> myArgs;
                InvocationExpressionSyntax ies = null;
                ObjectCreationExpressionSyntax oces = null;
                // es necesario manejarlos por separado, porque pese a que ambos tienen como propiedad Arguments
                // de tipo SeparatedSyntaxList<ArgumentSyntax>, no estan en la misma linea de jerarquia
                // de clases... entonces:
                if (origWithReplacedDesc.IsKind(SyntaxKind.InvocationExpression))
                {
                    ies = (InvocationExpressionSyntax)origWithReplacedDesc;
                    myArgs = ies.ArgumentList.Arguments;
                }
                else
                {
                    oces = (ObjectCreationExpressionSyntax)origWithReplacedDesc;
                    myArgs = oces.ArgumentList.Arguments;
                }
                foreach (var arg in myArgs)
                {
                    if (!(arg.Expression is LiteralExpressionSyntax || arg.Expression is IdentifierNameSyntax))
                    {
                        numbering++;
                        preList = preList.Add(SyntaxFactory.ParseStatement("var __a" + numbering + " = " + arg + ";"));
                        slst = slst.Add((SyntaxFactory.Argument(SyntaxFactory.ParseExpression("__a" + numbering))));
                    }
                }

                if (slst.Count() > 0)
                {
                    var argumentList = SyntaxFactory.ArgumentList(slst);
                    if (origWithReplacedDesc.IsKind(SyntaxKind.InvocationExpression))
                    {
                        return ies.WithArgumentList(argumentList);
                    }
                    else
                    {
                        return oces.WithArgumentList(argumentList);
                    }
                }
                else return original;
            }
        }
        /// <summary>
        /// This is jus a test to check the ability to preprocess the AST
        /// </summary>
        /// <param name="methodNode"></param>
        /// <param name="semanticModel"></param>
        /// <returns></returns>
        public static IMethodSymbol SimplifyASTForMethod(ref SyntaxNode methodNode, ref SemanticModel semanticModel)
        {
            var oldMethod = semanticModel.GetDeclaredSymbol(methodNode) as IMethodSymbol;


            var newMethodNode = MethodSimpifier.Test(methodNode);

            var annotation = new SyntaxAnnotation("Hi");

            newMethodNode = newMethodNode.WithAdditionalAnnotations(annotation);


            var root = methodNode.SyntaxTree.GetRoot();
            var newRoot = root.ReplaceNode(methodNode, newMethodNode);



            var oldCompilation = semanticModel.Compilation;

            var newCompilation = oldCompilation.ReplaceSyntaxTree(root.SyntaxTree, newRoot.SyntaxTree);
            var newSemanticModel = newCompilation.GetSemanticModel(newRoot.SyntaxTree);


            var recoveredMethodNode = newRoot.GetAnnotatedNodes(annotation).Single();

            var method = newSemanticModel.GetDeclaredSymbol(recoveredMethodNode) as IMethodSymbol;

            methodNode = recoveredMethodNode;
            semanticModel = newSemanticModel;

            return method;

        }
    }
}
