using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orleans.Runtime.Host;
using ReachingTypeAnalysis;
using SolutionTraversal.CallGraph;
using System.Threading.Tasks;
using TestSources;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure;
using System.Diagnostics.Contracts;
using Orleans.Runtime;
using Orleans;
using ReachingTypeAnalysis.Analysis;
using System.Data;
using System.IO;
using OrleansInterfaces;
using Microsoft.WindowsAzure.Storage.Table.Protocol;
using System.Threading;
using System.Text;

namespace ReachingTypeAnalysis.Statistics
{
	internal class SiloComputedStats
	{
		public long TotalRecvNetworkSilo { get; set; }
		public long TotalSentLocalSilo { get; set; }
		public long TotalSentNetworkSilo { get; set; }
		public long TotalRecvLocalSilo  { get; set; }
		public long TotalActivations { get; set; }
		public long TotalDeactivations { get; set; }

		public long MemoryUsage { get; set; }
        public long TotalClientMessages { get; internal set; }
    }

    public class AnalysisClient
    {
		private const long SYSTEM_MANAGEMENT_ID = 1;
		private IManagementGrain systemManagement;
		private CloudTable analysisTimes;
		private CloudTable querytimes;
		private CloudTable siloMetrics;

        private int machines;
        //private int methods;
        private string subject;

		private Stopwatch stopWatch;

		private SolutionAnalyzer analyzer;
        private ISolutionGrain solutionManager; 
		public string ExpID { get; private set; }

		public AnalysisClient(SolutionAnalyzer analyzer, int machines, string subject = "")
		{
			this.analyzer = analyzer;
            // We want to force the manager to be obtained when is ready
            // this.solutionManager = analyzer.SolutionManager;
			this.machines = machines;
			this.subject = subject;
		}
        /// <summary>
        /// This auxiliary constructor is used only then a SolutionManager (e.g. SolutionGrain, already exists)
        /// This is the case when we want to query an existing system
        /// </summary>
        /// <param name="solutionManager"></param>
        /// <param name="machines"></param>
        /// <param name="subject"></param>
        public AnalysisClient(ISolutionGrain solutionManager, int machines, string subject = "")
        {
            this.analyzer = null;
            this.solutionManager = solutionManager;
            this.machines = machines;
            this.subject = subject;
        }

        public ISolutionGrain SolutionManager
		{
			get {
                if(this.solutionManager==null)
                {
                    this.solutionManager = (ISolutionGrain)analyzer.SolutionManager;
                }
                return this.solutionManager;
            }
		}

