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
		Task<Dictionary<string,long>> GetSiloSentMsgs(string siloAddr);
		Task<Dictionary<string,long>> GetSiloReceivedMsgs(string siloAddr);
		Task<long>  GetSiloLocalMsgs(string siloAddr);
		Task<long>  GetSiloNetworkSentMsgs(string siloAddr);
		Task<long> GetSiloNetworkReceivedMsgs(string siloAddr);
		Task<long>  GetTotalSentMsgs(string siloAddr);
		Task<long>  GetTotalReceivedMsgs(string siloAddr);
	}
}
