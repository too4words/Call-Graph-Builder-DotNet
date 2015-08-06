using ConsoleServer.Utils;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleServer.Model
{
    [DataContract]
    public class CodeGraph
    {
        [DataMember]
        public List<Vertex> Vertices { get; set; }
    }
}
