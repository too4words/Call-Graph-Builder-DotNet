﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.Cci.Extensions.CSharp;

namespace Microsoft.Cci.Writers.CSharp
{
    public partial class CSDeclarationWriter
    {
        private void WritePropertyDefinition(IPropertyDefinition property)
        {
            bool isInterfaceProp = property.ContainingTypeDefinition.IsInterface;
            IMethodDefinition accessor = null;
            IMethodDefinition getter = null;
            IMethodDefinition setter = null;
            if (property.Getter != null)
            {
                getter = property.Getter.ResolvedMethod;
                if (!_filter.Include(getter))
                    getter = null;
                accessor = getter;
            }

            if (property.Setter != null)
            {
                setter = property.Setter.ResolvedMethod;
                if (!_filter.Include(setter))
                    setter = null;
                if (accessor == null)
                    accessor = setter;
            }

            if (accessor == null)
                return;

            bool isIndexer = accessor.ParameterCount > (accessor == setter ? 1 : 0);

            if (isIndexer)
            {
                string id = property.Name.Value;
                int index = id.LastIndexOf(".");
                if (index >= 0)
                    id = id.Substring(index + 1);

                if (id != "Item")
                {
                    WriteFakeAttribute("System.Runtime.CompilerServices.IndexerName", "\"" + id + "\"");
                }
            }

            WriteAttributes(property.Attributes);

            if (!isInterfaceProp)
            {
                if (!accessor.IsExplicitInterfaceMethod())
                    WriteVisibility(property.Visibility);

                // Getter and Setter modifiers should be the same
                WriteMethodModifiers(accessor);
            }
            if (property.GetHiddenBaseProperty(_filter) != Dummy.Property)
                WriteKeyword("new");

            WriteTypeName(property.Type);
            if (isIndexer)
            {
                int index = property.Name.Value.LastIndexOf(".");
                if (index >= 0)
                    WriteIdentifier(property.Name.Value.Substring(0, index + 1) + "this", false); // +1 to include the '.'
                else
                    WriteIdentifier("this", false);

                var parameters = new List<IParameterDefinition>(accessor.Parameters);
                if (accessor == setter) // If setter remove value parameter.
                    parameters.RemoveAt(parameters.Count - 1);
                WriteParameters(parameters, true);
            }
            else
            {
                WriteIdentifier(property.Name);
            }
            WriteSpace();
            WriteSymbol("{");

            //get
            if (getter != null)
            {
                WriteAccessorDefinition(property, getter, "get");
            }
            //set
            if (setter != null)
            {
                WriteAccessorDefinition(property, setter, "set");
            }
            WriteSpace();
            WriteSymbol("}");
        }

        private void WriteAccessorDefinition(IPropertyDefinition property, IMethodDefinition accessor, string accessorType)
        {
            WriteSpace();
            WriteAttributes(accessor.Attributes, writeInline: true);
            // If the accessor is an internal call (or a PInvoke) we should put those attributes here as well

            WriteMethodPseudoCustomAttributes(accessor);

            if (accessor.Visibility != property.Visibility)
                WriteVisibility(accessor.Visibility);
            WriteKeyword(accessorType, noSpace: true);
            WriteMethodBody(accessor);
        }
    }
}
