using Microsoft.CodeAnalysis.Tachyon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;

namespace ReachingTypeAnalysis
{
	internal class GraphEx<TNodeValue, TEdgeValue> : IGraph<TNodeValue, TEdgeValue>
	{
		private const string DATA_ANNOTATION = "N";

		private Graph graph;

		public GraphEx()
		{
			graph = new Graph();
		}

		public long NodeCount
		{
			get { return graph.VertexCount; }
		}

		public long EdgeCount
		{
			get { return graph.EdgeCount; }
		}

		public IEnumerable<GraphNode<TNodeValue>> Nodes
		{
			get { return from v in graph.EnumerateVertices() select this.GetNode(v); }
		}

		public IEnumerable<GraphEdge<TEdgeValue>> Edges
		{
			get { return from v in graph.EnumerateEdges() select this.GetEdge(v); }
		}

		public void Add(long nodeId, TNodeValue value)
		{
			var vertex = graph.AddVertex(nodeId);
			vertex[DATA_ANNOTATION] = value;
		}

		public void Add(long sourceId, long targetId, TEdgeValue value)
		{
			var edge = graph.AddEdge(sourceId, targetId);
			edge[DATA_ANNOTATION] = value;
		}

		public void Remove(long nodeId)
		{
			graph.RemoveVertex(nodeId);
		}

		public void Remove(long sourceId, long targetId)
		{
			graph.RemoveEdge(sourceId, targetId);
		}

		public bool Contains(long nodeId)
		{
			Vertex vertex;
			return graph.TryGetVertex(nodeId, out vertex);
		}

		public bool Contains(long sourceId, long targetId)
		{
			Edge edge;
			return graph.TryGetEdge(sourceId, targetId, out edge);
		}

		public GraphNode<TNodeValue> GetNode(long nodeId)
		{
			var node = graph.GetVertex(nodeId);
			return this.GetNode(node);
		}

		public GraphEdge<TEdgeValue> GetEdge(long sourceId, long targetId)
		{
			var edge = graph.GetEdge(sourceId, targetId);
			return this.GetEdge(edge);
		}

		public IEnumerable<GraphNode<TNodeValue>> GetSources(long nodeId)
		{
			var vertex = graph.GetVertex(nodeId);
			var edges = graph.EnumerateAdjacencyList(vertex);

			return from e in edges
				   where e.ToVertexId == nodeId
				   select this.GetNode(graph.GetVertex(e.FromVertexId));
		}

		public IEnumerable<GraphNode<TNodeValue>> GetTargets(long nodeId)
		{
			var vertex = graph.GetVertex(nodeId);
			var edges = graph.EnumerateAdjacencyList(vertex);

			return from e in edges
				   where e.FromVertexId == nodeId
				   select this.GetNode(graph.GetVertex(e.ToVertexId));
		}

		public IEnumerable<GraphEdge<TEdgeValue>> GetInEdges(long nodeId)
		{
			var vertex = graph.GetVertex(nodeId);
			var edges = graph.EnumerateAdjacencyList(vertex);

			return from e in edges
				   where e.ToVertexId == nodeId
				   select this.GetEdge(e);
		}

		public IEnumerable<GraphEdge<TEdgeValue>> GetOutEdges(long nodeId)
		{
			var vertex = graph.GetVertex(nodeId);
			var edges = graph.EnumerateAdjacencyList(vertex);

			return from e in edges
				   where e.FromVertexId == nodeId
				   select this.GetEdge(e);
		}

		public override string ToString()
		{
			return string.Format("graph with {0} nodes and {1} edges", this.NodeCount, this.EdgeCount);
		}

		private GraphNode<TNodeValue> GetNode(Vertex vertex)
		{
			var id = vertex.Id;
			var value = (TNodeValue)vertex[DATA_ANNOTATION];
			return new GraphNode<TNodeValue>(id, value);
		}

