// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using Orleans.Providers;
using OrleansInterfaces;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Net;
using TestSources;

namespace ReachingTypeAnalysis.Analysis
{
	public class StatsState  //interface IStatsState : IGrainState
	{
		public Dictionary<string,Dictionary<string, long>> SiloSentMsgs { get; set; }
		public Dictionary<string,Dictionary<string, long>> SiloRecvMsgs { get; set; }
		public Dictionary<string, Dictionary<string, long>> SiloActivations { get; set; }
		public Dictionary<string, Dictionary<string, long>> SiloDeactivations { get; set; }
		public ISet<string> GrainClasses { get; set; }

		//Dictionary<string, long> SiloLocalSentMsgs { get; set; }
		//Dictionary<string, long> SiloLocalRecvMsgs { get; set; }

		//Dictionary<string, long> SiloNetworkSentMsgs { get; set; }
		//Dictionary<string, long> SiloLocalRecvMsgs { get; set; }

	}

	public class LatencyInfo
	{
		public double AccumulattedTimeDifference { get; set; }
		public double MaxLatency  { get; set; }
		public string MaxLatencyMsg { get; set; }
	}
	//[StorageProvider(ProviderName = "AzureStore")]
	//public class StatsGrain : Grain<IStatsState>, IStatsGrain
	public class StatsGrain : Grain, IStatsGrain
    {
		internal  StatsState State;
		private Dictionary<string,long> operationCounter;
		private long messages;
		private LatencyInfo latencyInfo;

		private Task WriteStateAsync()
		{
			return TaskDone.Done;
		}
		private Task ClearStateAsync()
		{
			return TaskDone.Done;
		}


        public override  Task OnActivateAsync()
        {
			this.State = new StatsState();

			Logger.OrleansLogger = this.GetLogger();
            Logger.LogVerbose(this.GetLogger(), "StatsGrain", "OnActivate","Enter");

			this.State.SiloSentMsgs = new Dictionary<string, Dictionary<string, long>>();
			this.State.SiloRecvMsgs = new Dictionary<string, Dictionary<string, long>>();

			this.State.SiloActivations = new Dictionary<string, Dictionary<string, long>>();
			this.State.SiloDeactivations = new Dictionary<string, Dictionary<string, long>>();

			this.State.GrainClasses  = new HashSet<string>();

			this.operationCounter = new Dictionary<string,long>();
			this.messages = 0;

			this.latencyInfo = new LatencyInfo
			{
				AccumulattedTimeDifference = 0,
				MaxLatency = 0,
				MaxLatencyMsg = ""
			};

			Logger.LogVerbose(this.GetLogger(), "StatsGrain", "OnActivate", "Exit");
			return TaskDone.Done;
		}

		public Task RegisterMessage(string message, string senderAddr, string receiverAddr, double timeDiff)
		{
			AddToMap(this.State.SiloSentMsgs, senderAddr, receiverAddr);
			AddToMap(this.State.SiloRecvMsgs, receiverAddr, senderAddr);

			IncrementCounter(message, this.operationCounter);

			this.latencyInfo.AccumulattedTimeDifference += timeDiff;
			if(timeDiff>this.latencyInfo.MaxLatency)
			{
				this.latencyInfo.MaxLatency= timeDiff;
				this.latencyInfo.MaxLatencyMsg= message;
			}
			this.messages++;

			return this.WriteStateAsync();
		}
		public Task RegisterActivation(string grainClass, string calleeAddr)
		{
			AddToMap(this.State.SiloActivations, calleeAddr, grainClass);
			this.State.GrainClasses.Add(grainClass);

			return this.WriteStateAsync();
		}

		public Task RegisterDeactivation(string msg, string calleeAddr)
		{
			AddToMap(this.State.SiloDeactivations, calleeAddr, msg);

			return this.WriteStateAsync();
		}

		

