using Microsoft.CodeAnalysis;
using Orleans;
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using ReachingTypeAnalysis.Communication;
using ReachingTypeAnalysis.Roslyn;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;

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
		public ISet<TypeDescriptor> InstantiatedTypes { get; private set; }

		/// This is for the propagation of removal of concrete types
		/// It is currently not used. 
		//public ISet<T> TypesRequestedTobeRemoved { get; private set; }

		/// <summary>
		/// This is a flag to indicate that information has been propagated at least once
		/// </summary>
        public bool HasBeenPropagated { get { return PropGraph.HasBeenPropagated; } }

        public bool CanBeAnalized { get; private set; }
		/// <summary>
		/// This maintain the set of methods that calls this method
		/// This is used to compute the Caller of the CG 
		/// and to know to which method a return message should be send
		/// </summary>
		private ISet<CallContext> callers = new HashSet<CallContext>();
		//private IImmutableSet<CallContext> callers = ImmutableHashSet<CallContext>.Empty;

		// I need this to yield a copy to avoid problems in recursion
		public ISet<CallContext> Callers
		{
			get { return new HashSet<CallContext>(callers); }
		}

		/// <summary>
		/// The method being analyzed
		/// </summary>
		public MethodDescriptor MethodDescriptor { get; private set; }

        /// <summary>
        /// This group together all input/out data
        /// That is: parameters, return, out values
        /// It will also include potential effects that we need to propagate (like a R/W effect over a field)
        /// </summary>
        internal MethodInterfaceData MethodInterfaceData { get; private set; }

        /// <summary>
        /// The next properties obtains info from MethodDataInterface
        /// </summary>
        public VariableNode ThisRef
        {
            get { return MethodInterfaceData.ThisRef; }
        }
        /// <summary>
        /// These are the node correspoding to the parameters
        /// </summary>
        public IList<ParameterNode> ParameterNodes
        {
            get { return MethodInterfaceData.Parameters; }

        }
        /// <summary>
        /// Return the node rerpesenting the ret value
        /// </summary>
        public VariableNode ReturnVariable
        {
            get { return MethodInterfaceData.ReturnVariable; }
        }
		/// <summary>
		/// This is the important part of the this class 
		/// The propagation graph constains of all information about concrete types that a variable can have
		/// </summary>       
        private PropagationGraph propGraph; 
		///public PropagationGraph PropGraph { get; private set; }
        public PropagationGraph PropGraph { get { return propGraph; } }
		
		/// <summary>
		/// We use this mapping as a cache of already computed callees info
		/// </summary>
		//public MethodEntityProcessor EntityProcessor { get; private set; }

		//public ISet<E> NodesProcessing = new HashSet<E>();
		//public ISet<CallConext<M,E>> NodesProcessing = new HashSet<CallConext<M,E>>();

		/// <summary>
		/// A method entity is built using the methods interface (params, return) and the propagation graph that is 
		/// initially populated with the variables, parameters and calls of the method 
		/// </summary>
		/// <param name="methodDescriptor"></param>
		/// <param name="mid"></param>
		/// <param name="propGraph"></param>
		/// <param name="instantiatedTypes"></param>
		public MethodEntity(MethodDescriptor methodDescriptor,
			MethodInterfaceData mid,
			PropagationGraph propGraph,
            IEntityDescriptor descriptor,
			IEnumerable<TypeDescriptor> instantiatedTypes,
            bool canBeAnalyzed = true)
			: base()
		{
			this.MethodDescriptor = methodDescriptor;
            this.EntityDescriptor = descriptor; // EntityFactory.Create(methodDescriptor);
			this.MethodInterfaceData = mid;

			this.propGraph = propGraph;
			this.InstantiatedTypes = new HashSet<TypeDescriptor>(instantiatedTypes);
            this.CanBeAnalized = canBeAnalyzed;
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
        //public IEntityProcessor GetEntityProcessor(IDispatcher dispatcher)
        //{
        //    //if (this.EntityProcessor == null)
        //    //{
        //    //    EntityProcessor = new MethodEntityProcessor(this, dispatcher, true);
        //    //}
        //    //return EntityProcessor;
        //    return new MethodEntityProcessor(this, dispatcher, true);
        //}

        public void AddToCallers(CallContext context)
        {
            //callers = callers.Add(context);
            callers.Add(context);

        }
        /// <summary>
        /// This is used by the incremental analysis when a caller is removed of modified
        /// </summary>
        /// <param name="context"></param>
        public void RemoveFromCallers(CallContext context)
        {
            //callers = callers.Remove(context);
            callers.Remove(context);

        }

//        /// <summary>
//        /// Obtains the concrete types that a method expression (e.g, variable, parameter, field) may refer 
//        /// </summary>
//        /// <param name="n"></param>
//        /// <returns></returns>
//        public ISet<TypeDescriptor> GetTypes(PropGraphNodeDescriptor n)
//        {
//            if (n != null)
//            {
//                return this.PropGraph.GetTypes(n);
//            }
//            else
//            {
//                return new HashSet<TypeDescriptor>();
//            }
//        }

//        /// <summary>
//        /// Obtains the methods a delegate var may refer
//        /// </summary>
//        /// <param name="n"></param>
//        /// <returns></returns>
//        internal ISet<MethodDescriptor> GetDelegates(DelegateVariableNode n)
//        {
//            return this.PropGraph.GetDelegates(n);
//        }

//        internal ISet<TypeDescriptor> GetPotentialTypes(PropGraphNodeDescriptor analysisNode)
//        {
//            var result = new HashSet<TypeDescriptor>();
//            foreach (var typeDescriptor in PropGraph.GetTypes(analysisNode))
//            {
//                if (typeDescriptor.IsConcreteType)
//                {
//                    result.Add(typeDescriptor);
//                }
//                else
//                {
//                    result
//                        .UnionWith(InstantiatedTypes
//                            .Where(candidateTypeDescriptor => CodeProvider.IsSubtype(candidateTypeDescriptor,typeDescriptor)));
////							.Where(iType => iType.IsSubtype(t)));
//                }
//            }
//            return result;
//        }

		public void Save(string path)
		{
			PropGraph.Save(path);
		}

		public override string ToString()
		{
			return this.MethodDescriptor.ToString();
		}

        internal AnalysisCallNode GetCallSiteByOrdinal(int invocationPosition)
        {
            foreach(var callNode in this.PropGraph.CallNodes)
            {
                if(callNode.InMethodPosition==invocationPosition)
                {
                    return callNode;
                }
            }
            throw new ArgumentException();
            //return null;
        }
    }

    [Serializable]
	internal class MethodEntityDescriptor : IEntityDescriptor
	{
		private MethodDescriptor methodDescriptor;

        public MethodDescriptor MethodDescriptor
		{
			get { return methodDescriptor; }
			private set { methodDescriptor = value; }
		}
        internal MethodEntityDescriptor(MethodDescriptor methodDescriptor)
		{
			this.methodDescriptor = methodDescriptor;
		}
		public override bool Equals(object obj)
		{
			MethodEntityDescriptor md = obj as MethodEntityDescriptor;
			return md != null && methodDescriptor.Equals(md.methodDescriptor);
		}
		public override int GetHashCode()
		{
			return methodDescriptor.GetHashCode();
		}
		public override string ToString()
		{
			return methodDescriptor.ToString();
		}
	}

    [Serializable]
	internal class MethodInterfaceData
	{
		/// <summary>
		/// Future use: I include this to support other input/output info
		/// in addition to the parameters and retvalue
		/// </summary>
		internal MethodInterfaceData()
		{
			this.InputData = new Dictionary<string, PropGraphNodeDescriptor>();
			this.OutputData = new Dictionary<string, PropGraphNodeDescriptor>();
		}

		public VariableNode ThisRef { get; internal set; }

		public IList<ParameterNode> Parameters { get; internal set; }

		public VariableNode ReturnVariable { get; internal set; }

		public IDictionary<string, PropGraphNodeDescriptor> OutputData { get; internal set; }

		public IDictionary<string, PropGraphNodeDescriptor> InputData { get; internal set; }
	}

    internal class MethodEntityWithPropagator : IMethodEntityWithPropagator
    {
        private MethodEntity methodEntity;
        private ICodeProvider codeProvider;
        //private Orleans.Runtime.Logger logger = GrainClient.Logger;
        public MethodEntityWithPropagator(MethodDescriptor methodDescriptor, Solution solution)
        {
            //var providerAndSyntax = ProjectCodeProvider.GetProjectProviderAndSyntaxAsync(methodDescriptor,solution).Result;
            //this.codeProvider = providerAndSyntax.Item1;
            //this.methodEntity = (MethodEntity)codeProvider.CreateMethodEntityAsync(methodDescriptor).Result;

            var providerEntity = ProjectCodeProvider.FindCodeProviderAndEntity(methodDescriptor,solution).Result;
            this.methodEntity = providerEntity.Item2;
            this.codeProvider = providerEntity.Item1;
            SolutionManager.Instance.AddInstantiatedTypes(this.methodEntity.InstantiatedTypes);
        }

        public async Task<PropagationEffects> PropagateAsync(PropagationKind propKind)
        {
            Logger.LogS( "MethodEntityGrain", "PropagateAsync", "Propagation for {0} ", this.methodEntity.MethodDescriptor);

            // var codeProvider = await ProjectGrainWrapper.CreateProjectGrainWrapperAsync(this.methodEntity.MethodDescriptor);
            var propagationEffects = await this.methodEntity.PropGraph.PropagateAsync(codeProvider);

            foreach (var calleeInfo in propagationEffects.CalleesInfo)
            {
                //  Add instanciated types! 
                /// Diego: Ben. This may not work well in parallel... 
                /// We need a different way to update this info
                calleeInfo.InstantiatedTypes = this.methodEntity.InstantiatedTypes;

                // TODO: This is because of the refactor
                if (calleeInfo is MethodCallInfo)
                {
                    var methodCallInfo = calleeInfo as MethodCallInfo;
                    methodCallInfo.ReceiverPossibleTypes = GetTypes(methodCallInfo.Receiver);
                    methodCallInfo.PossibleCallees = await GetPossibleCalleesForMethodCallAsync(methodCallInfo, codeProvider);
                }
                else if (calleeInfo is DelegateCallInfo)
                {
                    var delegateCalleeInfo = calleeInfo as DelegateCallInfo;
                    delegateCalleeInfo.ReceiverPossibleTypes = GetTypes(delegateCalleeInfo.Delegate);
                    delegateCalleeInfo.PossibleCallees = await GetPossibleCalleesForDelegateCallAsync(delegateCalleeInfo, codeProvider);
                }

                for (int i = 0; i < calleeInfo.Arguments.Count; i++)
                {
                    var arg = calleeInfo.Arguments[i];
                    var potentialTypes = arg != null ? GetTypes(arg, propKind) : new HashSet<TypeDescriptor>();
                    calleeInfo.ArgumentsPossibleTypes.Add(potentialTypes);
                }
            }

            if (this.methodEntity.ReturnVariable != null)
            {
                foreach (var callerContext in this.methodEntity.Callers)
                {
                    var returnInfo = new ReturnInfo(this.methodEntity.MethodDescriptor, callerContext);
                    returnInfo.ResultPossibleTypes = GetTypes(this.methodEntity.ReturnVariable);
                    returnInfo.InstantiatedTypes = this.methodEntity.InstantiatedTypes;

                    propagationEffects.CallersInfo.Add(returnInfo);
                }
            }
            Logger.LogS("MethodEntityGrain", "PropagateAsync", "End Propagation for {0} ", this.methodEntity.MethodDescriptor);
            //this.methodEntity.Save(@"C:\Temp\"+this.methodEntity.MethodDescriptor.MethodName + @".dot");
            return propagationEffects;
        }

        public async Task<PropagationEffects> PropagateAsync(CallMessageInfo callMessageInfo)
        {
            Logger.LogS("MethodEntityGrain", "PropagateAsync-call", "Propagation for {0} ", callMessageInfo.Callee);
            if (!this.methodEntity.CanBeAnalized) return new PropagationEffects(new HashSet<CallInfo>(), false);

            if (this.methodEntity.ThisRef != null)
            {
                await this.methodEntity.PropGraph.DiffPropAsync(callMessageInfo.ReceiverPossibleTypes, this.methodEntity.ThisRef, callMessageInfo.PropagationKind);
            }

            for (var i = 0; i < this.methodEntity.ParameterNodes.Count; i++)
            {
                var parameterNode = this.methodEntity.ParameterNodes[i];

                if (parameterNode != null)
                {
                    await this.methodEntity.PropGraph.DiffPropAsync(callMessageInfo.ArgumentsPossibleTypes[i], parameterNode, callMessageInfo.PropagationKind);
                }
            }

            var context = new CallContext(callMessageInfo.Caller, callMessageInfo.LHS, callMessageInfo.CallNode);
            this.methodEntity.AddToCallers(context);


            var effects = await PropagateAsync(callMessageInfo.PropagationKind);
            Logger.LogS("MethodEntityGrain", "PropagateAsync-call", "End Propagation for {0} ", callMessageInfo.Callee);
            return effects;
        }

        public async Task<PropagationEffects> PropagateAsync(ReturnMessageInfo returnMessageInfo)
        {
            Logger.LogS("MethodEntityGrain", "PropagateAsync-return", "Propagation for {0} ", returnMessageInfo.Caller);
            //PropGraph.Add(lhs, retValues);
            await this.methodEntity.PropGraph.DiffPropAsync(returnMessageInfo.ResultPossibleTypes, returnMessageInfo.LHS, returnMessageInfo.PropagationKind);
            var effects = await PropagateAsync(returnMessageInfo.PropagationKind);
            Logger.LogS("MethodEntityGrain", "PropagateAsync-return", "End Propagation for {0} ", returnMessageInfo.Caller);
            return effects;
        }

        private async Task<ISet<MethodDescriptor>> GetPossibleCalleesForMethodCallAsync(MethodCallInfo methodCallInfo, ICodeProvider codeProvider)
        {
            var possibleCallees = new HashSet<MethodDescriptor>();

            // TODO: This is not good: one reason is that loads like b = this.f are not working
            // in a method m after call r.m() because only the value of r is passed and not all its structure (fields)

            if (methodCallInfo.Method.IsStatic)
            {
                // Static method call
                possibleCallees.Add(methodCallInfo.Method);
            }
            else
            {
                // Instance method call

                //// I need to compute all the callees
                //// In case of a deletion we can discard the deleted callee

                //// If callInfo.ReceiverPossibleTypes == {} it means that some info in missing => we should be conservative and use the instantiated types (RTA) 
                //// TODO: I make this False for testing what happens if we remove this

                //if (conservativeWithTypes && methodCallInfo.ReceiverPossibleTypes.Count == 0)
                //{
                //	// TO-DO: Should I fix the node in the receiver to show that is not loaded. Ideally I should use the declared type. 
                //	// Here I will use the already instantiated types

                //	foreach (var candidateTypeDescriptor in methodCallInfo.InstantiatedTypes)
                //	{
                //		var isSubtype = await codeProvider.IsSubtypeAsync(candidateTypeDescriptor, methodCallInfo.Receiver.Type);

                //		if (isSubtype)
                //		{
                //			methodCallInfo.ReceiverPossibleTypes.Add(candidateTypeDescriptor);
                //		}
                //	}
                //}

                if (methodCallInfo.ReceiverPossibleTypes.Count > 0)
                {
                    foreach (var receiverType in methodCallInfo.ReceiverPossibleTypes)
                    {
                        // Given a method m and T find the most accurate implementation wrt to T
                        // it can be T.m or the first super class implementing m
                        var methodDescriptor = await codeProvider.FindMethodImplementationAsync(methodCallInfo.Method, receiverType);
                        possibleCallees.Add(methodDescriptor);
                    }
                }
                //else
                //{
                //    // We don't have any possibleType for the receiver,
                //    // so we just use the receiver's declared type to
                //    // identify the calle method implementation
                //    possibleCallees.Add(methodCallInfo.Method);
                //}
            }

            return possibleCallees;
        }

        private async Task<ISet<MethodDescriptor>> GetPossibleCalleesForDelegateCallAsync(DelegateCallInfo delegateCallInfo, ICodeProvider codeProvider)
        {
            var possibleCallees = new HashSet<MethodDescriptor>();
            var possibleDelegateMethods = GetPossibleMethodsForDelegate(delegateCallInfo.Delegate);

            foreach (var method in possibleDelegateMethods)
            {
                if (method.IsStatic)
                {
                    // Static method call
                    possibleCallees.Add(method);
                }
                else
                {
                    // Instance method call

                    if (delegateCallInfo.ReceiverPossibleTypes.Count > 0)
                    {
                        foreach (var receiverType in delegateCallInfo.ReceiverPossibleTypes)
                        {
                            //var aMethod = delegateInstance.FindMethodImplementation(t);
                            // Diego: Should I use : codeProvider.FindImplementation(delegateInstance, t);
                            var callee = await codeProvider.FindMethodImplementationAsync(method, receiverType);
                            possibleCallees.Add(callee);
                        }
                    }
                    else
                    {
                        // We don't have any possibleType for the receiver,
                        // so we just use the receiver's declared type to
                        // identify the calle method implementation

                        // if Count is 0, it is a delegate that do not came form an instance variable
                        possibleCallees.Add(method);
                    }
                }
            }

            return possibleCallees;
        }

        private ISet<TypeDescriptor> GetTypes(PropGraphNodeDescriptor analysisNode)
        {
            if (analysisNode != null)
            {
                return this.methodEntity.PropGraph.GetTypes(analysisNode);
            }
            else
            {
                return new HashSet<TypeDescriptor>();
            }
        }
        private ISet<TypeDescriptor> GetTypes(PropGraphNodeDescriptor node, PropagationKind prop)
        {
            switch (prop)
            {
                case PropagationKind.ADD_TYPES:
                    return GetTypes(node);
                case PropagationKind.REMOVE_TYPES:
                    return GetDeletedTypes(node);
                default:
                    return GetTypes(node);
            }
        }
        internal ISet<TypeDescriptor> GetDeletedTypes(PropGraphNodeDescriptor node)
        {
            if (node != null)
            {
                return this.methodEntity.PropGraph.GetDeletedTypes(node);
            }
            else
            {
                return new HashSet<TypeDescriptor>();
            }
        }
        internal ISet<MethodDescriptor> GetPossibleMethodsForDelegate(DelegateVariableNode node)
        {
            return this.methodEntity.PropGraph.GetDelegates(node);
        }

        public Task<IEntity> GetMethodEntityAsync()
        {
            // Contract.Assert(this.methodEntity != null);
            return Task.FromResult<IEntity>(this.methodEntity);
        }

        public Task<ISet<MethodDescriptor>> GetCalleesAsync()
        {
            var codeProvider = this.codeProvider;
            Contract.Assert(codeProvider != null);
            return CallGraphQueryInterface.GetCalleesAsync(this.methodEntity, codeProvider);
        }

        public Task<IDictionary<AnalysisCallNode, ISet<MethodDescriptor>>> GetCalleesInfoAsync()
        {
            return CallGraphQueryInterface.GetCalleesInfo(this.methodEntity, this.codeProvider);
        }
        public Task<bool> IsInitializedAsync()
        {
            return Task.FromResult(this.methodEntity != null);
        }
    }
}
