using CodeGraphModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CodeGraphModel
{
    [DataContract]
    public class CodeGraph
    {
        [DataMember]
        public List<Vertex> Vertices { get; set; }
    }
}
