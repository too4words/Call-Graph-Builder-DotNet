﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using ReachingTypeAnalysis.Communication;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;

namespace ReachingTypeAnalysis.Analysis
{
	/// <summary>
	/// Method entities contains all the relavant data for the analysis o a given method
	/// In particular, the input/output parameters and the "propagation graph" where concrete type propagate through method variables
	/// </summary>
	/// <typeparam name="E"></typeparam>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="M"></typeparam>
	/// 
	[Serializable]
	internal class MethodEntity : Entity
	{
		/// <summary>
		///  This is how we name entities in the dispacther
		/// </summary>
		public IEntityDescriptor EntityDescriptor { get; private set; }

		/// <summary>
		/// This is for having a RTA like set of instantiated types 
		/// We need this give type to  unsupported expression using the declaredType instead of the concrete type 
		/// </summary>
		public ISet<AnalysisType> InstantiatedTypes { get; private set; }

		/// This is for the propagation of removal of concrete types
		/// It is currently not used. 
		//public ISet<T> TypesRequestedTobeRemoved { get; private set; }

		/// <summary>
		/// This is a flag to indicate that information has been propagated at least once
		/// </summary>
		public bool HasBeenPropagated { get { return PropGraph.HasBeenPropagated; } }

		/// <summary>
		/// This maintain the set of methods that calls this method
		/// This is used to compute the Caller of the CG 
		/// and to know to which method a return message should be send
		/// </summary>
		//private ISet<CallConext<M, E>> callers = new HashSet<CallConext<M, E>>();
		private IImmutableSet<CallContext> callers = ImmutableHashSet<CallContext>.Empty;

		// I need this to yield a copy to avoid problems in recursion
		public ISet<CallContext> Callers
		{
			get { return new HashSet<CallContext>(callers); }
		}

		/// <summary>
		/// The method being analyzed
		/// </summary>
		public AnalysisMethod Method { get; private set; }

		/// <summary>
		/// This is the important part of the this class 
		/// The propagation graph constains of all information about concrete types that a variable can have
		/// </summary>
		public PropagationGraph PropGraph { get; private set; }
		/// <summary>
		/// This group together all input/out data
		/// That is: parameters, return, out values
		/// It will also include potential effects that we need to propagate (like a R/W effect over a field)
		/// </summary>
		internal MethodInterfaceData MethodInterfaceData { get; private set; }

		/// <summary>
		/// The next properties obtains info from MethodDataInterface
		/// </summary>
		public AnalysisNode ThisRef
		{
			get { return MethodInterfaceData.ThisRef; }
		}
		/// <summary>
		/// These are the node correspoding to the parameters
		/// </summary>
		public IEnumerable<AnalysisNode> ParameterNodes
		{
			get { return MethodInterfaceData.Parameters; }

		}
		/// <summary>
		/// Return the node rerpesenting the ret value
		/// </summary>
		public AnalysisNode ReturnVariable
		{
			get { return MethodInterfaceData.ReturnVariable; }
		}

		/// <summary>
		/// We use this mapping as a cache of already computed callees info
		/// </summary>
		private IDictionary<AnalysisNode, ISet<AnalysisMethod>> calleesMappingCache = new Dictionary<AnalysisNode, ISet<AnalysisMethod>>();
		public MethodEntityProcessor EntityProcessor { get; private set; }

		//public ISet<E> NodesProcessing = new HashSet<E>();
		//public ISet<CallConext<M,E>> NodesProcessing = new HashSet<CallConext<M,E>>();

		/// <summary>
		/// A method entity is built using the methods interface (params, return) and the propagation graph that is 
		/// initially populated with the variables, parameters and calls of the method 
		/// </summary>
		/// <param name="method"></param>
		/// <param name="mid"></param>
		/// <param name="propGraph"></param>
		/// <param name="instantiatedTypes"></param>
		public MethodEntity(AnalysisMethod method,
			MethodInterfaceData mid,
			PropagationGraph propGraph,
			IEnumerable<AnalysisType> instantiatedTypes)
			: base()
		{
			this.Method = method;
			this.EntityDescriptor = EntityFactory.Create(method);
			this.MethodInterfaceData = mid;

			this.PropGraph = propGraph;
			this.InstantiatedTypes = new HashSet<AnalysisType>(instantiatedTypes);
		}

		/// <summary>
		/// Copy the information regarding parameters and callers from one entity and other
		/// The PropGraph of the old entity is used but copied
		/// This is used when one method is updated to recompute the PropGraph using the original input values
		/// </summary>
		/// <param name="oldEntity"></param>
		internal void CopyInterfaceDataAndCallers(MethodEntity entity)
		{
			Contract.Requires(this.ParameterNodes != null);
			foreach (var parameter in this.ParameterNodes)
			{
				if (parameter != null)
				{
					this.PropGraph.Add(parameter, entity.PropGraph.GetTypes(parameter));
					this.PropGraph.AddToWorkList(parameter);
				}
			}
			if (this.ThisRef != null)
			{
				this.PropGraph.Add(this.ThisRef, entity.PropGraph.GetTypes(entity.ThisRef));
				this.PropGraph.AddToWorkList(this.ThisRef);
			}

			foreach (var callersContext in entity.Callers)
			{
				this.AddToCallers(callersContext);
			}
		}

