using Orleans;
using ReachingTypeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansInterfaces
{
    public interface IStatsGrain : IGrainWithStringKey
    {
        Task RegisterMessage(string message, string senderAddr, string receiverAddr);
		Task ResetStats();
		Task<Dictionary<string,long>> SiloSentMsgs(string siloAddr);
		Task<Dictionary<string,long>> SiloRcvMsgs(string siloAddr);
		Task<long>  SiloLocalMsgs(string siloAddr);
		Task<long>  SiloNetworklMsgs(string siloAddr);
		Task<long>  TotalSentMsgs(string siloAddr);
		Task<long>  TotalRcvMsgs(string siloAddr);
	}
}
