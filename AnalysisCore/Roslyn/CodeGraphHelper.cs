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
				declAssembly = symbol.ContainingAssembly.Name,
				symbolType = SymbolType.Method,
				label = symbol.Name,
				hover = symbol.ToDisplayString(),
				refType = "decl",
				glyph = "72",
                range = new Range()
				{
					startLineNumber = span.StartLinePosition.Line + 1,
					startColumn = span.StartLinePosition.Character + 1,
					endLineNumber = span.EndLinePosition.Line + 1,
					endColumn = span.EndLinePosition.Character + 1
				}
			};

			this.DocumentInfo.declarationAnnotation.Add(declaration);

			base.VisitMethodDeclaration(node);
		}
	}
}
