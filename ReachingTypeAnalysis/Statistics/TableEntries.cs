using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Statistics
{
	public class SubjectExperimentResults : TableEntity
	{
		public string ExpID { get; set; }
		public DateTime Time { get; set; }
		public int Machines { get; set; }
		public string Subject { get; set; }
		public int Methods { get; set; }
		public int Messages { get; set; }
		public long ElapsedTime { get; set; }
		public long Activations { get; set; }
		public long Deactivations { get; set; }
		public string Observations { get; set; }
	}
	public class QueriesPerSubject : TableEntity
	{
		public string ExpID { get; set; }
		public DateTime Time { get; set; }
		public int Machines { get; set; }
		public string Subject { get; set; }
		public long AvgTime { get; set; }
		public long MinTime { get; set; }
		public long MaxTime { get; set; }
		public long Median { get; set; }
		public int Repeticions { get; set; }
		public string Observations { get; set; }
	}

	public class SiloRuntimeStats : TableEntity
	{
		public string ExpID { get; set; }
		//public string DeploymentId { get; set; }
		public string Address { get; set; }
		//public string SiloName { get; set; }
		//public string GatewayAddress { get; set; }
		//public string HostName { get; set; }

		public double CPU { get; set; }
		public long MemoryUsage { get; set; }
		public int Activations { get; set; }
		public int RecentlyUsedActivations { get; set; }
		//public int SendQueue { get; set; }
		//public int ReceiveQueue { get; set; }
		//public long RequestQueue { get; set; }
		public long SentMessages { get; set; }
		public long ReceivedMessages { get; set; }
		//public bool LoadShedding { get; set; }
		//public long ClientCount { get; set; }
	}
}
