using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeGraphModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReachingTypeAnalysis;
using ReachingTypeAnalysis.Analysis;

namespace ReachingTypeAnalysis.Roslyn
{
	internal class CodeGraphHelper
	{
		public static Task<IEnumerable<FileResponse>> GetDocumentsAsync(Project project)
		{
			var result = new List<FileResponse>();

			foreach (var document in project.Documents)
			{
				var fileResponse = CreateFileResponse(document);
				result.Add(fileResponse);
			}

			return Task.FromResult(result.AsEnumerable());
		}

		public static FileResponse CreateFileResponse(Document document)
		{
			var result = new FileResponse()
			{
				uid = document.Id.Id.ToString(),
				filepath = document.FilePath,
				assemblyname = document.Project.AssemblyName
			};

			return result;
		}

		public static async Task<IEnumerable<FileResponse>> GetDocumentEntitiesAsync(Document document)
		{			
			var visitor = new DocumentVisitor();
			var documentInfo = await visitor.VisitAsync(document);
			var result = new List<FileResponse>() { documentInfo };

			return result;
		}

		public static async Task<IEnumerable<FileResponse>> GetDocumentEntitiesAsync(IProjectCodeProvider projectProvider, ReachingTypeAnalysis.Analysis.DocumentInfo documentInfo)
		{
			var result = CodeGraphHelper.CreateFileResponse(documentInfo.Document);
			result.declarationAnnotation = new List<DeclarationAnnotation>();
			result.referenceAnnotation = new List<ReferenceAnnotation>();

			foreach (var methodDescriptor in documentInfo.DeclaredMethods.Keys)
			{
				var methodEntity = await projectProvider.GetMethodEntityAsync(methodDescriptor);
				var annotations = await methodEntity.GetAnnotationsAsync();
				var declarations = annotations.OfType<DeclarationAnnotation>();
				var references = annotations.OfType<ReferenceAnnotation>();

				result.declarationAnnotation.AddRange(declarations);
				result.referenceAnnotation.AddRange(references);
			}
			
			return new List<FileResponse>() { result };
		}

		public static DeclarationAnnotation GetMethodDeclarationInfo(SyntaxNode node, IMethodSymbol symbol)
		{
			var span = CodeGraphHelper.GetSpan(node);

			var result = new DeclarationAnnotation()
			{
				symbolId = CodeGraphHelper.GetSymbolId(symbol),
				symbolType = SymbolType.Method,
				label = symbol.Name,
				hover = symbol.ToDisplayString(),
				refType = "decl",
				glyph = "72",
				range = CodeGraphHelper.GetRange(span)
			};

			return result;
		}

		public static ReferenceAnnotation GetMethodInvocationInfo(MethodDescriptor callerDescriptor, AnalysisCallNode callNode)
		{
			var result = new ReferenceAnnotation()
			{
				declarationId = CodeGraphHelper.GetSymbolId(callNode.AdditionalInfo.StaticMethodDescriptor),
				symbolId = CodeGraphHelper.GetSymbolId(callerDescriptor, callNode.InMethodPosition),
				declFile = callNode.AdditionalInfo.StaticMethodDeclarationPath,
				symbolType = SymbolType.Method,
				label = callNode.Name,
				hover = callNode.AdditionalInfo.DisplayString,
				refType = "ref",
				range = callNode.LocationDescriptor.Range
			};

			return result;
		}

		public static ReferenceAnnotation GetMethodInvocationInfo(IMethodSymbol caller, IMethodSymbol callee, int invocationIndex, FileLinePositionSpan span)
		{
			var result = new ReferenceAnnotation()
			{
				declarationId = CodeGraphHelper.GetSymbolId(callee),
				symbolId = CodeGraphHelper.GetSymbolId(caller, invocationIndex),
				declFile = callee.Locations.First().GetMappedLineSpan().Path,
				symbolType = SymbolType.Method,
				label = callee.Name,
				hover = callee.ToDisplayString(),
				refType = "ref",
				range = CodeGraphHelper.GetRange(span)
			};

			return result;
		}

		public static SymbolReference GetMethodReferenceInfo(AnalysisCallNode callNode)
		{
			var result = new SymbolReference()
			{
				refType = "ref",
				preview = callNode.LocationDescriptor.FilePath,
				trange = callNode.LocationDescriptor.Range
			};

			return result;
		}

		public static SymbolReference GetMethodReferenceInfo(IMethodSymbol symbol)
		{
			var span = symbol.Locations.First().GetMappedLineSpan();

			var result = new SymbolReference()
			{
				refType = "ref",
				preview = span.Path,				
				trange = GetRange(span)
			};

			return result;
		}

		public static Range GetRange(FileLinePositionSpan span)
		{
			return new Range()
			{
				startLineNumber = span.StartLinePosition.Line + 1,
				startColumn = span.StartLinePosition.Character + 1,
				endLineNumber = span.EndLinePosition.Line + 1,
				endColumn = span.EndLinePosition.Character + 1
			};
        }