		private void AddToMap(Dictionary<string, Dictionary<string, long>> silosStatMap, string siloAddr, string key)
		{
			Dictionary<string, long> siloStat = null;
			if(!silosStatMap.TryGetValue(siloAddr,out siloStat))
			{
				siloStat = new Dictionary<string, long>();
				silosStatMap[siloAddr] = siloStat;
			}

			IncrementCounter(key, siloStat);
		}

		private static void IncrementCounter(string key, Dictionary<string, long> counterMap)
		{
			if (counterMap.ContainsKey(key))
			{
				counterMap[key]++;
			}
			else
			{
				counterMap[key] = 1;
			}
		}
		
		public async Task ResetStats()
		{
			await this.ClearStateAsync();

			this.State.SiloSentMsgs.Clear();
			this.State.SiloRecvMsgs.Clear();
			this.State.SiloActivations.Clear();
			this.State.SiloDeactivations.Clear();
			this.operationCounter.Clear();
			this.State.GrainClasses.Clear();

			await this.WriteStateAsync();
		}

		public Task<Dictionary<string, long>> GetSiloSentMsgs(string siloAddr)
		{
			Dictionary<string, long> result;
			if(!this.State.SiloSentMsgs.TryGetValue(siloAddr, out result))
			{
				result = new Dictionary<string, long>();
			}
			return Task.FromResult(result);
		}
		public Task<Dictionary<string, long>> GetSiloReceivedMsgs(string siloAddr)
		{
			Dictionary<string, long> result;
			if (!this.State.SiloRecvMsgs.TryGetValue(siloAddr, out result))
			{
				result = new Dictionary<string, long>();
			}
			return Task.FromResult(result);
		}
		public async Task<long> GetSiloLocalMsgs(string siloAddr)
		{
			var siloSent = await this.GetSiloSentMsgs(siloAddr);
			var total = siloSent.Where(item => item.Key.Equals(siloAddr)).Sum(item => item.Value);
			return total;
		}
		public async Task<long> GetSiloNetworkSentMsgs(string siloAddr)
		{
			var siloSent = await GetSiloSentMsgs(siloAddr);
			var total = siloSent.Where(item => !item.Key.Equals(siloAddr)).Sum(item => item.Value);
			return total;
		}

		public async Task<long> GetSiloNetworkReceivedMsgs(string siloAddr)
		{
			var siloRcv = await GetSiloReceivedMsgs(siloAddr);
			var total = siloRcv.Where(item => !item.Key.Equals(siloAddr)).Sum(item => item.Value);
			return total;
		}

		public async Task<long> GetTotalSentMsgs(string siloAddr)
		{
			var siloStat = await GetSiloSentMsgs(siloAddr);
			var total = siloStat.Sum(item => item.Value); ;
			return total;
		}
		public async Task<long> GetTotalReceivedMsgs(string siloAddr)
		{
			var siloStat = await GetSiloReceivedMsgs(siloAddr);
			var total = siloStat.Sum(item => item.Value);
			return total;
		}
		public async Task<long> GetActivations(string grainClass)
		{
			return await SumSiloPerCategory(grainClass, this.State.SiloActivations);
		}

		public Task<Dictionary<string, long>> GetActivationsPerSilo(string siloAddr)
		{
			return GetDictForSilo(siloAddr,this.State.SiloActivations);
		}

		public Task<Dictionary<string, long>> GetDeactivationsPerSilo(string siloAddr)
		{
			return GetDictForSilo(siloAddr,this.State.SiloDeactivations);
		}

		public async Task<long> GetDeactivations(string grainClass)
		{
			return await SumSiloPerCategory(grainClass, this.State.SiloDeactivations);
		}

		public Task<IEnumerable<string>> GetGrainClasses()
		{
			return Task.FromResult(this.State.GrainClasses.AsEnumerable());
		}

