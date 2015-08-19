// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System.Collections.Generic;

namespace ReachingTypeAnalysis
{
	public class PropagationEffects
	{
		public PropagationEffects(IEnumerable<CallInfo> calleesInfo, bool resultChanged)
		{
			this.CalleesInfo = new HashSet<CallInfo>(calleesInfo);
			this.ResultChanged = resultChanged;
            this.CallersInfo = new HashSet<ReturnInfo>();
		}

		public PropagationEffects(IEnumerable<ReturnInfo> callersInfo)
		{
			this.CalleesInfo = new HashSet<CallInfo>();
			this.ResultChanged = true;
			this.CallersInfo = new HashSet<ReturnInfo>(callersInfo);
		}

		public ISet<CallInfo> CalleesInfo { get; set; }
		public bool ResultChanged { get; set; }
        public ISet<ReturnInfo> CallersInfo { get; set; }  
	}
}