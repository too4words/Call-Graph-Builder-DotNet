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
	public interface IStatsState : IGrainState
	{
		Dictionary<string,Dictionary<string, long>> SiloSentMsgs { get; set; }
		Dictionary<string,Dictionary<string, long>> SiloRecvMsgs { get; set; }
		
		//Dictionary<string, long> SiloLocalSentMsgs { get; set; }
		//Dictionary<string, long> SiloLocalRecvMsgs { get; set; }

		//Dictionary<string, long> SiloNetworkSentMsgs { get; set; }
		//Dictionary<string, long> SiloLocalRecvMsgs { get; set; }

	}

	[StorageProvider(ProviderName = "AzureStore")]
	public class StatsGrain : Grain<IStatsState>, IStatsGrain
    {
		private Dictionary<string,long> operationCounter;

        public override  Task OnActivateAsync()
        {
			Logger.OrleansLogger = this.GetLogger();
            Logger.LogVerbose(this.GetLogger(), "StatsGrain", "OnActivate","Enter");

			this.State.SiloSentMsgs = new Dictionary<string, Dictionary<string, long>>();
			this.State.SiloRecvMsgs = new Dictionary<string, Dictionary<string, long>>();

			this.operationCounter = new Dictionary<string,long>();

			Logger.LogVerbose(this.GetLogger(), "StatsGrain", "OnActivate", "Exit");
			return TaskDone.Done;
		}

		public Task RegisterMessage(string message, string senderAddr, string receiverAddr)
		{
			AddToMap(this.State.SiloSentMsgs, senderAddr, receiverAddr);
			AddToMap(this.State.SiloRecvMsgs, receiverAddr, senderAddr);

			IncrementCounter(message, this.operationCounter);

			return this.WriteStateAsync();
		}

		private void AddToMap(Dictionary<string, Dictionary<string, long>> silosStatMap, string fromAddr, string toAddr)
		{
			Dictionary<string, long> siloStat = null;
			if(!silosStatMap.TryGetValue(fromAddr,out siloStat))
			{
				siloStat = new Dictionary<string, long>();
				silosStatMap[fromAddr] = siloStat;
			}

			IncrementCounter(toAddr, siloStat);
		}

		private static void IncrementCounter(string key, Dictionary<string, long> counterMap)
		{
			if (counterMap.ContainsKey(key))
			{
				counterMap[key]++;
			}
			else
			{
				counterMap[key] = 0;
			}
		}
		
		public Task ResetStats()
		{
			this.State.SiloSentMsgs.Clear();
			this.State.SiloRecvMsgs.Clear();
			return this.WriteStateAsync();
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
			var siloSent = await GetSiloSentMsgs(siloAddr);
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
			var callerAddr = RequestContext.Get(StatsHelper.CALLER_ADDR_CONTEXT) as string;
            var calleeAddr = GetMyIPAddr();
            return statGrain.RegisterMessage(msg, callerAddr, calleeAddr);
#else
			return TaskDone.Done;
#endif
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