		//public async Task<CallGraph<MethodDescriptor, LocationDescriptor>> AnalyzeTestAsync(string testFullName)
		public async Task<SubjectExperimentResults> RunExperiment(IGrainFactory grainFactory, string expId = "DummyExperimentID")
        {
			this.ExpID = expId;

            string testFullName = this.subject;

	
			// await systemManagement.ForceActivationCollection(System.TimeSpan.MaxValue);

			var myStatsGrain = StatsHelper.GetStatGrain(grainFactory);
			await myStatsGrain.ResetStats();

            this.stopWatch = Stopwatch.StartNew();

			await this.analyzer.AnalyzeOnDemandOrleans();
    
			this.stopWatch.Stop();
   
			var totalRecvNetwork = 0L;
			var totalSentLocal = 0L;
			var totalSentNetwork = 0L;
			var totalRecvLocal = 0L;

            this.systemManagement = grainFactory.GetGrain<IManagementGrain>(SYSTEM_MANAGEMENT_ID);
            await systemManagement.ForceGarbageCollection(null);
            var orleansStats = await systemManagement.GetRuntimeStatistics(null);
            var hosts = await systemManagement.GetHosts();
            //var silos = hosts.Keys.ToArray();


            var totalAct = 0L;
			var totalDeact = 0L;
			var time = DateTime.Now;

            var acummulatedPerSiloMemoryUsage = 0L;
            var maxPerSiloMemoryUsage = 0L;
            var acummulatedPerSiloCPUUsage = 0D;
            var maxPerSiloCPUUsage = 0D;

            //this.methods = -1;

            // var messageMetric = new MessageMetrics();			

            var silos = (await  myStatsGrain.GetSilos()).ToArray();

			var siloComputedStats = new SiloComputedStats[silos.Length];
						
			for (int i = 0; i < silos.Length; i++)
			{
				var silo = silos[i];
				var addrString = silo; /*/silo.Endpoint.Address.ToString();*/
				siloComputedStats[i] = new SiloComputedStats();
                
                siloComputedStats[i].TotalSentLocalSilo += await myStatsGrain.GetSiloLocalMsgs(addrString);
                siloComputedStats[i].TotalRecvLocalSilo += await myStatsGrain.GetSiloLocalMsgs(addrString);

                siloComputedStats[i].TotalSentNetworkSilo += await myStatsGrain.GetSiloNetworkSentMsgs(addrString);
				siloComputedStats[i].TotalRecvNetworkSilo += await myStatsGrain.GetSiloNetworkReceivedMsgs(addrString);
				siloComputedStats[i].MemoryUsage += await myStatsGrain.GetSiloMemoryUsage(addrString);

				var activationDic = await myStatsGrain.GetActivationsPerSilo(addrString);
				var deactivationDic = await myStatsGrain.GetDeactivationsPerSilo(addrString);
				var activations = activationDic.Sum(items => items.Value);
				var deactivations = deactivationDic.Sum(items => items.Value);
				siloComputedStats[i].TotalActivations += activations;
				siloComputedStats[i].TotalDeactivations += deactivations;
                siloComputedStats[i].TotalClientMessages += await myStatsGrain.GetTotalClientMsgsPerSilo(addrString);		
				// totalAct += orleansStats[i].ActivationCount;
				totalAct += activations;
				totalDeact += deactivations;

				//AddSiloMetric(silos[i], siloComputedStats[i], time, machines);
				// Save results in per silo table
                if(orleansStats.Length<=i)
                {
                    throw new IndexOutOfRangeException(String.Format("OrlenasStats Lenght is {0} and silos Lenght is {1}", orleansStats.Length, silos.Length));
                }
				AddSiloMetricWithOrleans(silos[i], orleansStats[i], siloComputedStats[i], time, machines);


				totalSentNetwork += siloComputedStats[i].TotalSentNetworkSilo; 
				totalRecvNetwork += siloComputedStats[i].TotalRecvNetworkSilo;

				totalSentLocal += siloComputedStats[i].TotalSentLocalSilo;
				totalRecvLocal += siloComputedStats[i].TotalSentLocalSilo;

                acummulatedPerSiloMemoryUsage += orleansStats[i].MemoryUsage;
                acummulatedPerSiloCPUUsage += orleansStats[i].CpuUsage;
                if(maxPerSiloMemoryUsage<orleansStats[i].MemoryUsage)
                {
                    maxPerSiloMemoryUsage = orleansStats[i].MemoryUsage;
                }
                if(maxPerSiloCPUUsage<orleansStats[i].CpuUsage)
                {
                    maxPerSiloCPUUsage = orleansStats[i].CpuUsage;
                }
			}
			
			var avgLatency = await myStatsGrain.GetAverageLatency();
            var maxLatency = await myStatsGrain.GetMaxLatency();
            var maxLatencyMsg = await myStatsGrain.GetMaxLatencyMsg();

            var totalMessages = await myStatsGrain.GetTotalMessages();
            var clientMessages = await myStatsGrain.GetTotalClientMessages();
            var methods = await this.SolutionManager.GetReachableMethodsCountAsync();

            var results = new SubjectExperimentResults()
            {
				ExpID = expId,
                Time = time,
                Subject = testFullName,
                Machines = machines,
                Methods = methods,
                Messages = totalMessages,
                ClientMessages =  clientMessages, // SolutionAnalyzer.MessageCounter,
                ElapsedTime = stopWatch.ElapsedMilliseconds,
				Activations = totalAct,
				Deactivations = totalDeact,
                Observations = "From web",
				PartitionKey = expId + " " + testFullName, //  + " " + time.ToFileTime().ToString(),
                RowKey = testFullName +" "+ time.ToFileTime().ToString(),
				TotalRecvNetwork = totalRecvNetwork,
				TotalSentLocal = totalSentLocal,
				TotalSentNetwork = totalSentNetwork,
				TotalRecvLocal = totalRecvLocal,
				AverageLatency = avgLatency,
                MaxLatency = maxLatency,
                MaxLatencyMsg = maxLatencyMsg,
                AveragePerSiloMemoryUsage = acummulatedPerSiloMemoryUsage/ silos.Length,
                AveragePerSiloCPUUsage = acummulatedPerSiloCPUUsage / silos.Length,
                MaxPerSiloMemoryUsage = maxPerSiloMemoryUsage,
                MaxPerSiloCPUUsage = maxPerSiloCPUUsage
            };

			// Save results in main table
			this.AddSubjetResults(results);

			//SaveResults(@"Y:\");

			return results;
		}

