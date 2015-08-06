using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleServer.Utils
{
    public class HttpUtils
    {
        internal static string ReadFileEntity(string filePath)
        {
            string query = string.Format("GraphEntities?entityType=File&filepath={0}&{1}", filePath, ReposVersion);

            return SendHttpRequest(query);            
        }

        internal static string ReadSymbolReferences(string uid)
        {
            string query = string.Format("GraphReferences/{0}", uid);

            return SendHttpRequest(query);      
        }

        private static string SendHttpRequest(string query)
        {
            // Create HttpCient and make a request to api/values 
            HttpClient client = new HttpClient();

            var response = client.GetAsync(BaseAddress + query).Result;

            return response.Content.ReadAsStringAsync().Result;
        }

        public const string BaseAddress = "http://indexservices.cloudapp.net/api/";

        private const string ReposVersion = "repository=dotnet-roslyn&version=master";
    }
}
