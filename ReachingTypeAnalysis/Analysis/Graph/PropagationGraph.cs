﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis.Tachyon;
using Microsoft.CodeAnalysis.Tachyon.DataAdapter;
using ReachingTypeAnalysis.Communication;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace ReachingTypeAnalysis
{
	/// <summary>
	/// Propagation graph: is the main data structure of the algorithm
	/// The basic idea is that concrete types flow from the graph nodes which represent proggram expressions (variables, fields, invocaitons)
	/// The invocations are dummy nodes. Their role is to trigger the propagation of data to the callees. When a type reach an invocation 
	/// it is marked to be processesd later
	/// </summary>
	/// <typeparam name="N"></typeparam>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="M"></typeparam>
	internal class PropagationGraph
	{
		/// <summary>
		/// The work list used during the propagation
		/// </summary>
		private ISet<AnalysisNode> workList = new HashSet<AnalysisNode>();
		//private IImmutableSet<N> workList = ImmutableHashSet<N>.Empty;

		/// <summary>
		/// Simirlar to the worklist but for the propagation of a removal of a concrete type
		/// </summary>
		private ISet<AnalysisNode> deletionWorkList = new HashSet<AnalysisNode>();
		/// <summary>
		/// The graph itself. We use Nuri Yeralan library (Tachyon)
		/// </summary>
		private Graph graph;
		/// <summary>
		/// A map to relate vertices with expressions in the program
		/// </summary>
		private IDictionary<AnalysisNode, int> vIndex;
		/// <summary>
		/// This is the set of invocations made by the method
		/// </summary>
		private ISet<AnalysisNode> callNodes;

		public IEnumerable<AnalysisNode> CallNodes
		{
			get { return callNodes; }
			//private set { callNodes = value; }
		}

		internal PropagationGraph()
		{
			graph = new Graph();
			vIndex = new Dictionary<AnalysisNode, int>();
			callNodes = new HashSet<AnalysisNode>();
		}
		private ICollection<AnalysisNode> Nodes
		{
			get { return vIndex.Keys; }
		}

		private Vertex AddVertex(AnalysisNode m)
		{
			int index;
			Vertex v = null;
			if (!vIndex.TryGetValue(m, out index))
			{
				int c = vIndex.Count();
				vIndex[m] = c;
				v = graph.AddVertex(c);
				var data = new GraphAnnotationData(m);
				v["N"] = data;
			}
			else v = graph.GetVertex(index);
			return v;
		}
		private Vertex AddVertex(AnalysisNode m, AnalysisInvocationExpession callNode)
		{
			Contract.Assert(callNode != null);
			Vertex v = AddVertex(m);
			if (callNode != null)
			{
				var data = GetData(v);
				data.CallNode = callNode;
			}

			return v;
		}

		private GraphAnnotationData GetData(Vertex v)
		{
			return (GraphAnnotationData)v["N"];
		}

		public void Add(AnalysisNode n)
		{
			Vertex v = AddVertex(n);
		}

		internal void Add(AnalysisNode n, AnalysisType t)
		{
			Vertex v = AddVertex(n);
			var data = GetData(v);
			data.Elems.Add(t);
			//((ISet<T>)v["Elems"]).Add(t);
		}

		public void AddDelegate(AnalysisNode n, AnalysisMethod m)
		{
			Vertex v = AddVertex(n);
			var data = GetData(v);
			if (data.Delegates == null)
			{
				data.Delegates = new HashSet<AnalysisMethod>();
			}
			data.Delegates.Add(m);
		}

		public void AddCall(AnalysisInvocationExpession call, AnalysisNode callNode)
		{
			Vertex v = AddVertex(callNode, call);
			callNodes.Add(callNode);
			AddToWorkList(callNode);
		}

		//public void AddDelegateCall(DelegateCallExp<M, T, N> call, N callNode)
		//{
		//    Vertex v = addV(callNode, call);
		//    callNodes.Add(callNode);
		//}

		internal void AddRet(AnalysisNode rv)
		{
			Vertex v = AddVertex(rv);
			//v["Ret"] = true;
			graph.AddVertex(v);
			var data = GetData(v);
			data.HasRetValue = true;
		}

		public AnalysisInvocationExpession GetInvocationInfo(AnalysisNode callNode)
		{
			int index;
			if (vIndex.TryGetValue(callNode, out index))
			{
				Vertex v = graph.GetVertex(index);
				//return (AInvocationExp<M, T, N>)v["Call"];
				return GetData(v).CallNode;
			}
			return null;
		}
		public bool IsCallNode(AnalysisNode n)
		{
			int index = vIndex[n];
			Vertex v = graph.GetVertex(index);
			//return v["Call"] != null && v["Call"] is CallExp<M, T, N>;
			return GetData(v).CallNode != null && GetData(v).CallNode is CallInfo;
		}
		bool IsRetNode(AnalysisNode n)
		{
			int index = vIndex[n];
			Vertex v = graph.GetVertex(index);
			//return v["Ret"] != null;
			return GetData(v).HasRetValue;
		}

		public bool IsDelegateCallNode(AnalysisNode n)
		{
			int index = vIndex[n];
			Vertex v = graph.GetVertex(index);
			// IMPROVE 
			//return v["Call"] != null && v["Call"] is DelegateCallExp<M,T,N>;
			return GetData(v).CallNode != null && GetData(v).CallNode is DelegateCallInfo;

		}

		internal void Add(AnalysisNode n, IEnumerable<AnalysisType> ts)
		{
			Vertex v = AddVertex(n);
			GetData(v).Elems.UnionWith(ts);
			//((ISet<T>)v["Elems"]).UnionWith(ts);
		}

		internal void RemoveTypes(AnalysisNode n, IEnumerable<AnalysisType> ts)
		{
			Vertex v = AddVertex(n);
			GetData(v).DeletedElems.UnionWith(ts);
			//((ISet<T>)v["DeletedElems"]).UnionWith(ts);
		}

		AnalysisNode GetAnalysisNode(Vertex v)
		{
			return GetData(v).Node;
			//return (N)v["N"];
		}
		public void AddEdge(AnalysisNode n1, AnalysisNode n2)
		{
			Add(n1);
			Add(n2);
			// Edge e = new Edge(vIndex[n1], vIndex[n2]);
			// graph.AddEdge(e);
			var e = graph.AddEdge(vIndex[n1], vIndex[n2]);
			e["Types"] = new HashSet<AnalysisType>();
		}

		internal ISet<AnalysisType> TypesInEdge(AnalysisNode n1, AnalysisNode n2)
		{
			int iN1, iN2;
			if (vIndex.TryGetValue(n1, out iN1) && vIndex.TryGetValue(n2, out iN2))
			{
				var e = graph.GetEdge(vIndex[n1], vIndex[n2]);
				var edges = (ISet<AnalysisType>)e["Types"];
				return edges;
			}
			return new HashSet<AnalysisType>();
		}

		public void ReplaceNode(AnalysisNode nOld, AnalysisNode nNew)
		{
			int index;
			if (vIndex.TryGetValue(nOld, out index))
			{
				var v = graph.GetVertex(index);
				GetData(v).Node = nNew;
				vIndex[nNew] = index;
				vIndex.Remove(nOld);
			}
		}

		internal Bag<AnalysisType> GetTypesMS(AnalysisNode m)
		{
			var result = new Bag<AnalysisType>();
			int index;
			if (vIndex.TryGetValue(m, out index))
			{
				Vertex v = graph.GetVertex(index);
				//return v != null ? (ISet<T>)v["Elems"] : res;
				return v != null ? GetData(v).Elems : result;
			}
			return result;
		}

		internal ISet<AnalysisType> GetTypes(AnalysisNode m)
		{
			var result = new HashSet<AnalysisType>();
			int index;
			if (vIndex.TryGetValue(m, out index))
			{
				Vertex v = graph.GetVertex(index);
				//return v != null ? (ISet<T>)v["Elems"] : res;
				return v != null ? GetData(v).Elems.AsSet() : result;
			}
			return result;
		}

		public ISet<AnalysisMethod> GetDelegates(AnalysisNode m)
		{
			var result = new HashSet<AnalysisMethod>();
			int index;
			if (vIndex.TryGetValue(m, out index))
			{
				Vertex v = graph.GetVertex(index);
				return v != null ? GetData(v).Delegates : result;
			}
			return result;
		}

		internal ISet<AnalysisType> GetDeletedTypes(AnalysisNode m)
		{
			var res = new HashSet<AnalysisType>();
			int index;
			if (vIndex.TryGetValue(m, out index))
			{
				Vertex v = graph.GetVertex(index);
				return v != null ? GetData(v).DeletedElems : res;
			}
			return res;
		}
		//public bool DiffProp(IEnumerable<T> src, N n)
		//{
		//    var ts = GetTypes(n);
		//    int c = ts.Count;
		//    ts.UnionWith(src.Where(t => !ts.Contains(t)));
		//    if (ts.Count > c)
		//    {
		//        this.workList.Add(n);
		//        return true;
		//    }
		//    return false;
		//}
		internal bool DiffProp(IEnumerable<AnalysisType> src, AnalysisNode n, PropagationKind propKind)
		{
			if (propKind == PropagationKind.REMOVE_TYPES || propKind == PropagationKind.REMOVE_ASSIGNMENT)
				return DiffDelProp(src, n);
			return DiffProp(src, n);
		}

		internal bool DiffProp(IEnumerable<AnalysisType> src, AnalysisNode n)
		{
			var ts = GetTypesMS(n);
			int c = ts.Count;
			ts.UnionWith(src.Where(t => !ts.Contains(t) && IsAssignable(t, n)));
			if (ts.Count > c)
			{
				this.AddToWorkList(n);
				return true;
			}
			return false;
		}

		internal bool IsAssignable(AnalysisType t1, AnalysisNode n)
		{
			var res = true;
			// Ugly
			AnalysisType type1 = (AnalysisType)t1;
			AnalysisNode anode = (AnalysisNode)n;

			var type2 = anode.AnalysisType;

			if (!type1.IsSubtype(type2))
				if (!type2.IsDelegate)
					if (!IsCallNode(n) && !IsDelegateCallNode(n))
						return false;
			//foreach(var t2 in ts.AsSet())
			//{
			//    AnalysisType type2 = (AnalysisType)t2;
			//    if (!type1.IsSubtype(type2))
			//        if (!IsCallNode(n) && !IsDelegateCallNode(n))
			//            return false;
			//}
			return res;
		}

		internal bool DiffDelProp(IEnumerable<AnalysisType> src, AnalysisNode n)
		{
			var delTypes = GetDeletedTypes(n);
			if (delTypes.IsSupersetOf(src))
				return false;
			var ts = GetTypesMS(n);
			int c = ts.Count;
			var removed = ts.ExceptWith(src);
			if (removed.Count() > 0)
			{
				this.AddToDeletionWorkList(n);
				// It should be the 
				this.RemoveTypes(n, removed);
				return true;
			}
			return false;
		}

		public bool DiffPropDelegates(IEnumerable<AnalysisMethod> src, AnalysisNode n)
		{
			var ts = GetDelegates(n);
			int c = ts.Count;
			ts.UnionWith(src.Where(t => !ts.Contains(t)));
			if (ts.Count > c)
			{
				this.AddToWorkList(n);
				return true;
			}
			return false;
		}

		public void RemoveDeletedTypes()
		{
			foreach (var n in this.Nodes)
			{
				this.GetDeletedTypes((AnalysisNode)n).Clear();
			}
		}


		#region Deprecated
		//public bool PropagateOneNode(N n)
		//{
		//    bool someNodeChanged = false;
		//    workList.Add(n);
		//    while(workList.Count>0)
		//    {
		//        N  n1 = workList.First();
		//        workList.Remove(n1);
		//        someNodeChanged= PropagateOneNodeToAdj(n);
		//    }
		//    return someNodeChanged;
		//}

		//public bool PropagateNodes()
		//{

		//    bool someNodeChanged = false;
		//    ISet<N> workList = new HashSet<N>();
		//    foreach(var n in Nodes)
		//    {
		//        someNodeChanged= PropagateOneNode(n);
		//    }
		//    //workList.Add(n);
		//    //while (workList.Count > 0)
		//    //{
		//    //    N n1 = workList.First();
		//    //    workList.Remove(n1);
		//    //    someNodeChanged = PropagateOneNode(n, workList);
		//    //}
		//    return someNodeChanged;
		//}
		#endregion

		private Vertex GetVertex(AnalysisNode n)
		{
			return graph.GetVertex(vIndex[n]);
		}

		public void AddToWorkList(AnalysisNode n)
		{
			workList.Add(n);
			//workList = workList.Add(n);
		}

		public void RemoveFromWorkList(AnalysisNode n)
		{
			workList.Remove(n);
			//workList = workList.Remove(n);
		}

		public void AddToDeletionWorkList(AnalysisNode n)
		{
			deletionWorkList.Add(n);
		}

		internal PropagationEffects Propagate()
		{
			var calls = new HashSet<AnalysisInvocationExpession>();
			bool retModified = false;

			while (workList.Count > 0)
			{
				var n = workList.First();
				this.RemoveFromWorkList(n);
				if (IsCallNode(n) || IsDelegateCallNode(n))
				{
					calls.Add(GetInvocationInfo(n));
					continue;
				}
				if (IsRetNode(n))
				{
					retModified = true;
				}

				var v = GetVertex(n);

				var types = GetTypes(n);

				foreach (var v1 in graph.EnumerateAdjacentVertices(v))
				{
					var n1 = GetAnalysisNode(v1);

					DiffProp(types, n1);

					var e = graph.GetEdge(v, v1);
					e["Types"] = types;

					DiffPropDelegates(GetDelegates(n), n1);
				}
			}
			HasBeenPropagated = true;
			return new PropagationEffects(calls, retModified);
		}

		internal PropagationEffects PropagateDeletionOfNodes()
		{
			var calls = new HashSet<AnalysisInvocationExpession>();
			bool retModified = false;

			while (deletionWorkList.Count > 0)
			{
				var n = deletionWorkList.First();
				deletionWorkList.Remove(n);

				if (IsCallNode(n) || IsDelegateCallNode(n))
				{
					calls.Add(GetInvocationInfo(n));
					continue;
				}

				if (IsRetNode(n))
				{
					retModified = true;
				}
				//if (IsDelegateCallNode(n))
				//{
				//    calls.Add(GetCallNode(n));
				//}
				var v = GetVertex(n);
				var types = GetDeletedTypes(n);
				foreach (var v1 in graph.EnumerateAdjacentVertices(v))
				{
					var n1 = GetAnalysisNode(v1);

					// Check here if some ingoing edge propagate some of the removed types
					if (true)
					{
						if (DiffDelProp(types, n1))
						{
							var e = graph.GetEdge(v, v1);
							var edgeTypes = (ISet<AnalysisType>)e["Types"];
							edgeTypes.ExceptWith(types);
						}
					}

					// DiffPropDelegates(GetDelegates(n), n1);

				}
				var ts = GetTypesMS(n);
				var removed = ts.ExceptWith(types);
			}
			return new PropagationEffects(calls, retModified);
		}

		private IEnumerable<AnalysisType> GetInGoingTypesFromEdges(Vertex v)
		{
			var res = new HashSet<AnalysisType>();
			foreach (var e in graph.EnumerateEdges().Where(e => e.ToVertexId == v.Id))
			{
				res.UnionWith((ISet<AnalysisType>)e["Types"]);
			}
			return res;
		}

		public void Save(string path)
		{
			IGraphDataAdapter dataAdapter = new GraphvizGraphDataAdapter(path);

			this.graph.Save(dataAdapter);
		}

		public bool HasBeenPropagated { get; private set; }

		/// <summary>
		///  This function is used to try to get a node using his name
		///  It can be use (for instance) in the incremental analysis
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		internal AnalysisNode FindNodeInPropationGraph(string text)
		{

			var nodes = this.Nodes.Where(n => n.ToString().Substring(0, text.Length).Equals(text));
			if (nodes.Count() > 0)
				return (AnalysisNode)nodes.First();
			return null;
		}
	}
}