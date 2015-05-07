// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using ReachingTypeAnalysis.Analysis;
using ReachingTypeAnalysis.Communication;
using System.Collections.Generic;

namespace ReachingTypeAnalysis.Roslyn
{
    /// <summary>
    /// Represent a method appearing in a Reference
    /// We only have MetadataInfo, not the code
    /// </summary>
    internal class LibraryMethodProcessor: GeneralRoslynMethodProcessor
    {
		internal LibraryMethodProcessor(IMethodSymbol method, IDispatcher dispatcher)
			: base(method, dispatcher)
		{ }

        internal LibraryMethodProcessor(MethodDescriptor methodDescriptor, IDispatcher dispatcher)
            : base(methodDescriptor,dispatcher)
        {
        }


        public IEntity ParseLibraryMethod()
        {
            if (this.RetVar != null)
            {
                this.StatementProcessor.RegisterNewExpressionAssignment(RetVar, 
                                            new TypeDescriptor(RetVar.Type,false));
            }

            var descriptor = EntityFactory.Create(this.MethodDescriptor);
            var methodEntity = EntityFactory.CreateEntity(
                                    new MethodEntity(this.MethodDescriptor,
                                                    this.MethodInterfaceData,
                                                    this.PropGraph,
                                                    this.InstantiatedTypes), descriptor);
            this.Dispatcher.RegisterEntity(descriptor, methodEntity);
            return methodEntity;
        }
        public override MethodInterfaceData CreateMethodInterfaceData()
        {
            ReturnNode retVar = null;
            ParameterNode thisRef = null;
            IList<ParameterNode> parameters;
            var inputs = new Dictionary<string, PropGraphNodeDescriptor>();
            var outputs = new Dictionary<string, PropGraphNodeDescriptor>();
            //if (!symbol.ReturnsVoid && Utils.IsTypeForAnalysis(symbol.ReturnType))
            //{
            //    retVar = new ReturnNode(new TypeDescriptor(symbol.ReturnType));
            //    outputs["retVar"] = retVar;
            //}
            //if (!symbol.IsStatic)
            //{
            //    thisRef = new ThisNode(new TypeDescriptor(symbol.ReceiverType));
            //}
            parameters = new List<ParameterNode>();
            //for (int i = 0; i < symbol.Parameters.Count(); i++)
            //{
            //    var p = symbol.Parameters[i];
            //    var parameterNode = new ParameterNode(symbol.Parameters[i].Name, i, new TypeDescriptor(p.Type));
            //    parameters.Add(parameterNode);
            //    if (p.RefKind == RefKind.Ref || p.RefKind == RefKind.Out)
            //    {
            //        outputs[p.Name] = parameterNode;
            //    }
            //    inputs[p.Name] = parameterNode;
            //}

            var methodInterfaceData = new MethodInterfaceData()
            {
                ReturnVariable = retVar,
                ThisRef = thisRef,
                Parameters = parameters,
                InputData = inputs,
                OutputData = outputs
            };
            return methodInterfaceData;
        }

    }
}
