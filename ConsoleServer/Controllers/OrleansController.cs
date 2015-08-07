using CodeGraphModel;
using ConsoleServer.Utils;
using ReachingTypeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace ConsoleServer.Controllers
{
	// http://localhost:14054/index.html?graph=ConsoleApplication1&mode=orleans
	public class OrleansController: ApiController
    {
		public static ISolutionManager SolutionManager { get; set; }

        /// <summary>
        /// Get All Files in the graph (Abbreviated)
        /// </summary>
        [Route("api/orleans/{graph}/files")]
        public IEnumerable<FileResponse> GetAllFiles(string graph)
        {
            using (TimedLog.Time(graph + " :: Get All Files"))
            {
				var result = GetAllFilesAsync(graph).Result;
				return result;
            }
        }

		private async Task<IEnumerable<FileResponse>> GetAllFilesAsync(string graph)
		{
			var result = new List<FileResponse>();
			var providers = await SolutionManager.GetProjectCodeProvidersAsync();

			foreach (var provider in providers)
			{
				var files = await provider.GetDocumentsAsync();

				files = from f in files
						where !FilterFile(f)
						select f;

				result.AddRange(files);
			}

			return result;
		}

		private static bool FilterFile(FileResponse file)
		{
			// TODO: Hack!!!
			var filename = Path.GetFileName(file.filepath);
			if (filename.StartsWith(".NETFramework,")) return true;

			var dir = @"C:\Users\t-edzopp\Desktop\ArcusClientPrototype\src\ArcusClient\data\";
			var buildInfo = new BuildInfo();
			var filepath = file.filepath;

			if (filepath.StartsWith(dir))
			{
				filepath = filepath.Substring(0, dir.Length);
			}

			file.filepath = filepath.Replace(@"\", "/");
			file.repository = buildInfo.RepositoryName;
			file.version = buildInfo.VersionName;

			return false;
		}

		/// <summary>
		/// Get Full Files matching specified file path
		/// </summary>
		[Route("api/orleans/{graph}/entities/file/{*filePath}")]
        public IEnumerable<FileResponse> GetFileEntities(string graph, string filePath)
        {
            using (TimedLog.Time(graph + " :: Get File"))
            {
				return new List<FileResponse>();
			}
        }

        /// <summary>
        /// Get All references of symbol with specified Uid
        /// </summary>
        [Route("api/orleans/{graph}/references/{uid}")]
        public IEnumerable<SymbolReference> GetReferences(string graph, string uid)
        {
            using (TimedLog.Time(graph + " :: Get References"))
            {
				return new List<SymbolReference>();
			}
        }

        /// <summary>
        /// Get Most referenced of symbols
        /// </summary>
        [Route("api/orleans/{graph}/referencecount/{count}")]
        public IEnumerable<SymbolReferenceCount> GetReferenceCounts(string graph, uint count)
        {
            using (TimedLog.Time(graph + " :: Reference Count"))
            {
				return new List<SymbolReferenceCount>();
			}
        }

        /// <summary>
        /// Get Most Co-occurring symbols
        /// </summary>
        [Route("api/orleans/{graph}/co-occurs/file/{count}")]
        public IEnumerable<SymbolReferenceCount> GetCoOccursCounts(string graph, uint count)
        {
            using (TimedLog.Time(graph + " :: Co-occurrence Count"))
            {
				return new List<SymbolReferenceCount>();
			}
        }
    }
}
