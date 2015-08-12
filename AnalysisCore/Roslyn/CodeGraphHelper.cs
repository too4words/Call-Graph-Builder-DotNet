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

namespace AnalysisCore.Roslyn
{
	class CodeGraphHelper
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

		public static SymbolReference GetDeclarationInfo(IMethodSymbol symbol)
		{
			var span = symbol.Locations.First().GetMappedLineSpan();

			var result = new SymbolReference()
			{
				refType = "ref",
				preview = span.Path,				
				trange = CodeGraphHelper.GetRange(span)
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

		public static string GetSymbolId(IMethodSymbol symbol)
		{
			var methodDescriptor =  Utils.CreateMethodDescriptor(symbol);
			var result = methodDescriptor.Marshall();

			return result;
		}

		public static string GetSymbolId(IMethodSymbol symbol, int invocationIndex)
		{
			var result = GetSymbolId(symbol);
			result = string.Format("{0}@{1}", result, invocationIndex);

			return result;
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

		private void VisitBaseMethodDeclaration(BaseMethodDeclarationSyntax node, FileLinePositionSpan span)
		{
			var symbol = this.model.GetDeclaredSymbol(node);

			var declaration = new DeclarationAnnotation()
			{
				symbolId = CodeGraphHelper.GetSymbolId(symbol),
				symbolType = SymbolType.Method,
				label = symbol.Name,
				hover = symbol.ToDisplayString(),
				refType = "decl",
				glyph = "72",
				range = CodeGraphHelper.GetRange(span)

			};

			this.DocumentInfo.declarationAnnotation.Add(declaration);
			this.currentMethodSymbol = symbol;
			this.invocationIndex = 0;
		}

		public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
		{
			var span = node.SyntaxTree.GetLineSpan(node.Identifier.Span);
			this.VisitBaseMethodDeclaration(node, span);
			base.VisitConstructorDeclaration(node);
			this.currentMethodSymbol = null;
		}

		public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
		{
			var span = node.SyntaxTree.GetLineSpan(node.Identifier.Span);
			this.VisitBaseMethodDeclaration(node, span);
			base.VisitMethodDeclaration(node);
			this.currentMethodSymbol = null;
		}

		private void VisitBaseMethodInvocationExpression(SimpleNameSyntax methodName, IMethodSymbol methodSymbol)
		{
			this.invocationIndex++;
			var span = methodName.SyntaxTree.GetLineSpan(methodName.Span);

			var reference = new ReferenceAnnotation()
			{
				declarationId = CodeGraphHelper.GetSymbolId(methodSymbol),
				symbolId = CodeGraphHelper.GetSymbolId(this.currentMethodSymbol, this.invocationIndex),
				declFile = methodSymbol.Locations.First().GetMappedLineSpan().Path,
				symbolType = SymbolType.Method,
				label = methodSymbol.Name,
				hover = methodSymbol.ToDisplayString(),
				refType = "ref",
				range = CodeGraphHelper.GetRange(span)
			};

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
