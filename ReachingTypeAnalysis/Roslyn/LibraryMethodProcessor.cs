// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;
using ReachingTypeAnalysis.Analysis;
using ReachingTypeAnalysis.Communication;

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
    }
}