		private GraphEdge<TEdgeValue> GetEdge(Edge edge)
		{
			var sourceId = edge.FromVertexId;
			var targetId = edge.ToVertexId;
			var value = (TEdgeValue)edge[DATA_ANNOTATION];
			return new GraphEdge<TEdgeValue>(sourceId, targetId, value);
		}
	}

	[Serializable]
	internal class SerializableGraph<TNodeValue, TEdgeValue> : IGraph<TNodeValue, TEdgeValue>, IDeserializationCallback
	{
		private ISet<GraphNode<TNodeValue>> nodes;
		private ISet<GraphEdge<TEdgeValue>> edges;

		[NonSerialized]
		private IDictionary<long, GraphNode<TNodeValue>> nodesById;
		[NonSerialized]
		private IDictionary<long, IDictionary<long, GraphEdge<TEdgeValue>>> sources;
		[NonSerialized]
		private IDictionary<long, IDictionary<long, GraphEdge<TEdgeValue>>> targets;

		public SerializableGraph()
		{
			nodes = new HashSet<GraphNode<TNodeValue>>();
			edges = new HashSet<GraphEdge<TEdgeValue>>();
			nodesById = new Dictionary<long, GraphNode<TNodeValue>>();
			sources = new Dictionary<long, IDictionary<long, GraphEdge<TEdgeValue>>>();
			targets = new Dictionary<long, IDictionary<long, GraphEdge<TEdgeValue>>>();
		}

		public long NodeCount
		{
			get { return nodes.Count; }
		}

		public long EdgeCount
		{
			get { return edges.Count; }
		}

		public IEnumerable<GraphNode<TNodeValue>> Nodes
		{
			get { return nodes; }
		}

		public IEnumerable<GraphEdge<TEdgeValue>> Edges
		{
			get { return edges; }
		}

		public void Add(long nodeId, TNodeValue value)
		{
			if (nodesById.ContainsKey(nodeId))
			{
				throw new Exception("Node already exists");
			}

			var node = new GraphNode<TNodeValue>(nodeId, value);

			nodes.Add(node);
			nodesById.Add(nodeId, node);
			sources.Add(nodeId, new Dictionary<long, GraphEdge<TEdgeValue>>());
			targets.Add(nodeId, new Dictionary<long, GraphEdge<TEdgeValue>>());
		}

		public void Add(long sourceId, long targetId, TEdgeValue value)
		{
			if (!nodesById.ContainsKey(sourceId))
			{
				throw new Exception("Unknown source node");
			}

			if (!nodesById.ContainsKey(targetId))
			{
				throw new Exception("Unknown target node");
			}

			if (targets[sourceId].ContainsKey(targetId))
			{
				throw new Exception("Edge already exists");
			}

			var edge = new GraphEdge<TEdgeValue>(sourceId, targetId, value);

			edges.Add(edge);
			sources[targetId].Add(sourceId, edge);
			targets[sourceId].Add(targetId, edge);
		}

		public void Remove(long nodeId)
		{
			if (!nodesById.ContainsKey(nodeId)) return;

			var node = nodesById[nodeId];

			nodesById.Remove(nodeId);
			nodes.Remove(node);

			edges.ExceptWith(sources[nodeId].Values);
			edges.ExceptWith(targets[nodeId].Values);
		}

		public void Remove(long sourceId, long targetId)
		{
			if (!nodesById.ContainsKey(sourceId)) return;
			if (!nodesById.ContainsKey(targetId)) return;

			if (!targets[sourceId].ContainsKey(targetId)) return;
			var edge = targets[sourceId][targetId];

			edges.Remove(edge);
			sources[targetId].Remove(sourceId);
			targets[sourceId].Remove(targetId);			
		}

		public bool Contains(long nodeId)
		{
			return nodesById.ContainsKey(nodeId);
		}

		public bool Contains(long sourceId, long targetId)
		{
			if (!nodesById.ContainsKey(sourceId)) return false;
			if (!nodesById.ContainsKey(targetId)) return false;

			var result = targets[sourceId].ContainsKey(targetId);
			return result;
		}

