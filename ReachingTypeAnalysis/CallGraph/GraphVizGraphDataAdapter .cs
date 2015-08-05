// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Tachyon;
using Microsoft.CodeAnalysis.Tachyon.DataAdapter;
using ReachingTypeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionTraversal.CallGraph
{
    public class GraphVizGraphDataAdapter : IGraphDataAdapter
    {
        private readonly string _outputFile;

        public GraphVizGraphDataAdapter(string outputFile)
        {
            _outputFile = outputFile;
        }

        public Graph Load()
		{
			throw new NotImplementedException();
		}

        public void Save(Graph graph)
        {
            using (var writer = File.CreateText(_outputFile))
            {
                writer.WriteLine("digraph G {");
                writer.WriteLine(@"node [label="""",shape=""point"",style=""filled"",fillcolor=""gray"",width=""0.1""];");
                writer.WriteLine(@"edge [arrowhead=""none""];");

                foreach (var v in graph.EnumerateVertices())
                {
                    // var method = v["M"] as IMethodSymbol;
                    var method = v["M"] as MethodDescriptor;
                    var dclass = method.ClassName;
                    var name = method.MethodName;
                    //var dclass = method.ContainingType.Name;
                    //var name = method.Name;
                    writer.WriteLine(@"{0} [shape=""box"",label=""{1}"",fillcolor=""gray""];", v.Id, dclass + "." + name);
                }

                foreach (var edge in graph.EnumerateEdges())
                {
                    var locationMethod = (KeyValuePair<LocationDescriptor, MethodDescriptor>)edge["L"];
                    //var locString = locationMethod.Key.Location != null ? locationMethod.Key.Location.GetLineSpan().StartLinePosition.ToString() : "?";
                    var locString = locationMethod.Key.Location.ToString();
                    writer.WriteLine(@"{0} [label=""{1}""];", edge, locString);
                    //var locationMethod = (KeyValuePair<Location, IMethodSymbol>)edge["L"];
                    //writer.WriteLine(@"{0} [label=""{1}""];", edge,locationMethod.Key.GetLineSpan().StartLinePosition);
                }

                writer.WriteLine("}");
            }
        }
    }

}
