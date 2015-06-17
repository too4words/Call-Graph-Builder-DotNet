// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Tachyon;
using ReachingTypeAnalysis.Communication;
using ReachingTypeAnalysis.Roslyn;

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
    internal partial class PropagationGraph
    {

        internal async Task<bool> DiffPropAysnc(IEnumerable<TypeDescriptor> src, PropGraphNodeDescriptor n, PropagationKind propKind)
        {
            if (propKind == PropagationKind.REMOVE_TYPES || propKind == PropagationKind.REMOVE_ASSIGNMENT)
            {
                return DiffDelProp(src, n);
            }
            else
            {
                var res = await DiffPropAsync(src, n);
                return res;
            }
        }

        internal async Task<bool> DiffPropAsync(IEnumerable<TypeDescriptor> src, PropGraphNodeDescriptor n)
        {
            var ts = GetTypesMS(n);
            int c = ts.Count;
            foreach(var t  in src)
            {
                if(!ts.Contains(t) )
                {
                    var isAsig = await IsAssignableAsync(t, n);
                    if(isAsig)
                    {
                        ts.Add(t);
                    }
                }
            }
            //ts.UnionWith(src.Where(t => !ts.Contains(t) && (await IsAssignableAsync(t, n))));
            if (ts.Count > c)
            {
                this.AddToWorkList(n);
                return true;
            }
            return false;
        }

        internal async Task<bool> IsAssignableAsync(TypeDescriptor t1, PropGraphNodeDescriptor analysisNode)
        {
            // Contract.Assert(this.codeProvider!=null);
            if (codeProvider == null)
            {
                return true;
            }

            var res = true;
            // Ugly
            TypeDescriptor type1 = t1;

            var type2 = analysisNode.Type;

            if (!(await this.codeProvider.IsSubtypeAsync(type1, type2)))
            {
                if (!type2.IsDelegate)
                {
                    if (!IsCallNode(analysisNode) && !IsDelegateCallNode(analysisNode))
                    {
                        return false;
                    }
                }
            }
            return res;
        }


        internal async Task<PropagationEffects> PropagateAsync(ICodeProvider codeProvider)
        {
            this.codeProvider = codeProvider;

            var calls = new HashSet<AnalysisInvocationExpession>();
            bool retModified = false;

            while (workList.Count > 0)
            {
                var analysisNode = workList.First();
                this.RemoveFromWorkList(analysisNode);
                if (IsCallNode(analysisNode) || IsDelegateCallNode(analysisNode))
                {
                    calls.Add(GetInvocationInfo(analysisNode));
                    continue;
                }
                if (IsRetNode(analysisNode))
                {
                    retModified = true;
                }

                var v = GetVertex(analysisNode);

                var types = GetTypes(analysisNode);

                foreach (var v1 in graph.GetTargets(v.Id))
                {
                    var n1 = GetAnalysisNode(v1);

                    await DiffPropAsync(types, n1);

                    var e = graph.GetEdge(v.Id, v1.Id);
                    e.Value.Types = types;

                    DiffPropDelegates(GetDelegates(analysisNode), n1);
                }
            }
            HasBeenPropagated = true;
            return new PropagationEffects(calls, retModified);
        }

        internal async Task<PropagationEffects> PropagateDeletionOfNodesAsync()
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
                foreach (var v1 in graph.GetTargets(v.Id))
                {
                    var n1 = GetAnalysisNode(v1);

                    // Check here if some ingoing edge propagate some of the removed types
                    if (true)
                    {
                        if (DiffDelProp(types, n1))
                        {
                            var e = graph.GetEdge(v.Id, v1.Id);
                            e.Value.Types.ExceptWith(types);
                        }
                    }

                    // DiffPropDelegates(GetDelegates(n), n1);

                }
                var ts = GetTypesMS(n);
                var removed = ts.ExceptWith(types);
            }
            return new PropagationEffects(calls, retModified);
        }
    }
}