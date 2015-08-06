using ConsoleServer.Model;
using ConsoleServer.Utils;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace ConsoleServer.Controllers
{
	// http://localhost:14054/index.html?graph=BasicTest&mode=orleans
	public class OrleansController: ApiController
    {
        /// <summary>
        /// Get All Files in the graph (Abbreviated)
        /// </summary>
        [Route("api/orleans/{graph}/files")]
        public IEnumerable<FileResponse> GetAllFiles(string graph)
        {
            using (TimedLog.Time(graph + " :: Get All Files"))
            {
				throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Get Full Files matching specified file path
        /// </summary>
        [Route("api/orleans/{graph}/entities/file/{*filePath}")]
        public IEnumerable<FileResponse> GetFileEntities(string graph, string filePath)
        {
            using (TimedLog.Time(graph + " :: Get File"))
            {
				throw new NotImplementedException();
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
				throw new NotImplementedException();
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
				throw new NotImplementedException();
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
				throw new NotImplementedException();
			}
        }
    }
}
