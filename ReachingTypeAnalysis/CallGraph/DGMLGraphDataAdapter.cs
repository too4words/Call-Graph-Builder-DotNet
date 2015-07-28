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
using System.Xml;

namespace SolutionTraversal.Callgraph
{
    public class DGMLGraphDataAdapter : IGraphDataAdapter
    {
        private readonly string _outputFile;

        public DGMLGraphDataAdapter(string outputFile)
        {
            _outputFile = outputFile;
        }

        public Graph Load()
		{
			throw new NotImplementedException();
		}

        public void Save(Graph graph)
        {
            using (var writer = new XmlTextWriter(_outputFile, Encoding.UTF8))
            {
				writer.WriteStartElement("DirectedGraph", @"http://schemas.microsoft.com/vs/2009/dgml");
				writer.WriteStartElement("Nodes");

                foreach (var v in graph.EnumerateVertices())
                {
                    var method = v["M"] as MethodDescriptor;
					var id = Convert.ToString(v.Id);
					var label = method.Name;

					writer.WriteStartElement("Node");
					writer.WriteAttributeString("Id", id);
					writer.WriteAttributeString("Label", label);
					writer.WriteEndElement();
                }

				writer.WriteEndElement();
				writer.WriteStartElement("Links");

                foreach (var edge in graph.EnumerateEdges())
                {
                    var locationMethod = (KeyValuePair<LocationDescriptor, MethodDescriptor>)edge["L"];
					var source = Convert.ToString(edge.FromVertexId);
					var target = Convert.ToString(edge.ToVertexId);
                    var label = locationMethod.Key.Location.ToString();

					writer.WriteStartElement("Link");
					writer.WriteAttributeString("Source", source);
					writer.WriteAttributeString("Target", target);
					writer.WriteAttributeString("Label", label);
					writer.WriteEndElement();
                }

				writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }
    }
}
