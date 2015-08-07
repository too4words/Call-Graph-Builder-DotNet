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
	public class OrleansController : ApiController
    {
		private const string ROOT_DIR = @"C:\Users\t-edzopp\Desktop\ArcusClientPrototype\src\ArcusClient\data\";

		public static ISolutionManager SolutionManager { get; set; }
		private static IDictionary<string, string> documentsAssemblyName;

		static OrleansController()
		{
			documentsAssemblyName = new Dictionary<string, string>();
        }

		/// <summary>
		/// Get All Files in the graph (Abbreviated)
		/// </summary>
		[Route("api/orleans/{graph}/files")]
		public async Task<IEnumerable<FileResponse>> GetAllFilesAsync(string graph)
		{
			using (TimedLog.Time(graph + " :: Get All Files"))
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
		}

		private static bool FilterFile(FileResponse file)
		{
			// TODO: Hack!!!
			var filename = Path.GetFileName(file.filepath);
			if (filename.StartsWith(".NETFramework,")) return true;

			var buildInfo = new BuildInfo();
			var filepath = file.filepath;

			if (filepath.StartsWith(ROOT_DIR, StringComparison.InvariantCultureIgnoreCase))
			{
				filepath = filepath.Substring(ROOT_DIR.Length, filepath.Length - ROOT_DIR.Length);
			}

			file.filepath = filepath.Replace(@"\", "/");
			file.repository = buildInfo.RepositoryName;
			file.version = buildInfo.VersionName;

			documentsAssemblyName[file.filepath] = file.assemblyname;
			return false;
		}

		/// <summary>
		/// Get Full Files matching specified file path
		/// </summary>
		[Route("api/orleans/{graph}/entities/file/{*filePath}")]
		public async Task<IEnumerable<FileResponse>> GetFileEntitiesAsync(string graph, string filePath)
		{
			using (TimedLog.Time(graph + " :: Get File"))
			{
				var fullPath = Path.Combine(ROOT_DIR, filePath).Replace("/", @"\");
				var assemblyName = documentsAssemblyName[filePath];
				var provider = await SolutionManager.GetProjectCodeProviderAsync(assemblyName);
				var result = await provider.GetDocumentEntitiesAsync(fullPath);

				return result;
			}
		}

		/// <summary>
		/// Get All references of symbol with specified Uid
		/// </summary>
		[Route("api/orleans/{graph}/references/{uid}")]
        public Task<IEnumerable<SymbolReference>> GetReferencesAsync(string graph, string uid)
        {
            using (TimedLog.Time(graph + " :: Get References"))
            {
				var result = new List<SymbolReference>();
				return Task.FromResult(result.AsEnumerable());
			}
        }

        /// <summary>
        /// Get Most referenced of symbols
        /// </summary>
        [Route("api/orleans/{graph}/referencecount/{count}")]
        public Task<IEnumerable<SymbolReferenceCount>> GetReferenceCountsAsync(string graph, uint count)
        {
            using (TimedLog.Time(graph + " :: Reference Count"))
            {
				var result = new List<SymbolReferenceCount>();
				return Task.FromResult(result.AsEnumerable());
			}
        }

        /// <summary>
        /// Get Most Co-occurring symbols
        /// </summary>
        [Route("api/orleans/{graph}/co-occurs/file/{count}")]
        public Task<IEnumerable<SymbolReferenceCount>> GetCoOccursCountsAsync(string graph, uint count)
        {
            using (TimedLog.Time(graph + " :: Co-occurrence Count"))
            {
				var result = new List<SymbolReferenceCount>();
				return Task.FromResult(result.AsEnumerable());
			}
        }
    }
}