		public static void SaveResults(string path)
		{
			SaveTable<SubjectExperimentResults>("AnalysisResults", path);
			SaveTable<SiloRuntimeStats>("SiloMetrics",path);
		}

		public static async Task PerformDeactivation(IGrainFactory grainFactory, ISolutionGrain solutionGrain)
		{
			//var solutionGrain = this.SolutionManager as ISolutionGrain;
			await solutionGrain.ForceDeactivation();

			//var systemManagement = grainFactory.GetGrain<IManagementGrain>(SYSTEM_MANAGEMENT_ID);
			//await systemManagement.ForceActivationCollection(new System.TimeSpan(0,0,5));

			// EmptyTable("OrleansGrainState");


		}

		private static string GetAddressFromStat(string statValue, string statPrefix)
		{
			var result = statValue.Substring(statPrefix.Length,statValue.IndexOf(':')-statPrefix.Length);
			return result;
		}
		private static Tuple<string,string> GetAddressesFromStat(string statValue, string statPrefix)
		{
			var addrTo= statValue.Substring(statPrefix.Length, statValue.IndexOf(':') - statPrefix.Length);
			var pos = statValue.IndexOf(".From.")+7;
			var addrFrom = statValue.Substring(pos, statValue.IndexOf(':',pos) - pos);
			return new Tuple<string,string>(addrTo,addrFrom);
		}


		public static async Task<string> PrintGrainStatistics(IGrainFactory grainFactory)
		{
			var systemManagement = grainFactory.GetGrain<IManagementGrain>(SYSTEM_MANAGEMENT_ID);
			await systemManagement.ForceGarbageCollection(null);

			var stats = await systemManagement.GetRuntimeStatistics(null);

            var hosts = await systemManagement.GetHosts();
            var silos = hosts.Keys.ToArray();

            var output = new StringBuilder();
            foreach (var s in stats)
				output.AppendFormat("Act;{0};  Mem;{1}; CPU;{2}; Rec;{3}; Sent;{4} \n", s.ActivationCount, s.MemoryUsage / 1024, s.CpuUsage,
					s.ReceiveQueueLength, s.SendQueueLength);
            return output.ToString();
		}
		
		private static SiloAddress ParseSilo(string s)
		{
			return SiloAddress.FromParsableString(s);
		}

