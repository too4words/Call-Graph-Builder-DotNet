using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Statistics
{
	public static class SolutionStats
	{
		public static void ComputeSolutionStats(string solutionPath)
		{
			Utils.ComputeSolutionStatsAsync(solutionPath).Wait();
		}
	}
}