		public GraphNode<TNodeValue> GetNode(long nodeId)
		{
			if (!nodesById.ContainsKey(nodeId))
			{
				throw new Exception("Unknown node");
			}

			return nodesById[nodeId];
		}

		public GraphEdge<TEdgeValue> GetEdge(long sourceId, long targetId)
		{
			if (!nodesById.ContainsKey(sourceId))
			{
				throw new Exception("Unknown source node");
			}

			if (!nodesById.ContainsKey(targetId))
			{
				throw new Exception("Unknown target node");
			}

			if (!targets[sourceId].ContainsKey(targetId))
			{
				throw new Exception("Unknown edge");
			}

			var edge = targets[sourceId][targetId];
			return edge;
		}

		public IEnumerable<GraphNode<TNodeValue>> GetSources(long nodeId)
		{
			if (!nodesById.ContainsKey(nodeId))
			{
				throw new Exception("Unknown node");
			}

			return from sourceId in sources[nodeId].Keys
				   select nodesById[sourceId];
		}

		public IEnumerable<GraphNode<TNodeValue>> GetTargets(long nodeId)
		{
			if (!nodesById.ContainsKey(nodeId))
			{
				throw new Exception("Unknown node");
			}

			return from targetId in targets[nodeId].Keys
				   select nodesById[targetId];
		}

		public IEnumerable<GraphEdge<TEdgeValue>> GetInEdges(long nodeId)
		{
			if (!nodesById.ContainsKey(nodeId))
			{
				throw new Exception("Unknown node");
			}

			return sources[nodeId].Values;
		}

		public IEnumerable<GraphEdge<TEdgeValue>> GetOutEdges(long nodeId)
		{
			if (!nodesById.ContainsKey(nodeId))
			{
				throw new Exception("Unknown node");
			}

			return targets[nodeId].Values;
		}

		public override string ToString()
		{
			return string.Format("graph with {0} nodes and {1} edges", this.NodeCount, this.EdgeCount);
		}

		#region IDeserializationCallback

		//[OnDeserialized]
		//public void OnDeserialized(StreamingContext context)
		//{
		//	// We need to reconstruct the node mappings.
		//	// The nodes and edges fields should be already initialized.
		//	nodesById = new Dictionary<long, GraphNode<TNodeValue>>();
		//	sources = new Dictionary<long, IDictionary<long, GraphEdge<TEdgeValue>>>();
		//	targets = new Dictionary<long, IDictionary<long, GraphEdge<TEdgeValue>>>();

		//	foreach (var node in nodes)
		//	{
		//		nodesById.Add(node.Id, node);
		//		sources.Add(node.Id, new Dictionary<long, GraphEdge<TEdgeValue>>());
		//		targets.Add(node.Id, new Dictionary<long, GraphEdge<TEdgeValue>>());
		//	}

		//	foreach (var edge in edges)
		//	{
		//		sources[edge.TargetId].Add(edge.SourceId, edge);
		//		targets[edge.SourceId].Add(edge.TargetId, edge);
		//	}
		//}

		void IDeserializationCallback.OnDeserialization(object sender)
		{
			// We need to reconstruct the node mappings.
			// The nodes and edges fields should be already initialized.
			nodesById = new Dictionary<long, GraphNode<TNodeValue>>();
			sources = new Dictionary<long, IDictionary<long, GraphEdge<TEdgeValue>>>();
			targets = new Dictionary<long, IDictionary<long, GraphEdge<TEdgeValue>>>();

			foreach (var node in nodes)
			{
				nodesById.Add(node.Id, node);
				sources.Add(node.Id, new Dictionary<long, GraphEdge<TEdgeValue>>());
				targets.Add(node.Id, new Dictionary<long, GraphEdge<TEdgeValue>>());
			}

			foreach (var edge in edges)
			{
				sources[edge.TargetId].Add(edge.SourceId, edge);
				targets[edge.SourceId].Add(edge.TargetId, edge);
			}
		}

		#endregion
	}
}
