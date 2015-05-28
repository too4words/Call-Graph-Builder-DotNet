using Common;
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
	/// <typeparam name="N"></typeparam>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="M"></typeparam>
	internal class GraphAnnotationData
	{
		internal Bag<TypeDescriptor> Elems = new Bag<TypeDescriptor>();
		//public ISet<T> Elems = new HashSet<T>();
		internal ISet<TypeDescriptor> DeletedElems { get; private set; }
		internal ISet<MethodDescriptor> Delegates { get; set; }
		internal PropGraphNodeDescriptor Node { get; set; }
		internal AnalysisInvocationExpession CallNode { get; set; }
		internal bool HasRetValue { get; set; }

		internal GraphAnnotationData(PropGraphNodeDescriptor m)
		{
			this.DeletedElems = new HashSet<TypeDescriptor>();
			this.Delegates = new HashSet<MethodDescriptor>();
			this.Node = m;
			this.HasRetValue = false;
		}
	}
}