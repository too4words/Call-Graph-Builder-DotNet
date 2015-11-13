using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Statistics
{
	public static class SolutionStats
	{
		public static IList<MethodDescriptor> ComputeSolutionStats(string solutionPath)
		{
			var methods = Utils.ComputeSolutionStatsAsync(solutionPath).Result;
			return methods;
		}
	}
}
