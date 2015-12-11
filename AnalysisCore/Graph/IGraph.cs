using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis
{
	[Serializable]
	internal class GraphNode<TValue>
	{
		public long Id { get; private set; }
		public TValue Value { get; private set; }

		public GraphNode(long id, TValue value)
		{
			this.Id = id;
			this.Value = value;
		}

		public override int GetHashCode()
		{
			return this.Id.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var node = obj as GraphNode<TValue>;
			return this.Id == node.Id;
		}

		public override string ToString()
		{
			return string.Format("node {0}: {1}", this.Id, this.Value);
		}
	}

	[Serializable]
	internal class GraphEdge<TValue>
	{
		public long SourceId { get; private set; }
		public long TargetId { get; private set; }
		public TValue Value { get; private set; }

		public GraphEdge(long sourceId, long targetId, TValue value)
		{
			this.SourceId = sourceId;
			this.TargetId = targetId;
			this.Value = value;
		}

		public override int GetHashCode()
		{
			return this.SourceId.GetHashCode() ^ this.TargetId.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var edge = obj as GraphEdge<TValue>;
			return this.SourceId == edge.SourceId && this.TargetId == edge.TargetId;
		}

		public override string ToString()
		{
			return string.Format("edge {0} -> {1}: {2}", this.SourceId, this.TargetId, this.Value);
		}
	}

	internal interface IGraph<TNodeValue, TEdgeValue>
	{
		long NodeCount { get; }
		long EdgeCount { get; }
		IEnumerable<GraphNode<TNodeValue>> Nodes { get; }
		IEnumerable<GraphEdge<TEdgeValue>> Edges { get; }

		void Add(long nodeId, TNodeValue value);
		void Add(long sourceId, long targetId, TEdgeValue value);
		void Remove(long nodeId);
		void Remove(long sourceId, long targetId);

		bool Contains(long nodeId);
		bool Contains(long sourceId, long targetId);

		GraphNode<TNodeValue> GetNode(long nodeId);
		GraphEdge<TEdgeValue> GetEdge(long sourceId, long targetId);
		
		IEnumerable<GraphNode<TNodeValue>> GetSources(long nodeId);
		IEnumerable<GraphNode<TNodeValue>> GetTargets(long nodeId);

		IEnumerable<GraphEdge<TEdgeValue>> GetInEdges(long nodeId);
		IEnumerable<GraphEdge<TEdgeValue>> GetOutEdges(long nodeId);
	}
}
