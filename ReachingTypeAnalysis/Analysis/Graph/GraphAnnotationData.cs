using System;
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System.Collections.Generic;

namespace ReachingTypeAnalysis
{
	/// <summary>
	/// This class represents the data that a node in the PropGraph can carry
	/// Elems: Concrete Types (we use a Bag to try to count the number of ocurrences)
	/// DeletedElems: Is the set of concrete types we want to delete. Used to propagate a removal of a type or an edge in the graph
	/// Node: is tne node value (the graph from Nuri uses numbers for vertex)
	/// CallNode: Some node may include information about an invocation (the call nodes)
	/// </summary>
	[Serializable]
	internal class GraphNodeAnnotationData
	{
		internal Bag<TypeDescriptor> Elems { get; private set; }
		internal ISet<TypeDescriptor> DeletedElems { get; private set; }
		internal ISet<MethodDescriptor> Delegates { get; set; }
		internal PropGraphNodeDescriptor Node { get; set; }
		internal CallInfo CallNode { get; set; }
		internal bool HasRetValue { get; set; }

		internal GraphNodeAnnotationData(PropGraphNodeDescriptor node)
		{
			this.Node = node;
			this.Elems = new Bag<TypeDescriptor>();
			this.DeletedElems = new HashSet<TypeDescriptor>();
			this.Delegates = new HashSet<MethodDescriptor>();			
			this.HasRetValue = false;
		}
	}

	/// <summary>
	/// This class represents the data that an edge in the PropGraph can carry
	/// </summary>
	[Serializable]
	internal class GraphEdgeAnnotationData
	{
		internal ISet<TypeDescriptor> Types { get; set; }

		internal GraphEdgeAnnotationData()
		{
			this.Types = new HashSet<TypeDescriptor>();
		}
	}
}
