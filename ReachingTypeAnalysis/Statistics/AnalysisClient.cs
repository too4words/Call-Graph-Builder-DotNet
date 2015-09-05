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

namespace ReachingTypeAnalysis.Statistics
{
    public class AnalysisClient
    {
		private const long SYSTEM_MANAGEMENT_ID = 1;
		private IManagementGrain systemManagement;
		private CloudTable analysisTimes;
		private CloudTable querytimes;
		private CloudTable siloMetrics;

        int machines;
        //int methods;
        string subject;

		private Stopwatch stopWatch;

		private SolutionAnalyzer analyzer;
		public string ExpID { get; private set; }


		public AnalysisClient(SolutionAnalyzer analyzer, int machines)
		{
			this.analyzer = analyzer;
			this.machines = machines;
		}

		//public async Task<CallGraph<MethodDescriptor, LocationDescriptor>> AnalyzeTestAsync(string testFullName)
		public async Task<SubjectExperimentResults> RunExperiment(IGrainFactory grainFactory, string expId = "Edgar")
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

			//this.methods = -1;

			await systemManagement.ForceGarbageCollection(silos);
			var stats = await systemManagement.GetRuntimeStatistics(silos);

			var totalAct = 0;

			for (int i = 0; i < silos.Length; i++)
			{
				totalAct += stats[i].ActivationCount;
				AddSiloMetric(silos[i], stats[i]);
			}

            
			var time = DateTime.Now;
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
                RowKey = time.ToFileTime().ToString()
            };
            
		
			this.AddSubjetResults(results);



            this.SolutionManager = analyzer.SolutionManager;

			return results;
        }

		public async Task PrintGrainStatistics(IGrainFactory grainFactory)
		{
			this.systemManagement = grainFactory.GetGrain<IManagementGrain>(SYSTEM_MANAGEMENT_ID);
			var hosts = await systemManagement.GetHosts();
			var silos = hosts.Keys.ToArray();


			// var stats = await systemManagement.GetSimpleGrainStatistics(silos);
			await systemManagement.ForceGarbageCollection(silos);


			var stats = await systemManagement.GetRuntimeStatistics(silos);

			foreach (var s in stats)
				Console.WriteLine("Act;{0};  Mem;{1}; CPU;{2}; Rec;{3}; Sent;{4} \n", s.ActivationCount, s.MemoryUsage / 1024, s.CpuUsage,
					s.ReceiveQueueLength, s.SendQueueLength);
		}
		
		private static SiloAddress ParseSilo(string s)
		{
			return SiloAddress.FromParsableString(s);
		}


        public async Task<Tuple<long, long, long>> ComputeRandomQueries(string className, string methodPrejix, int numberOfMethods, int repetitions)
        {
            var solutionManager = this.SolutionManager; 
            Random random = new Random();
            long sumTime = 0;
            long maxTime = 0;
            long minTime = long.MaxValue;
			long[] times = new long[repetitions];

            for (int i = 0; i < repetitions; i++)
            {
                int methodNumber = random.Next(numberOfMethods) + 1;
                var methodDescriptor = new MethodDescriptor(className, methodPrejix + methodNumber, true);
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

		private static CloudTable EmptyTable(string name)
		{
			CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));

			// Create the table client.
			CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

			// Create the table if it doesn't exist.
			CloudTable table = tableClient.GetTableReference(name);
			table.DeleteIfExists();
			table.CreateIfNotExists();
			return table;
		}


		
        internal void AddSubjetResults(SubjectExperimentResults results)
        {
            if(this.analysisTimes==null)
            {
                this.analysisTimes = CreateTable("AnalysisResults");
            }
            TableOperation insertOperation = TableOperation.Insert(results);
            // Execute the insert operation.
            this.analysisTimes.Execute(insertOperation);
        }
		internal void AddSiloMetric(SiloAddress siloAddr,  SiloRuntimeStatistics siloMetric)
		{
			var siloStat = new SiloRuntimeStats()
			{
                ExpID = this.ExpID,
				Address = siloAddr.ToString(), 
				CPU = siloMetric.CpuUsage,
				MemoryUsage = siloMetric.MemoryUsage,
				Activations = siloMetric.ActivationCount,
				RecentlyUsedActivations = siloMetric.RecentlyUsedActivationCount,
				SentMessages = 0, // siloMetric.SentMessages,
				ReceivedMessages = 0, // siloMetric.ReceivedMessages,
				PartitionKey = this.ExpID,
                RowKey = siloAddr.ToString()

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


        public ISolutionManager SolutionManager { get; set; }

		public async Task<CallGraph<MethodDescriptor, LocationDescriptor>> Analyze()
		{
			//var analyzer = SolutionAnalyzer.CreateFromSolution(solutionFileName);
			await analyzer.AnalyzeAsync(AnalysisStrategyKind.ONDEMAND_ORLEANS);
			var callgraph = await analyzer.GenerateCallGraphAsync();
			return callgraph;
		}

    }
}