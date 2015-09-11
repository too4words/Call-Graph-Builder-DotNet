//---------------------------------------------------------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//---------------------------------------------------------------------------------------------------------------------

namespace WebAPI
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Web.Http;
	using System;
	using CodeGraphModel;
	using System.Threading.Tasks;
	using ReachingTypeAnalysis.Statistics;
	using ReachingTypeAnalysis;
	using Orleans;
	using System.IO;
	using OrleansInterfaces;

	/// <summary>
	/// Controller to handle all REST calls against graph entities
	/// </summary>
	/// 
	public class OrleansController : ApiController
    {
		//public const string ROOT_DIR = @"C:\Users\t-digarb\Source\Repos\ArcusClientPrototype\src\ArcusClient\data\";
		public const string ROOT_DIR = @"C:\Users\t-edzopp\Desktop\ArcusClientPrototype\src\ArcusClient\data\";

		private const AnalysisStrategyKind StrategyKind = AnalysisStrategyKind.ONDEMAND_ORLEANS;

		private static string solutionPath;
		private static SolutionAnalyzer analyzer;
		private static IDictionary<string, string> documentsAssemblyName;
		private static BuildInfo buildInfo;

		static OrleansController()
		{
			OrleansController.documentsAssemblyName = new Dictionary<string, string>();
			OrleansController.buildInfo = new BuildInfo();
        }

		public static ISolutionManager SolutionManager
		{
			get { return analyzer.SolutionManager; }
		}

		// http://localhost:49176/api/Orleans?solutionPath=Hola
		[HttpGet]
		public async Task AnalyzeSolutionAsync(string solutionPath, AnalysisStrategyKind strategyKind = StrategyKind)
		{
			{
				// Hack! Remove these lines
				//var solutionToTest = @"ConsoleApplication1\ConsoleApplication1.sln";
				var solutionToTest = @"Coby\Coby.sln";
				solutionPath = Path.Combine(OrleansController.ROOT_DIR, solutionToTest);
			}

			OrleansController.solutionPath = solutionPath;
			OrleansController.analyzer = SolutionAnalyzer.CreateFromSolution(solutionPath);
			await analyzer.AnalyzeAsync(strategyKind);

			// This call is to fill the documentsAssemblyName mapping
			await GetAllFilesAsync();
        }

		[HttpGet]
		public async Task AnalyzeUpdatesAsync(string gitDiffOutput)
		{
			var solutionFolder = Path.GetDirectoryName(solutionPath);
			var modifiedDocuments = gitDiffOutput.Split('\n')
												 .Where(docPath => !string.IsNullOrEmpty(docPath))
												 .Select(docPath => docPath.Replace("/", @"\"))
												 .Select(docPath => Path.Combine(solutionFolder, docPath))
												 .ToList();

			await analyzer.ApplyModificationsAsync(modifiedDocuments);
		}

		[HttpGet]
		public async Task GenerateCallGraphAsync(string outputPath)
		{
			var callgraph = await analyzer.GenerateCallGraphAsync();
			callgraph.Save(outputPath);
		}

		[HttpGet]
		public async Task<IList<FileResponse>> GetAllFilesAsync()
		{
			var providers = await SolutionManager.GetProjectCodeProvidersAsync();
			var result = new List<FileResponse>();

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

		[HttpGet]
        public IDictionary<string, long> GetItemsCount()
        {
			//var graphProvider = ArcusGraphService.CreateGraphProvider();

			//return new Dictionary<string, long>
			//{
			//	{"vertices", graphProvider.GetVerticesCount() },
			//	{"edges", graphProvider.GetEdgesCount() }
			//};

			//return new Dictionary<string, long>
			//{
			//	{"vertices", 0L },
			//	{"edges", 0L }
			//};

			throw new NotImplementedException();
		}

		/// <summary>
		/// _apis/arcusgraph/entities?entitytype={string}
		/// </summary>
		/// <param name="entityType">type of entity</param>
		/// <returns>a list of entities that matches the given type</returns>
		// http://localhost:49176/api/Orleans?entityType=File
		[HttpGet]
		public async Task<IList<FileResponse>> GetEntitiesAsync(string entityType)
		{
			IList<FileResponse> result = null;

			if (entityType == EntityType.File)
			{
				result = await GetAllFilesAsync();
			}
			else
			{
				throw new HttpResponseException(HttpStatusCode.NotFound);
			}

			return result;
		}

		/// <summary>
		/// Handle REST request for _apis/arcusgraph/entities?entitytype={string}&filepath={string}&repository={string}&version={string}.
		/// </summary>
		/// <param name="entityType">type of entity (has to be file type)</param>
		/// <param name="filepath">file path of the file entity</param>
		/// <param name="version">git version of file entity (by default is the git branch name)</param>
		/// <param name="repository">repository of the file entity</param>
		/// <returns>file entity that matches the filtering parameters</returns>
		// http://localhost:49176/api/Orleans?entityType=File&filePath=ConsoleApplication1/ConsoleApplication1/Program.cs&version=1&repository=hola
        [HttpGet]
        public async Task<IList<FileResponse>> GetFilesWithFilterAsync(string entityType, string filepath, string version, string repository)
        {
			if (entityType != EntityType.File)
			{
				throw new HttpResponseException(HttpStatusCode.BadRequest);
			}

			buildInfo = new BuildInfo()
			{
				VersionName = version,
				RepositoryName = repository
			};

			filepath = "coby/src/" + filepath;
			var fullPath = Path.Combine(ROOT_DIR, filepath).Replace("/", @"\");
			var assemblyName = documentsAssemblyName[filepath];
			var provider = await SolutionManager.GetProjectCodeProviderAsync(assemblyName);
			var result = await provider.GetDocumentEntitiesAsync(fullPath);

			foreach (var file in result)
			{
				ProcessFileResponse(file);
			}

			if (!result.Any())
			{
				throw new HttpResponseException(HttpStatusCode.NotFound);
			}

			return result.ToList();
		}

        /// <summary>
        /// _apis/arcusgraph/entities/{uid}
        /// </summary>
        /// <param name="uid">uniq id of an entity</param>
        /// <returns>entity object</returns>
        [HttpGet]
        public object GetEntityByUId(string uid)
        {
			//var graphProvider = ArcusGraphService.CreateGraphProvider();

			//dynamic data = graphProvider.GetEntityByUIdDynamic(uid);
			//if (data != null && data is JObject)
			//{
			//    var entity = (data as JObject).ToObject<Entity>();
			//    if (entity != null)
			//    {
			//        if (entity.Type == EntityType.File)
			//        {
			//            return FileToResponse((data as JObject).ToObject<File>());

			//        }
			//        else if (entity.Type == EntityType.Symbol)
			//        {
			//            return SymbolToResponse((data as JObject).ToObject<Symbol>());
			//        }
			//    }
			//}
			//throw new HttpResponseException(HttpStatusCode.NotFound);

			throw new NotImplementedException();
		}

		// _apis/arcusgraph/entities/{uid}/references
		[HttpGet]
		public async Task<IList<SymbolReference>> GetReferencesAsync(string uid)
		{
			var result = new List<SymbolReference>();

			if (uid.Contains('@'))
			{
				// Find all method definitions
				var uidparts = uid.Split('@');
				var methodId = uidparts[0];
				var invocationIndex = Convert.ToInt32(uidparts[1]);

				var methodDescriptor = MethodDescriptor.DeMarsall(methodId);
				var methodEntity = await SolutionManager.GetMethodEntityAsync(methodDescriptor);
				var callees = await methodEntity.GetCalleesAsync(invocationIndex);

				foreach (var callee in callees)
				{
					var provider = await SolutionManager.GetProjectCodeProviderAsync(callee);
					var reference = await provider.GetDeclarationInfoAsync(callee);
					//var calleeEntity = await SolutionManager.GetMethodEntityAsync(calleeDescriptor);
					//var reference = await calleeEntity.GetDeclarationInfoAsync();

					if (reference != null)
					{
						ProcessSymbolReference(reference);
						result.Add(reference);
					}
				}
			}
			else
			{
				// Find all method references
				var methodId = uid;
				var methodDescriptor = MethodDescriptor.DeMarsall(methodId);
				var methodEntity = await SolutionManager.GetMethodEntityAsync(methodDescriptor);
				var callers = await methodEntity.GetCallersAsync();

				foreach (var caller in callers)
				{
					var provider = await SolutionManager.GetProjectCodeProviderAsync(caller.Caller);
					var reference = await provider.GetInvocationInfoAsync(caller);

					if (reference != null)
					{
						ProcessSymbolReference(reference);
						result.Add(reference);
					}
				}

				//var callers = await methodEntity.GetCallersDeclarationInfoAsync();

				//foreach (var reference in callers)
				//{
				//	ProcessSymbolReference(reference);
				//	result.Add(reference);
				//}
			}

			return result;
		}

		private static bool FilterFile(FileResponse file)
		{
			// TODO: Hack!!!
			var filename = Path.GetFileName(file.filepath);
			if (filename.StartsWith(".NETFramework,")) return true;

			ProcessFileResponse(file);
			documentsAssemblyName[file.filepath.ToLowerInvariant()] = file.assemblyname;
			return false;
		}

		private static void ProcessFileResponse(FileResponse file)
		{
			//var buildInfo = new BuildInfo();

			file.filepath = FixFilePath(file.filepath);
			file.repository = buildInfo.RepositoryName;
			file.version = buildInfo.VersionName;

			if (file.referenceAnnotation != null)
			{
				foreach (var declaration in file.declarationAnnotation)
				{
					ProcessDeclarationAnnotation(declaration);
				}

				foreach (var reference in file.referenceAnnotation)
				{
					ProcessReferenceAnnotation(reference);
				}
			}
		}

		private static void ProcessAnnotation(Annotation annotation)
		{
			//var buildInfo = new BuildInfo();

			annotation.declAssembly = string.Format("{0}/{1}", buildInfo.RepositoryName, buildInfo.BranchName);
		}

		private static void ProcessDeclarationAnnotation(DeclarationAnnotation declaration)
		{
			ProcessAnnotation(declaration);
		}

		private static void ProcessReferenceAnnotation(ReferenceAnnotation reference)
		{
			ProcessAnnotation(reference);

			reference.declFile = FixFilePath(reference.declFile);
		}

		private static void ProcessSymbolReference(SymbolReference reference)
		{
			//var buildInfo = new BuildInfo();

			reference.preview = FixFilePath(reference.preview);
			reference.tref = string.Format("{0}/{1}/{2}", buildInfo.RepositoryName, buildInfo.BranchName, reference.preview);
		}

		private static string FixFilePath(string filePath)
		{
			if (filePath == null) return null;
			var rootDir = ROOT_DIR + @"Coby\src\";

			if (filePath.StartsWith(rootDir, StringComparison.InvariantCultureIgnoreCase))
			{
				filePath = filePath.Substring(rootDir.Length, filePath.Length - rootDir.Length);
			}

			filePath = filePath.Replace(@"\", "/");
			return filePath;
		}
	}
}
