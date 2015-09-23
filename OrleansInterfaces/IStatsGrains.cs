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
        Task RegisterMessage(string message, string senderAddr, string receiverAddr, bool isClient, double timeDiff);
		Task ResetStats();
		Task<Dictionary<string,long>> GetSiloSentMsgs(string siloAddr);
		Task<Dictionary<string,long>> GetSiloReceivedMsgs(string siloAddr);
		Task<long>  GetSiloLocalMsgs(string siloAddr);
		Task<long>  GetSiloNetworkSentMsgs(string siloAddr);
		Task<long> GetSiloNetworkReceivedMsgs(string siloAddr);
		Task<long>  GetTotalSentMsgs(string siloAddr);
		Task<long>  GetTotalReceivedMsgs(string siloAddr);
		Task RegisterActivation(string grainClass, string calleeAddr);
		Task RegisterDeactivation(string grainClass, string calleeAddr);
		Task<Dictionary<string, long>> GetActivationsPerSilo(string siloAddr);
		Task<Dictionary<string, long>> GetDeactivationsPerSilo(string siloAddr);
		Task<IEnumerable<string>> GetGrainClasses();
		Task<long> GetActivations(string grainClass);
		Task<long> GetDeactivations(string grainClass);
		Task<long> GetTotalMessages();
        Task<long> GetTotalClientMessages();
        Task<long> GetTotalClientMsgsPerSilo(string siloAddr);
        Task<double> GetAverageLatency();
        Task<double> GetMaxLatency();
        Task<string> GetMaxLatencyMsg();
        Task<long> GetSiloMemoryUsage(string addrString);

		Task<IEnumerable<string>> GetSilos();
	}
}
