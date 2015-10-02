// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis.Tachyon;
using Microsoft.CodeAnalysis.Tachyon.AlgorithmExtensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SolutionTraversal.CallGraph
{
	public class CallGraph<M, L>
	{
		private Graph graph;
		private Graph invertedGraph = null;
		private IDictionary<M, int> vertexIndex;
		private ISet<M> roots = new HashSet<M>();

		public CallGraph()
		{
			this.graph = new Graph();
			this.vertexIndex = new Dictionary<M, int>();
		}

        public void Compress()
        {
            this.graph.Compress();
        }

		public void Add(M method)
		{
			int index;
			Vertex vertex;

			if (!this.vertexIndex.TryGetValue(method, out index))
			{
				var c = this.vertexIndex.Count();
				this.vertexIndex[method] = c;
				vertex = graph.AddVertex(c);
				vertex["M"] = method;
			}
			//if (!graph.TryGetVertex(m, out v))
			//{
			//    graph.AddVertex(m);
			//}
		}

		public void AddCall(M method1, M method2)
		{
			this.Add(method1);
			this.Add(method2);
			this.AddCallEdge(method1, method2);
		}

		public void AddCallAtLocation(L location, M method1, M method2)
		{
			this.Add(method1);
			this.Add(method2);

			var edge = this.AddCallEdge(method1, method2);
			edge["L"] = new KeyValuePair<L, M>(location, method2);
		}

		private Edge AddCallEdge(M method1, M method2)
		{
			//Edge e = new Edge(vIndex[m1], vIndex[m2]);
			//graph.AddEdge(e);
			var e = this.graph.AddEdge(this.vertexIndex[method1], this.vertexIndex[method2]);
			e["E"] = new KeyValuePair<M, M>(method1, method2);

			return e;
		}

		public IEnumerable<M> GetNodes()
		{
			foreach (var vertex in this.graph.EnumerateVertices())
			{
				yield return (M)vertex["M"];
			}
		}

		public IEnumerable<KeyValuePair<M, M>> GetEdges()
		{
			foreach (var edge in this.graph.EnumerateEdges())
			{
				yield return (KeyValuePair<M, M>)edge["E"];
			}
		}

		public IEnumerable<M> GetCallees(M m)
		{
			var result = new HashSet<M>();
			int index = this.vertexIndex[m];
			var v = this.graph.GetVertex(index);

			// Vertex v = graph.GetVertex(m);

			foreach (Vertex v2 in this.graph.EnumerateAdjacentVertices(v))
			{
				// result.Add((M)v2["M"]);
				yield return (M)v2["M"];
			}

			// return result;
		}

		public ISet<M> GetCallees(M m, L location)
		{
			var result = new HashSet<M>();
			int index;
			if (this.vertexIndex.TryGetValue(m, out index))
			{
				Vertex v = this.graph.GetVertex(index);
				foreach (Edge edge in this.graph.EnumerateAdjacencyList(v))	// Doesn't work? .EnumerateAdjacencyList(v))
				{
					// var edge = graph.GetEdge(v, v2);
					var locationMethod = (KeyValuePair<L, M>)edge["L"];
					if (locationMethod.Key.Equals(location))
					{
						result.Add(locationMethod.Value);
					}
				}
			}
			return result;
		}

		public ISet<KeyValuePair<L, M>> GetCallers(M m)
		{
			if (invertedGraph == null)
			{
				this.invertedGraph = graph.ReverseEdgeDirections();
			}

			var result = new HashSet<KeyValuePair<L, M>>();
			int index;
			if (vertexIndex.TryGetValue(m, out index))
			{
				Vertex v = this.invertedGraph.GetVertex(index);
				// wait : that one didn't work well for me... try... it gave me an empty list
				foreach (Edge edge in this.invertedGraph.EnumerateAdjacencyList(v))
				{
					var locationMethod = (KeyValuePair<L, M>)edge["L"];
					var callerVertex = this.invertedGraph.GetVertex(edge.ToVertexId);
					var caller = (M)callerVertex["M"];
					result.Add(new KeyValuePair<L, M>(locationMethod.Key, caller));
				}
			}
			return result;
		}

		public void AddRootMethod(M method)
		{
			//// TODO: remove this assert, it is just for debugging
			//if (method.ToString().Contains("CommandLine.Program.Main"))
			//{
			//	System.Diagnostics.Debug.Assert(false, "AddRootMethod");
			//}

			roots.Add(method);
			this.Add(method);
		}

		public void AddRootMethods(IEnumerable<M> methods)
		{
			//// TODO: remove this assert, it is just for debugging
			//var test = methods.ToList();

			//if (test.Any(m => m.ToString().Contains("CommandLine.Program.Main")))
			//{
			//	System.Diagnostics.Debug.Assert(false, "AddRootMethods");
			//}

			//roots.UnionWith(methods);

			foreach (var method in methods)
			{
				this.AddRootMethod(method);
			}
		}

		public ISet<M> GetReachableMethods()
		{
			var result = new HashSet<M>();

			foreach (var root in roots)
			{
				var reachableMethods = GetReachableMethods(root);
                result.UnionWith(reachableMethods);
			}

			return result;
		}

		public IEnumerable<M> GetReachableMethods(M method)
		{
			//// TODO: remove this assert, it is just for debugging
			//System.Diagnostics.Debug.Assert(this.vertexIndex.ContainsKey(method), method.ToString());

			var index = this.vertexIndex[method];
			var vertex = this.graph.GetVertex(index);

			return this.graph.EnumerateVerticesReachableBy(vertex).Select(v1 => (M)v1["M"]);
		}

		public void Save(string path)
		{			
			//var adapter = new GraphVizGraphDataAdapter(path);
			var adapter = new DGMLGraphDataAdapter(path);
			this.graph.Save(adapter);
		}

		internal void Clear()
		{
			this.graph = new Graph();
			this.invertedGraph = null;
			this.vertexIndex = new Dictionary<M, int>();
			this.roots.Clear();
		}
	}
}