		private async Task<long> SumSiloPerCategory(string grainClass,  Dictionary<string, Dictionary<string, long>> silosStatMap)
		{
			var total = 0L;
			foreach (var siloAddr in this.State.SiloActivations.Keys)
			{
				var siloActivations = await GetDictForSilo(siloAddr, silosStatMap);
				var siloTotal = 0L;
				if (siloActivations.TryGetValue(grainClass, out siloTotal))
				{
					total += siloTotal;
				}
			}
			return total;
		}

		private Task<Dictionary<string, long>> GetDictForSilo(string siloAddr, Dictionary<string, Dictionary<string, long>> silosStatMap)
		{
			Dictionary<string, long> result;
			if (!silosStatMap.TryGetValue(siloAddr, out result))
			{
				result = new Dictionary<string, long>();
			}
			return Task.FromResult(result);
		}

		public Task<double> GetAverageLattency()
		{
			return Task.FromResult(this.latencyInfo.AccumulattedTimeDifference / this.messages);
		}

		public Task<long> TotalMessages()
		{
			return Task.FromResult(this.messages);
		}
	}

	[Serializable]
	public class StatsContext
	{
		public string IPAddr { get; set; }
		public DateTime TimeStamp { get; set; }
	}

	public static class StatsHelper
	{
		public const string STATS_GRAIN = "Stats";
		public const string CALLER_ADDR_CONTEXT = "CallerAddr";
		public const string SILO_ADDR = "MyIPAddr";

		public static IStatsGrain GetStatGrain(IGrainFactory grainFactory)
		{
			var statGrain = grainFactory.GetGrain<IStatsGrain>(STATS_GRAIN);
			return statGrain;
		}

		public static Task RegisterMsg(string msg, IGrainFactory grainFactory)
		{
#if COMPUTE_STATS
			var statGrain = GetStatGrain(grainFactory);
			var context = RequestContext.Get(StatsHelper.CALLER_ADDR_CONTEXT) as StatsContext;
			var callerAddr = context.IPAddr;
            var calleeAddr = GetMyIPAddr();

			var timeDiff = DateTime.UtcNow.Subtract(context.TimeStamp).TotalMilliseconds;
            return statGrain.RegisterMessage(msg, callerAddr, calleeAddr, timeDiff);
#else
			return TaskDone.Done;
#endif
		}

		public static Task RegisterActivation(string grainClass, IGrainFactory grainFactory)
		{
#if COMPUTE_STATS
			var statGrain = GetStatGrain(grainFactory);
			var calleeAddr = GetMyIPAddr();
			return statGrain.RegisterActivation(grainClass, calleeAddr);
#else
			return TaskDone.Done;
#endif
		}

		public static Task RegisterDeactivation(string grainClass, IGrainFactory grainFactory)
		{
#if COMPUTE_STATS
			var statGrain = GetStatGrain(grainFactory);
			var calleeAddr = GetMyIPAddr();
			return statGrain.RegisterActivation(grainClass, calleeAddr);
#else
			return TaskDone.Done;
#endif
		}

		public static StatsContext CreateMyIPAddrContext()
		{
			var addr = GetMyIPAddr();
			return new StatsContext()
			{
				IPAddr = addr,
				TimeStamp = DateTime.UtcNow,
			};
		}

		public static string GetMyIPAddr()
		{
			//IPHostEntry host;
			//string localIP = "?";
			//host = Dns.GetHostEntry(Dns.GetHostName());
			//foreach (IPAddress ip in host.AddressList)
			//{
			//	if (ip.AddressFamily.ToString() == "InterNetwork")
			//	{
			//		localIP = ip.ToString();
			//	}
			//}
			//return localIP;

			//RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["YourInternalEndpoint"].IPEndpoint.Address;

			var myIP = Environment.GetEnvironmentVariable(SILO_ADDR);
			if(myIP==null)
			{
				myIP = "Unknown";
			}
			return myIP;

			//if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
			//{
			//	return null;
			//}

			//var host = Dns.GetHostEntry(Dns.GetHostName());

			//return host
			//	.AddressList
			//	.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
			//	.ToString();
		}
	}
}