		/// <summary>
		/// Return an EntityProccesor. It is the class that performs the analysis using the data in the entity
		/// </summary>
		/// <param name="dispatcher"></param>
		/// <returns></returns>
		public override IEntityProcessor GetEntityProcessor(IDispatcher dispatcher)
		{
			if (this.EntityProcessor == null)
			{
				EntityProcessor = new MethodEntityProcessor(this, dispatcher, true);
			}
			return EntityProcessor;
		}

		#region Methods that compute caller callees relation 
		public void InvalidateCaches()
		{
			calleesMappingCache.Clear();
		}
		/// <summary>
		/// Compute all the calless of this method entities
		/// </summary>
		/// <returns></returns>
		public IEnumerable<AnalysisMethod> Callees()
		{
			var result = new HashSet<AnalysisMethod>();
			foreach (var callNode in PropGraph.CallNodes)
			{
				result.UnionWith(Callees(callNode));
			}
			return result;
		}

		/// <summary>
		/// Computes all the potential callees for a particular method invocation
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public ISet<AnalysisMethod> Callees(AnalysisNode node)
		{
			ISet<AnalysisMethod> result;

			if (!calleesMappingCache.TryGetValue(node, out result))
			{
				var calleesForNode = new HashSet<AnalysisMethod>();
				var invExp = PropGraph.GetInvocationInfo(node);
				Contract.Assert(invExp != null);

				calleesForNode.UnionWith(invExp.ComputeCalleesForNode(this.PropGraph));

				calleesMappingCache[node] = calleesForNode;
				result = calleesForNode;

			}
			return result;
		}

		/// <summary>
		/// Generates a dictionary invocationNode -> potential callees
		/// This is used for example by the demo to get the caller / callee info
		/// </summary>
		/// <returns></returns>
		public IDictionary<AnalysisNode, ISet<AnalysisMethod>> GetCalleesInfo()
		{
			var calleesPerEntity = new Dictionary<AnalysisNode, ISet<AnalysisMethod>>();
			foreach (var calleeNode in this.PropGraph.CallNodes)
			{
				calleesPerEntity[calleeNode] = Callees(calleeNode);
			}
			return calleesPerEntity;
		}

		//private void GetCallesForCalleeNode(ANode calleeNode)
		//{
		//    var res = new HashSet<AMethod>();
		//    var callInfo = this.PropGraph.GetInvocationInfo(calleeNode);
		//    res.UnionWith(callInfo.ComputeCalleesForNode(this.PropGraph));
		//    calleesPerEntityCache[calleeNode] = res;
		//}

		/// <summary>
		/// This methods register that there is a new caller for this method
		/// This is used in to register who calls this method
		/// </summary>
		/// <param name="context"></param>
		public void AddToCallers(CallContext context)
		{
			callers = callers.Add(context);
		}
		/// <summary>
		/// This is used by the incremental analysis when a caller is removed of modified
		/// </summary>
		/// <param name="context"></param>
		public void RemoveFromCallers(CallContext context)
		{
			callers = callers.Remove(context);
		}
		#endregion

		/// <summary>
		/// Obtains the concrete types that a method expression (e.g, variable, parameter, field) may refer 
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public ISet<AnalysisType> GetTypes(AnalysisNode n)
		{
			if (n != null)
				return this.PropGraph.GetTypes(n);
			else return new HashSet<AnalysisType>();
		}

		/// <summary>
		/// Obtains the methods a delegate var may refer
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		internal ISet<AnalysisMethod> GetDelegates(AnalysisNode n)
		{
			return this.PropGraph.GetDelegates(n);
		}

		internal ISet<AnalysisType> GetPotentialTypes(AnalysisNode n)
		{
			var result = new HashSet<AnalysisType>();
			foreach (var t in PropGraph.GetTypes(n))
			{
				if (t.IsConcreteType)
				{
					result.Add(t);
				}
				else
				{
					result
						.UnionWith(InstantiatedTypes
							.Where(iType => iType.IsSubtype(t)));
				}
			}
			return result;
		}

		public void Save(string path)
		{
			PropGraph.Save(path);
		}

		public override string ToString()
		{
			return this.Method.ToString();
		}
	}

	internal class MethodEntityDescriptor<AMethod> : IEntityDescriptor
	{
		private AMethod method;

		public AMethod Method
		{
			get { return method; }
			private set { method = value; }
		}
		internal MethodEntityDescriptor(AMethod method)
		{
			this.method = method;
		}
		public override bool Equals(object obj)
		{
			MethodEntityDescriptor<AMethod> md = obj as MethodEntityDescriptor<AMethod>;
			return md != null && method.Equals(md.method);
		}
		public override int GetHashCode()
		{
			return method.GetHashCode();
		}
		public override string ToString()
		{
			return method.ToString();
		}
	}

	internal class MethodInterfaceData
	{
		/// <summary>
		/// Future use: I include this to support other input/output info
		/// in addition to the parameters and retvalue
		/// </summary>
		internal MethodInterfaceData()
		{
			this.InputData = new Dictionary<string, AnalysisNode>();
			this.OutputData = new Dictionary<string, AnalysisNode>();
		}
		public AnalysisNode ThisRef { get; internal set; }

		public IEnumerable<AnalysisNode> Parameters { get; internal set; }

		public AnalysisNode ReturnVariable { get; internal set; }

		public IDictionary<string, AnalysisNode> OutputData { get; internal set; }

		public IDictionary<string, AnalysisNode> InputData { get; internal set; }
	}
}
