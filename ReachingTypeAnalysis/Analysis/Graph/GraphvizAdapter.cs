// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis.Tachyon;
using Microsoft.CodeAnalysis.Tachyon.DataAdapter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ReachingTypeAnalysis
{
	internal class GraphvizGraphDataAdapter
	{
		private readonly string outputFile;

		public GraphvizGraphDataAdapter(string outputFile)
		{
			this.outputFile = outputFile;
		}

		public void Save(IGraph<GraphNodeAnnotationData, GraphEdgeAnnotationData> graph)
		{
			using (var writer = File.CreateText(outputFile))
			{
				writer.WriteLine("digraph G {");
				writer.WriteLine(@"node [label="""",shape=""point"",style=""filled"",fillcolor=""gray"",width=""0.1""];");
				writer.WriteLine(@"edge [arrowhead=""none""];");

				//                foreach (Vertex v in graph.EnumerateVertices().Where(vertex => (bool)vertex["IsForwarder"]))
				foreach (var v in graph.Nodes)
				{
					var data = v.Value;
					var node = data.Node;
					var elems = data.Elems;
					var invoc = data.CallNode;
					var deletions = data.DeletedElems;
					//var node = (N)v["N"] ;
					//var elems = v["Elems"] as ISet<T>;
					//var invoc = v["Call"];



					string elemsStr = ElemsToStr(elems);
					string deletionsStr = ElemsToStr(deletions);

					if (invoc == null)
					{
						var nodeStr = node.ToString();
						writer.WriteLine(@"{0} [shape=""box"",label=""{1}:[{2}]-[{3}]"",fillcolor=""gray""];", v.Id, nodeStr, elemsStr, deletionsStr);
					}
					else
					{

						if (invoc is MethodCallInfo)
						{
							var callNode = invoc as MethodCallInfo;
							var lhsStr = callNode.LHS == null ? "$" : callNode.LHS.ToString();
							var recStr = callNode.Receiver == null ? "[]" : callNode.Receiver.ToString(); ;
							writer.WriteLine(@"{0} [shape=""circle"",label=""{1}:{2}, lhs={3} rec={4}"",fillcolor=""blue""];", v.Id, callNode.Method.ToString(), elemsStr, lhsStr, recStr);
						}
						if (invoc is DelegateCallInfo)
						{
							var deleNode = invoc as DelegateCallInfo;
							writer.WriteLine(@"{0} [shape=""circle"",label=""{1}:[{2}]"",fillcolor=""brown""];", v.Id, deleNode.Delegate.ToString(), elemsStr);

						}
					}


				}

				foreach (var e in graph.Edges)
				{
//					string elemsStr = ElemsToStr((Bag<TypeDescriptor>)e["Types"]);
                    string elemsStr = ElemsToStr(e.Value.Types);

					writer.WriteLine("{0}: [{1}];", e, elemsStr);
				}
				writer.WriteLine("}");
			}
		}

		private static string ElemsToStr(Bag<TypeDescriptor> elems)
		{

			var sb = new StringBuilder();
			if (elems != null)
			{
				foreach (var e in elems.AsSet())
					sb.Append("(" + e + ":" + elems.Occurrences(e) + ")");
			}
			return sb.ToString();
		}
		private static string ElemsToStr(ISet<TypeDescriptor> elems)
		{

			var sb = new StringBuilder();
			if (elems != null)
			{
				foreach (var e in elems)
					sb.Append(e + " ");
			}
			return sb.ToString();
		}
	}
}