        public async Task<Tuple<long, long, long>> ComputeRandomQueries(string className, string methodPrejix, int numberOfMethods, int repetitions, string assemblyName = "MyProject", string expId = "DummyExperimentID")
        {
            if(String.IsNullOrEmpty(this.ExpID))
            {
                this.ExpID = expId;
            }

            var solutionManager = this.SolutionManager; 
            Random random = new Random();
            long sumTime = 0;
            long maxTime = 0;
            long minTime = long.MaxValue;
			long[] times = new long[repetitions];

            for (int i = 0; i < repetitions; i++)
            {
                int methodNumber = random.Next(numberOfMethods) + 1;
                var typeDescriptor = new TypeDescriptor("", className, assemblyName);
                var methodDescriptor = new MethodDescriptor(typeDescriptor, methodPrejix + methodNumber, true);
                var invocationCount = await CallGraphQueryInterface.GetInvocationCountAsync(solutionManager, methodDescriptor);

                if (invocationCount > 0)
                {
                    var invocation = random.Next(invocationCount) + 1;

                    IEnumerable<MethodDescriptor> result = null;

                    var stopWatch = Stopwatch.StartNew();
                    if (invocation > 0)
                    {
                        result = await CallGraphQueryInterface.GetCalleesAsync(solutionManager, methodDescriptor, invocation, "");
                    }
                    else
                    {
                        result = await CallGraphQueryInterface.GetCalleesAsync(solutionManager, methodDescriptor);
                    }

                    stopWatch.Stop();
                    var time = stopWatch.ElapsedMilliseconds;
					times[i] = time;
                    if (time > maxTime) maxTime = time;
                    if (time < minTime) minTime = time;
                    sumTime += time;
                }
            }
            if (repetitions > 0)
            {
                var avgTime = sumTime / repetitions;

                var time = DateTime.Now;
                var results = new QueriesPerSubject()
                {
					ExpID = this.ExpID,
                    Time = time,
                    Subject = this.subject,
                    Machines = machines,
                    Repeticions = repetitions,
                    AvgTime = avgTime,
                    MinTime  = minTime,
                    MaxTime = maxTime,
					Median = times[repetitions / 2],
                    Observations = "From web",
                    PartitionKey = this.ExpID,
                    RowKey = time.ToFileTime().ToString()
                };
                this.AddQueryResults(results);
				// SaveTable<QueriesPerSubject>("QueryResults", @"Y:\");

                return Tuple.Create<long, long, long>(avgTime, minTime, maxTime);
            }
            return Tuple.Create<long, long, long>(0, 0, 0);

        }

        public async Task<Tuple<long, long, long>> ComputeRandomQueries(int repetitions, string expId = "DummyExperimentID")
        {
            if (String.IsNullOrEmpty(this.ExpID))
            {
                this.ExpID = expId;
            }

            var solutionManager = this.SolutionManager;
            Random random = new Random();
            long sumTime = 0;
            long maxTime = 0;
            long minTime = long.MaxValue;
            long[] times = new long[repetitions];

            var numberOfMethods = await solutionManager.GetReachableMethodsCountAsync();

            var warmingUpQueries = 10;

            for (int i = 0; i < repetitions+ warmingUpQueries; i++)
            {
                int methodNumber = random.Next(numberOfMethods);
                var methodDescriptor = await solutionManager.GetMethodDescriptorByIndexAsync(methodNumber);
                //var typeDescriptor = new TypeDescriptor("", className, assemblyName);
                //var methodDescriptor = new MethodDescriptor(typeDescriptor, methodPrejix + methodNumber, true);
                var invocationCount = await CallGraphQueryInterface.GetInvocationCountAsync(solutionManager, methodDescriptor);

                if (invocationCount > 0)
                {
                    var invocation = random.Next(invocationCount) + 1;

                    IEnumerable<MethodDescriptor> result = null;

                    var stopWatch = Stopwatch.StartNew();
                    if (invocation > 0)
                    {
                        result = await CallGraphQueryInterface.GetCalleesAsync(solutionManager, methodDescriptor, invocation, "");
                    }
                    else
                    {
                        result = await CallGraphQueryInterface.GetCalleesAsync(solutionManager, methodDescriptor);
                    }

                    stopWatch.Stop();
                    if (i >= warmingUpQueries)
                    {
                        var time = stopWatch.ElapsedMilliseconds;
                        times[i - warmingUpQueries] = time;
                        if (time > maxTime) maxTime = time;
                        if (time < minTime) minTime = time;
                        sumTime += time;
                    }
                }
            }
            if (repetitions > 0)
            {
                var avgTime = sumTime / repetitions;
                var stdDev = 0D;
                for (var i =0; i<times.Length;i++)
                {
                    stdDev +=  (times[i] - avgTime)*(times[i] - avgTime);
                }
                stdDev = Math.Sqrt(stdDev / repetitions);
                var time = DateTime.Now;
                var results = new QueriesPerSubject()
                {
                    ExpID = this.ExpID,
                    Time = time,
                    Subject = this.subject,
                    Machines = machines,
                    Repeticions = repetitions,
                    AvgTime = avgTime,
                    MinTime = minTime,
                    MaxTime = maxTime,
                    StdDev = stdDev,
                    Median = times[repetitions / 2],
                    Observations = "From web",
                    PartitionKey = this.ExpID,
                    RowKey = time.ToFileTime().ToString()
                };
                this.AddQueryResults(results);
                // SaveTable<QueriesPerSubject>("QueryResults", @"Y:\");

                return Tuple.Create<long, long, long>(avgTime, minTime, maxTime);
            }
            return Tuple.Create<long, long, long>(0, 0, 0);

        }



