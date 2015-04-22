// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis.Tachyon;
using Microsoft.CodeAnalysis.Tachyon.DataAdapter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ReachingTypeAnalysis
{
	public class GraphvizGraphDataAdapter : IGraphDataAdapter
	{
		private readonly string outputFile;

		public GraphvizGraphDataAdapter(string outputFile)
		{
			this.outputFile = outputFile;
		}

		public Graph Load() { throw new NotImplementedException(); }

		public void Save(Graph graph)
		{
			using (StreamWriter writer = File.CreateText(outputFile))
			{
				writer.WriteLine("digraph G {");
				writer.WriteLine(@"node [label="""",shape=""point"",style=""filled"",fillcolor=""gray"",width=""0.1""];");
				writer.WriteLine(@"edge [arrowhead=""none""];");

				//                foreach (Vertex v in graph.EnumerateVertices().Where(vertex => (bool)vertex["IsForwarder"]))
				foreach (Vertex v in graph.EnumerateVertices())
				{
					var data = (GraphAnnotationData)v["N"];
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

						if (invoc is CallInfo)
						{
							var callNode = invoc as CallInfo;
							var lhsStr = callNode.LHS == null ? "$" : callNode.LHS.ToString();
							var recStr = callNode.Receiver == null ? "[]" : callNode.Receiver.ToString(); ;
							writer.WriteLine(@"{0} [shape=""circle"",label=""{1}:{2}, lhs={3} rec={4}"",fillcolor=""blue""];", v.Id, callNode.Callee.ToString(), elemsStr, lhsStr, recStr);
						}
						if (invoc is DelegateCallInfo)
						{
							var deleNode = invoc as DelegateCallInfo;
							writer.WriteLine(@"{0} [shape=""circle"",label=""{1}:[{2}]"",fillcolor=""brown""];", v.Id, deleNode.CalleeDelegate.ToString(), elemsStr);

						}
					}


				}

				foreach (Edge e in graph.EnumerateEdges())
				{
					string elemsStr = ElemsToStr((Bag<AnalysisType>)e["Types"]);
					writer.WriteLine("{0}: [{1}];", e, elemsStr);
				}
				writer.WriteLine("}");
			}
		}

		private static string ElemsToStr(Bag<AnalysisType> elems)
		{

			var sb = new StringBuilder();
			if (elems != null)
			{
				foreach (var e in elems.AsSet())
					sb.Append("(" + e + ":" + elems.Occurrences(e) + ")");
			}
			return sb.ToString();
		}
		private static string ElemsToStr(ISet<AnalysisType> elems)
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
