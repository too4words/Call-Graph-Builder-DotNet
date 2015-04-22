// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System.Collections.Generic;

namespace ReachingTypeAnalysis
{
	internal class PropagationEffects
	{
		internal PropagationEffects(ISet<AnalysisInvocationExpession> calls, bool rvChange)
		{
			this.Calls = calls;
			this.RetValueChange = rvChange;
		}

		internal ISet<AnalysisInvocationExpession> Calls { get; set; }
		internal bool RetValueChange { get; set; }
	}
}