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


        public MethodEntity ParseLibraryMethod()
        {
            if (this.RetVar != null)
            {
                this.StatementProcessor.RegisterNewExpressionAssignment(RetVar, 
                                            new TypeDescriptor(RetVar.Type,false));
            }

            var descriptor = new MethodEntityDescriptor(this.MethodDescriptor); //EntityFactory.Create(this.MethodDescriptor, this.Dispatcher);
            var methodEntity = new MethodEntity(this.MethodDescriptor,
                                                    this.MethodInterfaceData,
                                                    this.PropGraph, descriptor,
                                                    this.InstantiatedTypes,
                                                    false);
            return methodEntity;
        }
    }
}
