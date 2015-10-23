﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci.Extensions;
using System.IO;

namespace Microsoft.Cci.Filters
{
    public class DocIdIncludeListFilter : ICciFilter
    {
        private readonly HashSet<string> _docIds;

        public DocIdIncludeListFilter(IEnumerable<string> docIds)
        {
            _docIds = new HashSet<string>(docIds);
        }

        public DocIdIncludeListFilter(string whiteListFilePath)
        {
            _docIds = DocIdExtensions.ReadDocIds(whiteListFilePath);
        }

        public bool AlwaysIncludeNonEmptyTypes { get; set; }

        public bool Include(INamespaceDefinition ns)
        {
            // Only include non-empty namespaces
            return ns.GetTypes().Any(Include);
        }

        public bool Include(ITypeDefinition type)
        {
            if (AlwaysIncludeNonEmptyTypes && type.Members.Any(Include))
                return true;

            string typeId = type.DocId();
            return _docIds.Contains(typeId);
        }

        public bool Include(ITypeDefinitionMember member)
        {
            string memberId = member.DocId();
            return _docIds.Contains(memberId);
        }

        public bool Include(ICustomAttribute attribute)
        {
            string typeId = attribute.DocId();
            string removeUsages = "RemoveUsages:" + typeId;

            if (_docIds.Contains(removeUsages))
                return false;

            return _docIds.Contains(typeId);
        }
    }
}
