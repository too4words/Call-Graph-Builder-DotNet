// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Orleans;
using OrleansInterfaces;
using ReachingTypeAnalysis.Roslyn;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Analysis
{
    [Serializable]
    internal class OrleansEntityDescriptor : IEntityDescriptor
    {
        public Guid Guid { get; set; }
        public MethodDescriptor MethodDescriptor { get; set; }

        public OrleansEntityDescriptor(MethodDescriptor methodDescriptor)
        {
            this.Guid = Guid.Empty;
            this.MethodDescriptor = methodDescriptor;
        }

        public OrleansEntityDescriptor(MethodDescriptor methodDescriptor, Guid guid)
        {
            this.Guid = guid;
            this.MethodDescriptor = methodDescriptor;
        }
        public override bool Equals(object obj)
        {
            var oed = (OrleansEntityDescriptor)obj;
            return oed != null && this.MethodDescriptor.Equals(oed.MethodDescriptor);
        }
        public override int GetHashCode()
        {
            return this.MethodDescriptor.GetHashCode();
        }

        public override string ToString()
        {
            return this.MethodDescriptor.ToString();
        }
    }

}