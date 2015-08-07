using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeGraphModel;
using Microsoft.CodeAnalysis;

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

		private static FileResponse CreateFileResponse(Document document)
		{
			var result = new FileResponse()
			{
				uid = document.Id.Id.ToString(),
				filepath = document.FilePath,
				assemblyname = document.Project.AssemblyName
			};

			return result;
		}

		public static Task<IEnumerable<FileResponse>> GetDocumentEntitiesAsync(Document document)
		{
			var result = new List<FileResponse>();

			return Task.FromResult(result.AsEnumerable());
		}
	}
}
