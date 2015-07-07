// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System.Collections.Generic;

namespace ReachingTypeAnalysis
{
	public class PropagationEffects
	{
		public PropagationEffects(ISet<AnalysisInvocationExpession> calls, bool rvChange)
		{
			this.Calls = calls;
			this.RetValueChange = rvChange;
		}

		public ISet<AnalysisInvocationExpession> Calls { get; set; }
		public bool RetValueChange { get; set; }
	}
}