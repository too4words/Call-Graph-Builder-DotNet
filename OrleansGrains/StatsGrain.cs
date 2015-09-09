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
	}

	[StorageProvider(ProviderName = "AzureStore")]
	public class StatsGrain : Grain<IStatsState>, IStatsGrain
    {
        public override  Task OnActivateAsync()
        {
			Logger.OrleansLogger = this.GetLogger();
            Logger.LogVerbose(this.GetLogger(), "StatsGrain", "OnActivate","Enter");

			this.State.SiloSentMsgs = new Dictionary<string, Dictionary<string, long>>();
			this.State.SiloRecvMsgs = new Dictionary<string, Dictionary<string, long>>();

			Logger.LogVerbose(this.GetLogger(), "StatsGrain", "OnActivate", "Exit");
			return TaskDone.Done;
		}

		public Task RegisterMessage(string message, string senderAddr, string receiverAddr)
		{
			return TaskDone.Done;
		}
		public Task ResetStats()
		{
			this.State.SiloSentMsgs.Clear();
			this.State.SiloRecvMsgs.Clear();
			return TaskDone.Done;
		}
		public Task<Dictionary<string, long>> SiloSentMsgs(string siloAddr)
		{
			Dictionary<string, long> result;
			if(!this.State.SiloSentMsgs.TryGetValue(siloAddr, out result))
			{
				result = new Dictionary<string, long>();
			}
			return Task.FromResult(result);
		}
		public Task<Dictionary<string, long>> SiloRcvMsgs(string siloAddr)
		{
			Dictionary<string, long> result;
			if (!this.State.SiloRecvMsgs.TryGetValue(siloAddr, out result))
			{
				result = new Dictionary<string, long>();
			}
			return Task.FromResult(result);
		}
		public Task<long> SiloLocalMsgs(string siloAddr)
		{
			return Task.FromResult(0L);
		}
		public Task<long> SiloNetworklMsgs(string siloAddr)
		{
			return Task.FromResult(0L);
		}

		public Task<long> TotalSentMsgs(string siloAddr)
		{
			return Task.FromResult(0L);
		}
		public Task<long> TotalRcvMsgs(string siloAddr)
		{
			return Task.FromResult(0L);
		}
	}    

	public static class StatsHelper
	{
		public static Task RegisterMsg(string msg, IGrainFactory grainFactory)
		{
#if COMPUTE_STATS
			var statGrain = grainFactory.GetGrain<IStatsGrain>("Stats");
            var callerAddr = RequestContext.Get("CallerAddr") as string;
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
			if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
			{
				return null;
			}

			var host = Dns.GetHostEntry(Dns.GetHostName());

			return host
				.AddressList
				.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
				.ToString();
		}
	}
}