        private static CloudTable CreateTable(string name)
		{
			CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));

			// Create the table client.
			CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

			// Create the table if it doesn't exist.
			CloudTable table = tableClient.GetTableReference(name);
			table.CreateIfNotExists();
			
			return table;
		}

		private static void SaveTable<T>(string name, string path="")  where T: TableEntityCSV, new()
		{
			CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));

			TableQuery<T> query = new TableQuery<T>()
			{
			};
			CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
			// Create the table if it doesn't exist.
			CloudTable table = tableClient.GetTableReference(name);

			var entities = table.ExecuteQuery<T>(query).ToList();
			var outputFile = Path.Combine(path,name)+".csv";
			using (var output = new StreamWriter(outputFile))
			{
				if (entities.Count > 0)
				{
					output.Write(entities.First().GetHeaders()+"\n");
					foreach (var entity in entities)
					{
						output.Write(entity.ToDelimited() + "\n");
					}
				}
			}
		}
        public static string TableToCvs<T>(string name) where T : TableEntityCSV, new()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));

            TableQuery<T> query = new TableQuery<T>()
            {
            };
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            // Create the table if it doesn't exist.
            CloudTable table = tableClient.GetTableReference(name);

            var entities = table.ExecuteQuery<T>(query).ToList();

            var output = new StringBuilder();
            {
                if (entities.Count > 0)
                {
                    output.Append(entities.First().GetHeaders() + "\n");
                    foreach (var entity in entities)
                    {
                        output.Append(entity.ToDelimited() + "\n");
                    }
                }
            }
            return output.ToString();
        }

        public static CloudTable EmptyTable(string name)
		{
			CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
			// Create the table client.
			CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
			// Create the table if it doesn't exist.
			CloudTable table = tableClient.GetTableReference(name);
			table.DeleteIfExists();

			SafeCreateIfNotExists(table);
			// table.CreateIfNotExists();
			return table;
		}


		public static bool SafeCreateIfNotExists(CloudTable table, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
		{
			do
			{
				try
				{
					return table.CreateIfNotExists(requestOptions, operationContext);
				}
				catch (StorageException e)
				{
					if ((e.RequestInformation.HttpStatusCode == 409) && (e.RequestInformation.ExtendedErrorInformation.ErrorCode.Equals(TableErrorCodeStrings.TableBeingDeleted)))
							Thread.Sleep(1000);// The table is currently being deleted. Try again until it works.
					else
						throw;
				}
			} while (true);
		}


        internal void AddSubjetResults(SubjectExperimentResults results)
        {
            if (this.analysisTimes==null)
            {
                this.analysisTimes = CreateTable("AnalysisResults");
            }
            TableOperation insertOperation = TableOperation.Insert(results);
            // Execute the insert operation.
            this.analysisTimes.Execute(insertOperation);
        }


		internal void AddSiloMetric(string siloAddr, SiloComputedStats siloComputedStat, DateTime time, int machines)
		{
			var siloStat = new SiloRuntimeStats()
			{
				ExpID = this.ExpID,
				Time = time,
                Machines = machines, 
				Address = siloAddr.ToString(),
				CPU = -1,
				MemoryUsage = siloComputedStat.MemoryUsage,
				//Activations = siloMetric.ActivationCount,
				Activations = siloComputedStat.TotalActivations,
				RecentlyUsedActivations = -1, // siloMetric.RecentlyUsedActivationCount,
				ReceivedMessages = siloComputedStat.TotalRecvLocalSilo + siloComputedStat.TotalRecvNetworkSilo,
				SentMessages = siloComputedStat.TotalSentLocalSilo + siloComputedStat.TotalSentNetworkSilo,
				PartitionKey = this.ExpID,
				RowKey = siloAddr.ToString() +":" + time.ToFileTime().ToString(),
				TotalRecvNetworkSilo = siloComputedStat.TotalRecvNetworkSilo,
				TotalSentLocalSilo = siloComputedStat.TotalSentLocalSilo,
				TotalSentNetworkSilo = siloComputedStat.TotalSentNetworkSilo,
				TotalRecvLocalSilo = siloComputedStat.TotalRecvLocalSilo
			};

			if (this.siloMetrics == null)
			{
				this.siloMetrics = CreateTable("SiloMetrics");
			}
			TableOperation insertOperation = TableOperation.Insert(siloStat);
			// Execute the insert operation.
			this.siloMetrics.Execute(insertOperation);
		}


		internal void AddSiloMetricWithOrleans(/*SiloAddress*/ string siloAddr,  SiloRuntimeStatistics siloMetric, SiloComputedStats siloComputedStat, DateTime time, int machines)
		{
			var siloStat = new SiloRuntimeStats()
			{
                ExpID = this.ExpID,
				Time = time,
                Machines = machines,
				Address = siloAddr.ToString(), 
				CPU = siloMetric.CpuUsage,
				MemoryUsage = siloMetric.MemoryUsage,
				//Activations = siloMetric.ActivationCount,
				Activations = siloComputedStat.TotalActivations,
				RecentlyUsedActivations = siloMetric.RecentlyUsedActivationCount,
				ReceivedMessages = siloComputedStat.TotalRecvLocalSilo+siloComputedStat.TotalRecvNetworkSilo,
				SentMessages = siloComputedStat.TotalSentLocalSilo+siloComputedStat.TotalSentNetworkSilo,
				PartitionKey = this.ExpID ,
                RowKey = siloAddr.ToString() +":" + time.ToFileTime().ToString(),
				TotalRecvNetworkSilo = siloComputedStat.TotalRecvNetworkSilo,
				TotalSentLocalSilo   = siloComputedStat.TotalSentLocalSilo,
				TotalSentNetworkSilo = siloComputedStat.TotalSentNetworkSilo,
				TotalRecvLocalSilo   = siloComputedStat.TotalRecvLocalSilo, 
                TotalClientMsg = siloComputedStat.TotalClientMessages
			};

			if (this.siloMetrics == null)
			{
				this.siloMetrics = CreateTable("SiloMetrics");
			}
			TableOperation insertOperation = TableOperation.Insert(siloStat);
			// Execute the insert operation.
			this.siloMetrics.Execute(insertOperation);
		}

        internal void AddQueryResults(QueriesPerSubject results)
        {
            if (this.querytimes == null)
            {
				this.querytimes = CreateTable("QueryResults");
            }
            TableOperation insertOperation = TableOperation.Insert(results);
            // Execute the insert operation.
            this.querytimes.Execute(insertOperation);
        }

        internal void RetriveInfoFromAnalysis()
        {
            var table = this.analysisTimes;

            // Construct the query operation for all customer entities where PartitionKey="Smith".
            var query = new TableQuery<SubjectExperimentResults>();
            // .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "Smith"));

            // Print the fields for each customer.
            foreach (var entity in table.ExecuteQuery(query))
            {
                Trace.TraceInformation("{0}, {1}\t{2}\t{3}", entity.Subject, entity.Methods, entity.ElapsedTime, entity.Messages);
            }
        }

		public async Task<CallGraph<MethodDescriptor, LocationDescriptor>> Analyze()
		{
			//var analyzer = SolutionAnalyzer.CreateFromSolution(solutionFileName);
			await analyzer.AnalyzeAsync(AnalysisStrategyKind.ONDEMAND_ORLEANS);
			var callgraph = await analyzer.GenerateCallGraphAsync();
			return callgraph;
		}

		/*
		public async Task<SubjectExperimentResults> RunExperimentOld(IGrainFactory grainFactory, string expId = "Edgar")
        {
			this.ExpID = expId;

            string testFullName = this.subject;

			this.systemManagement = grainFactory.GetGrain<IManagementGrain>(SYSTEM_MANAGEMENT_ID);
			var hosts = await systemManagement.GetHosts();
			var silos = hosts.Keys.ToArray();
			// await systemManagement.ForceActivationCollection(System.TimeSpan.MaxValue);

            this.stopWatch = Stopwatch.StartNew();

			await this.analyzer.AnalyzeOnDemandOrleans();
    
			this.stopWatch.Stop();

			await systemManagement.ForceGarbageCollection(silos);
			var stats = await systemManagement.GetRuntimeStatistics(silos);

			var totalRecvNetwork = 0L;
			var totalSentLocal = 0L;
			var totalSentNetwork = 0L;
			var totalRecvLocal = 0L;


			var totalAct = 0;

			//this.methods = -1;
			
            var messageMetric = new MessageMetrics();
			
			var time = DateTime.Now;
			

			var siloNetworkStats = new SiloNetworkStats[silos.Length];

			for (int i = 0; i < silos.Length; i++)
			{
				var silo = silos[i];
				var addrString = silo.Endpoint.Address.ToString();
				siloNetworkStats[i] = new SiloNetworkStats();

				foreach (var item in messageMetric.PerSiloReceiveCounters)
				{
					var recCounterAddr = GetAddressFromStat(item.Key, "Messaging.Received.Messages.From.S");
					if (recCounterAddr.Equals(silo.Endpoint.Address.ToString()))
					{
						siloNetworkStats[i].TotalRecvLocalSilo += item.Value;
					}
					else
					{
						siloNetworkStats[i].TotalRecvNetworkSilo += item.Value;
					}
				}
				foreach (var item in messageMetric.PerSiloSendCounters)
				{
					var sentCounterAddr = GetAddressFromStat(item.Key, "Messaging.Sent.Messages.To.S");
					if (sentCounterAddr.Equals(silo.Endpoint.Address.ToString()))
					{
						siloNetworkStats[i].TotalSentLocalSilo += item.Value;
					}
					else
					{
						siloNetworkStats[i].TotalSentNetworkSilo += item.Value;
					}
				}

				totalAct += stats[i].ActivationCount;
				AddSiloMetric(silos[i], stats[i], siloNetworkStats[i], time);

				totalRecvNetwork += siloNetworkStats[i].TotalRecvNetworkSilo;
				totalSentLocal += siloNetworkStats[i].TotalSentLocalSilo;
				totalSentNetwork += siloNetworkStats[i].TotalSentNetworkSilo;
				totalRecvLocal += siloNetworkStats[i].TotalRecvLocalSilo;
			}
          
			
            var results = new SubjectExperimentResults()
            {
				ExpID = expId,
                Time = time,
                Subject = testFullName,
                Machines = machines,
                Methods = -1,
                Messages = SolutionAnalyzer.MessageCounter,
                ElapsedTime = stopWatch.ElapsedMilliseconds,
				Activations = totalAct,
                Observations = "From web",
				PartitionKey = expId +" "+ testFullName,
                RowKey = time.ToFileTime().ToString(),
				TotalRecvNetwork = totalRecvNetwork,
				TotalSentLocal   = totalSentLocal,
				TotalSentNetwork = totalSentNetwork,
				TotalRecvLocal   = totalRecvLocal 
            };
            
		
			this.AddSubjetResults(results);

			return results;
		}
		*/

    }
}