		public static FileLinePositionSpan GetSpan(SyntaxNodeOrToken nodeOrToken)
		{
			var span = nodeOrToken.Span;

			if (nodeOrToken.IsNode)
			{
				var node = nodeOrToken.AsNode();

				if (node is ConstructorDeclarationSyntax)
				{
					var constructorDeclarationNode = node as ConstructorDeclarationSyntax;
					span = constructorDeclarationNode.Identifier.Span;
				}
				else if (node is MethodDeclarationSyntax)
				{
					var methodDeclarationNode = node as MethodDeclarationSyntax;
					span = methodDeclarationNode.Identifier.Span;
				}
				else if (node is AccessorDeclarationSyntax)
				{
					var accessprDecl = node as AccessorDeclarationSyntax;
					span = accessprDecl.Keyword.Span;
				}

				else if (node is ObjectCreationExpressionSyntax)
				{
					var objectCreationExpression = node as ObjectCreationExpressionSyntax;
					span = objectCreationExpression.Type.Span;
				}
				else if (node is MemberAccessExpressionSyntax)
				{
					var memberAccess = node as MemberAccessExpressionSyntax;
					span = memberAccess.Name.Span;
				}
				else if (node is InvocationExpressionSyntax)
				{
					var invocationExpression = node as InvocationExpressionSyntax;
					span = invocationExpression.Expression.Span;

					if (invocationExpression.Expression is MemberAccessExpressionSyntax)
					{
						var memberAccess = invocationExpression.Expression as MemberAccessExpressionSyntax;
						span = memberAccess.Name.Span;
					}
				}
			}

			var result = nodeOrToken.SyntaxTree.GetLineSpan(span);
			return result;
		}

		public static string GetSymbolId(MethodDescriptor methodDescriptor)
		{
			var result = methodDescriptor.Marshall();
			return result;
		}

		public static string GetSymbolId(MethodDescriptor methodDescriptor, int invocationIndex)
		{
			var result = CodeGraphHelper.GetSymbolId(methodDescriptor);
			result = string.Format("{0}@{1}", result, invocationIndex);
			return result;
		}

		public static string GetSymbolId(IMethodSymbol symbol)
		{
			var methodDescriptor = Utils.CreateMethodDescriptor(symbol);
			return CodeGraphHelper.GetSymbolId(methodDescriptor);
		}

		public static string GetSymbolId(IMethodSymbol symbol, int invocationIndex)
		{
			var methodDescriptor = Utils.CreateMethodDescriptor(symbol);
			return CodeGraphHelper.GetSymbolId(methodDescriptor, invocationIndex);
		}
	}

	class DocumentVisitor : CSharpSyntaxWalker
	{
		private SemanticModel model;
		private IMethodSymbol currentMethodSymbol;
		private int invocationIndex;

		public FileResponse DocumentInfo { get; private set; }

		public DocumentVisitor()
		{
		}

		public async Task<FileResponse> VisitAsync(Document document)
		{
            this.DocumentInfo = CodeGraphHelper.CreateFileResponse(document);
			this.DocumentInfo.declarationAnnotation = new List<DeclarationAnnotation>();
			this.DocumentInfo.referenceAnnotation = new List<ReferenceAnnotation>();
			this.model = await document.GetSemanticModelAsync();

			var root = await document.GetSyntaxRootAsync();
			this.Visit(root);

			return this.DocumentInfo;
        }

		private void VisitBaseMethodDeclaration(BaseMethodDeclarationSyntax node)
		{
			var symbol = this.model.GetDeclaredSymbol(node);
			var declaration = CodeGraphHelper.GetMethodDeclarationInfo(node, symbol);

			this.DocumentInfo.declarationAnnotation.Add(declaration);
			this.currentMethodSymbol = symbol;
			this.invocationIndex = 0;
		}

		public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
		{
			this.VisitBaseMethodDeclaration(node);
			base.VisitConstructorDeclaration(node);
			this.currentMethodSymbol = null;
		}

		public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
		{
			this.VisitBaseMethodDeclaration(node);
			base.VisitMethodDeclaration(node);
			this.currentMethodSymbol = null;
		}

		private void VisitBaseMethodInvocationExpression(SimpleNameSyntax methodName, IMethodSymbol methodSymbol)
		{
			this.invocationIndex++;
			var span = methodName.SyntaxTree.GetLineSpan(methodName.Span);
			var reference = CodeGraphHelper.GetMethodInvocationInfo(this.currentMethodSymbol, methodSymbol, this.invocationIndex, span);

			this.DocumentInfo.referenceAnnotation.Add(reference);
		}

		public override void VisitInvocationExpression(InvocationExpressionSyntax node)
		{
			if (node.Expression is MemberAccessExpressionSyntax)
			{
				var memberAccess = node.Expression as MemberAccessExpressionSyntax;
				var methodName = memberAccess.Name;
                var symbolInfo = this.model.GetSymbolInfo(node);
				var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

				this.VisitBaseMethodInvocationExpression(methodName, methodSymbol);
			}

			base.VisitInvocationExpression(node);
		}

		public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
		{
			if (node.Type is SimpleNameSyntax)
			{
				var methodName = node.Type as SimpleNameSyntax;
				var symbolInfo = this.model.GetSymbolInfo(node);
				var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

				this.VisitBaseMethodInvocationExpression(methodName, methodSymbol);
			}

			base.VisitObjectCreationExpression(node);
		}
	}
}
