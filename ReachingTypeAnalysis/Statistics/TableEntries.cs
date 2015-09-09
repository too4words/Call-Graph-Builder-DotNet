using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Statistics
{

	public class TableEntityCSV: TableEntity
	{
		private PropertyInfo[] _PropertyInfos = null;

		public virtual string ToDelimited()
		{
			if(_PropertyInfos == null)
				_PropertyInfos = this.GetType().GetProperties();

			var list = new List<string>();
			foreach (var info in _PropertyInfos)
			{
				var value = info.GetValue(this, null) ?? "(null)";
				list.Add(value.ToString());
			}

			return string.Join(",",list);
		}
		public virtual string GetHeaders()
		{
			if (_PropertyInfos == null)
				_PropertyInfos = this.GetType().GetProperties();

			var list = new List<string>();
			foreach (var info in _PropertyInfos)
			{
				list.Add(info.Name.ToString());
			}

			return string.Join(",", list);
		}
	}

	public class SubjectExperimentResults : TableEntityCSV
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
		public long TotalRecvNetwork { get; set; }
		public long TotalSentLocal { get; set; }
		public long TotalSentNetwork { get; set; }
		public long TotalRecvLocal { get; set; }
		public double AverageLatency { get; set; }
	}
	public class QueriesPerSubject : TableEntityCSV
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

	public class SiloRuntimeStats : TableEntityCSV
	{
		public string ExpID { get; set; }
		//public string DeploymentId { get; set; }
		public DateTime Time { get; set; }
		public string Address { get; set; }
		//public string SiloName { get; set; }
		//public string GatewayAddress { get; set; }
		//public string HostName { get; set; }

		public double CPU { get; set; }
		public long MemoryUsage { get; set; }
		public long Activations { get; set; }
		public int RecentlyUsedActivations { get; set; }
		//public int SendQueue { get; set; }
		//public int ReceiveQueue { get; set; }
		//public long RequestQueue { get; set; }
		public long SentMessages { get; set; }
		public long ReceivedMessages { get; set; }
		//public bool LoadShedding { get; set; }
		//public long ClientCount { get; set; }
		public long TotalRecvNetworkSilo { get; set; }
		public long TotalSentNetworkSilo { get; set; }
		public long TotalRecvLocalSilo { get; set; }
		public long TotalSentLocalSilo { get; set; }
	}
}
