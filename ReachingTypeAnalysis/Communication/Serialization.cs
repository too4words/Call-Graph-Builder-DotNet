﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace ReachingTypeAnalysis.Communication
{
	/// <summary>
	/// Mechanisms for providing (de-)serialization for in-memory data structures.
	/// </summary>
	internal static class Demarshaler
	{
		internal static AnalysisMethod Demarshal(MethodDescriptor methodDescriptor)
		{
			return default(AnalysisMethod);
		}

		internal static ISet<TypeDescriptor> Demarshal(ISet<TypeDescriptor> typeDescriptors)
		{
			throw new NotImplementedException();
		}

		internal static AnalysisNode Demarshal(VariableNode variableDescriptor)
		{
			throw new NotImplementedException();
		}

		internal static AnalysisNode Demarshal(InvocationDescriptor locationDescriptor)
		{
			throw new NotImplementedException();
		}
	}

	internal static class Marshal {
		internal static ISet<TypeDescriptor> ToTypeDescriptors(IEnumerable<TypeDescriptor> instantiatedTypes)
		{
			var result = new HashSet<TypeDescriptor>();
			foreach (var type in instantiatedTypes)
			{
                result.Add(new TypeDescriptor(type));
				//result.Add(new TypeDescriptor(type.RoslynType));
			}

			return result;
		}

		internal static IEnumerable<ISet<TypeDescriptor>> ToTypeDescriptorList(IEnumerable<ISet<TypeDescriptor>> argumentValues)
		{
			return argumentValues.Select(hash => ToTypeDescriptors(hash));
		}
	}
}
