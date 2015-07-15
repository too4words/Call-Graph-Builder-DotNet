// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Tachyon;
using ReachingTypeAnalysis.Communication;
using ReachingTypeAnalysis.Roslyn;
using System.Diagnostics;

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
        internal async Task<bool> DiffPropAsync(IEnumerable<TypeDescriptor> src, PropGraphNodeDescriptor n, PropagationKind propKind)
        {
            Logger.Instance.Log("PropagationGraph", "DiffPropAsync", "Diff({0},{1})", src, n);

            if (propKind == PropagationKind.REMOVE_TYPES || propKind == PropagationKind.REMOVE_ASSIGNMENT)
            {
                return DiffDelProp(src, n);
            }
            else
            {
                return await DiffPropAsync(src, n);
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

            var calls = new HashSet<CallInfo>();
            var retModified = false;

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
            var calls = new HashSet<CallInfo>();
            var retModified = false;

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

        // DIEGO 
        // TODO: ADD Here the instantiated types
        internal async Task<ISet<TypeDescriptor>> GetPotentialTypesAsync(PropGraphNodeDescriptor n, MethodCallInfo callInfo, ICodeProvider codeProvider)
        {
            var result = new HashSet<TypeDescriptor>();
            var types = this.GetTypes(n);

            if (types.Count() == 0)
            {
                foreach(var potentialType in callInfo.InstantiatedTypes)
                {
                    if(await codeProvider.IsSubtypeAsync(potentialType, callInfo.Receiver.Type))
                    {
                        types.Add(potentialType);
                    }
                }
            }
            if (types.Count() == 0)
            {
                types.Add(callInfo.Receiver.Type);
            }
            foreach (var typeDescriptor in types)
            {
                // TO-DO fix by adding a where T: AnalysisType
                if (typeDescriptor.IsConcreteType)
                {
                    result.Add(typeDescriptor);
                }
                else
                {
                    Contract.Assert(callInfo.InstantiatedTypes != null);
                    foreach (var candidateType in callInfo.InstantiatedTypes)
                    {
                        var isSubtype = await codeProvider.IsSubtypeAsync(candidateType, typeDescriptor);
                        if (isSubtype)
                        {
                            result.Add(candidateType);
                        }
                    }
                }
            }
            return result;
        }
        internal  ISet<MethodDescriptor> ComputeCalleesForCallNode(MethodCallInfo callInfo, ICodeProvider codeProvider)
        {
            var calleesForNode = new HashSet<MethodDescriptor>();
            if (callInfo.Receiver != null)
            {
                // I replaced the invocation for a local call to mark that functionality is missing
                //var callees = GetPotentialTypes(this.Receiver, propGraph)
                //    .Select(t => this.Callee.FindMethodImplementation(t));
                var callees = GetPotentialTypes(callInfo.Receiver, callInfo, codeProvider)
                        .Select(t => codeProvider.FindMethodImplementation(callInfo.Method, t));
                calleesForNode.UnionWith(callees);
            }
            else
            {
                calleesForNode.Add(callInfo.Method);
            }
            return calleesForNode;
        }
        /// <summary>
        ///  C
        /// </summary>
        /// <param name="invoInfo"></param>
        /// <param name="codeProvider"></param>
        /// <returns></returns>
        internal Task<ISet<MethodDescriptor>> ComputeCalleesForNodeAsync(CallInfo invoInfo, ICodeProvider codeProvider)
        {
            //TODO: Ugly... but we needed this refactor for moving stuff to the common project 
            if (invoInfo is MethodCallInfo)
            {
                return ComputeCalleesForCallNodeAsync((MethodCallInfo)invoInfo, codeProvider);                
            }
            Contract.Assert(invoInfo is DelegateCallInfo);
            return ComputeCalleesForDelegateNodeAsync((DelegateCallInfo)invoInfo, codeProvider);
        }

        internal async  Task<ISet<MethodDescriptor>> ComputeCalleesForCallNodeAsync(MethodCallInfo callInfo, ICodeProvider codeProvider)
        {
            Contract.Assert(codeProvider != null);
            var calleesForNode = new HashSet<MethodDescriptor>();
            if (callInfo.Receiver != null)
            {
                // I replaced the invocation for a local call to mark that functionality is missing
                //var callees = GetPotentialTypes(this.Receiver, propGraph)
                //    .Select(t => this.Callee.FindMethodImplementation(t));
                foreach (var type in await this.GetPotentialTypesAsync(callInfo.Receiver, callInfo, codeProvider))
                {
                    Contract.Assert(type != null);
                    Contract.Assert(callInfo != null);

                    var realCallee = await codeProvider.FindMethodImplementationAsync(callInfo.Method, type);
                    Contract.Assert(realCallee != null);

                    calleesForNode.Add(realCallee);
                }
            }
            else
            {
                calleesForNode.Add(callInfo.Method);
            }
            return calleesForNode;
        }

        internal async Task<ISet<MethodDescriptor>> ComputeCalleesForDelegateNodeAsync(DelegateCallInfo callInfo, ICodeProvider codeProvider)
        {
            return await GetDelegateCalleesAsync(callInfo.Delegate, codeProvider);
        }

        private async Task<ISet<MethodDescriptor>> GetDelegateCalleesAsync(VariableNode delegateNode, ICodeProvider codeProvider)
        {
            var callees = new HashSet<MethodDescriptor>();
            var typeDescriptors = this.GetTypes(delegateNode);
            foreach (var delegateInstance in this.GetDelegates(delegateNode))
            {
                if (typeDescriptors.Count() > 0)
                {
                    foreach (var typeDescriptor in typeDescriptors)
                    {
                        // TO-DO!!!
                        // Ugly: I'll fix it
                        //var aMethod = delegateInstance.FindMethodImplementation(type);
                        var aMethod = await codeProvider.FindMethodImplementationAsync(delegateInstance, typeDescriptor);
                        callees.Add(aMethod);
                    }
                }
                else
                {
                    // if Count is 0, it is a delegate that do not came form an instance variable
                    callees.Add(delegateInstance);
                }
            }

            return callees;
        }

    }
}