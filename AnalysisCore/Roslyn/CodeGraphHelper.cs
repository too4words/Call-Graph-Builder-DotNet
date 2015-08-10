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

		public static string GetSymbolId(ISymbol symbol)
		{
			var moduleName = symbol.ContainingModule != null ? symbol.ContainingModule.Name : "shared";
			var assemblyName = symbol.ContainingAssembly != null ? symbol.ContainingAssembly.ToDisplayString() : "shared";
			var symbolString = string.Empty;

			try
			{
				// Use GetDocumentationCommentId as a unique string for the symbol.
				// N.B. Since GetDocumentationCommentId can throw exception and return null
				// will it be okay just use symbol.ToString()?
				symbolString = symbol.GetDocumentationCommentId();
			}
			catch (InvalidOperationException ex)
			{
				symbolString = symbol.ToString();
			}

			return string.Format("{0}:{1}:{2}", moduleName, assemblyName, symbolString);
		}
	}

	class DocumentVisitor : CSharpSyntaxWalker
	{
		private SemanticModel model;

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

		public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
		{
			var span = node.SyntaxTree.GetLineSpan(node.Identifier.Span);
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

			base.VisitMethodDeclaration(node);
		}

		public override void VisitInvocationExpression(InvocationExpressionSyntax node)
		{
			var memberAccess = node.Expression as MemberAccessExpressionSyntax;

			if (memberAccess != null)
			{
				var span = node.SyntaxTree.GetLineSpan(memberAccess.Name.Span);
				var symbolInfo = this.model.GetSymbolInfo(memberAccess.Name);
				var symbol = symbolInfo.Symbol;

				var reference = new ReferenceAnnotation()
				{
					symbolId = CodeGraphHelper.GetSymbolId(symbol),
					declFile = symbol.Locations.First().GetMappedLineSpan().Path,
					symbolType = SymbolType.Method,
					label = symbol.Name,
					hover = symbol.ToDisplayString(),
					refType = "ref",
					range = CodeGraphHelper.GetRange(span)
				};

				this.DocumentInfo.referenceAnnotation.Add(reference);
			}

			base.VisitInvocationExpression(node);
		}
	}
}
