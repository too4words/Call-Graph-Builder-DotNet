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

	/// <summary>
	/// Controller to handle all REST calls against graph entities
	/// </summary>
	/// 
	public class OrleansController : ApiController
    {
		[HttpGet]
		public async Task<string> RunTest(string testName, int machines, int numberOfMethods)
		{
			var result = string.Empty;

			try
			{
                var analysisClient = new AnalysisClient(machines, numberOfMethods, testName);

				//var stopWatch = Stopwatch.StartNew();
				var results = await analysisClient.AnalyzeTestAsync();

				//stopWatch.Stop();
				result = string.Format("Ready for queries. Time: {0} ms", results.ElapsedTime);

			}
			catch (Exception exc)
			{
				while (exc is AggregateException) exc = exc.InnerException;
				result = "Error connecting to Orleans: " + exc + " at " + DateTime.Now;
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

			throw new NotImplementedException();
        }

        /// <summary>
        /// _apis/arcusgraph/entities?entitytype={string}
        /// </summary>
        /// <param name="entityType">type of entity</param>
        /// <returns>a list of entities that matches the given type</returns>
        [HttpGet]
        public IList<object> GetEntities(string entityType)
        {
			//if (entityType == EntityType.File || entityType == EntityType.Symbol)
			//{
			//    var graphProvider = ArcusGraphService.CreateGraphProvider();

			//    if (entityType == EntityType.File)
			//    {
			//        return graphProvider.GetEntitiesByType<File>(entityType).Select(e => FileToResponse(e)).Cast<object>().ToList();
			//    }
			//    else if (entityType == EntityType.Symbol)
			//    {
			//        return graphProvider.GetEntitiesByType<Symbol>(entityType).Select(e => SymbolToResponse(e)).Cast<object>().ToList();
			//    }
			//}

			//throw new HttpResponseException(HttpStatusCode.NotFound);

			throw new NotImplementedException();
		}

        /// <summary>
        /// Handle REST request for _apis/arcusgraph/entities?entitytype={string}&filepath={string}&repository={string}&version={string}.
        /// </summary>
        /// <param name="entityType">type of entity (has to be file type)</param>
        /// <param name="filepath">file path of the file entity</param>
        /// <param name="version">git version of file entity (by default is the git branch name)</param>
        /// <param name="repository">repository of the file entity</param>
        /// <returns>file entity that matches the filtering parameters</returns>
        [HttpGet]
        public IList<FileResponse> GetFilesWithFilter(string entityType, string filepath, string version, string repository)
        {
			//if (entityType != EntityType.File)
			//{
			//    throw new HttpResponseException(HttpStatusCode.BadRequest);
			//}

			//var graphProvider = ArcusGraphService.CreateGraphProvider();

			//var filter = new Dictionary<string, object>
			//{
			//    { "GitBranch", version },
			//    { "FilePath", filepath.ToLower() },
			//    { "GitRepository", repository }
			//};

			//var files = graphProvider.GetEntitiesByFilters<File>(EntityType.File, filter);
			//if (!files.Any())
			//{
			//    throw new HttpResponseException(HttpStatusCode.NotFound);
			//}

			//return files.Select(f => FileToResponse(f)).ToList();

			throw new NotImplementedException();
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
    }
}
