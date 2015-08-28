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

namespace WebRole1
{
    public class SubjectExperimentResults : TableEntity
    {
        public DateTime Time { get; set; }
        public int Machines { get; set; }
        public string Subject { get; set; }
        public int Methods { get; set; }
        public int Messages { get; set; }
        public long ElapsedTime { get; set; }
        public string Observations { get; set; }   
    }
    public class QueriesPerSubject : TableEntity
    {
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
    public class AnalysisClient
    {
        CloudTable analysisTimes;
        CloudTable querytimes;
        int machines;
        int methods;
        string subject;
        public AnalysisClient(int machines, int methods, string subject)
        {
            Contract.Assert(subject != null);
            this.machines = machines;
            this.methods = methods;
            this.subject = subject;
        }
        public async Task<CallGraph<MethodDescriptor, LocationDescriptor>> AnalyzeSolutionAsync(string solutionFileName)
        {
            var analyzer = SolutionAnalyzer.CreateFromSolution(solutionFileName);
            var callgraph = await analyzer.AnalyzeAsync(AnalysisStrategyKind.ONDEMAND_ORLEANS);
            return callgraph;
        }
        public async Task<CallGraph<MethodDescriptor, LocationDescriptor>> AnalyzeSourceCodeAsync(string source)
        {
            var analyzer = SolutionAnalyzer.CreateFromSource(source);
            var callgraph = await analyzer.AnalyzeAsync(AnalysisStrategyKind.ONDEMAND_ORLEANS);
            return callgraph;
        }

		//public async Task<CallGraph<MethodDescriptor, LocationDescriptor>> AnalyzeTestAsync(string testFullName)
		public async Task<SubjectExperimentResults> AnalyzeTestAsync()
        {
            string testFullName = this.subject;

            var stopWatch = Stopwatch.StartNew();
            //var source = BasicTestsSources.Test[testFullName];
			var analyzer = SolutionAnalyzer.CreateFromTest(testFullName);
			/// var callgraph = await analyzer.AnalyzeAsync(AnalysisStrategyKind.ONDEMAND_ORLEANS);
			await analyzer.AnalyzeOnDemandOrleans();
            stopWatch.Stop();
            var time = DateTime.Now;
            var results = new SubjectExperimentResults()
            {
                Time = time,
                Subject = testFullName,
                Machines = machines,
                Methods = methods,
                Messages = SolutionAnalyzer.MessageCounter,
                ElapsedTime = stopWatch.ElapsedMilliseconds,
                Observations = "From web",
                PartitionKey = testFullName,
                RowKey = time.ToFileTime().ToString()
            };
            this.AddSubjetResults(results);
            this.SolutionManager = analyzer.SolutionManager;
			return results;
        }

        internal async Task<Tuple<long, long, long>> ComputeRandomQueries(string className, string methodPrejix, int repetitions)
        {
            var solutionManager = this.SolutionManager; 
            Random random = new Random();
            long sumTime = 0;
            long maxTime = 0;
            long minTime = long.MaxValue;
			long[] times = new long[repetitions];

            for (int i = 0; i < repetitions; i++)
            {
                int methodNumber = random.Next(this.methods) + 1;
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
                    Time = time,
                    Subject = this.subject,
                    Machines = machines,
                    Repeticions = repetitions,
                    AvgTime = avgTime,
                    MinTime  = minTime,
                    MaxTime = maxTime,
					Median = times[repetitions / 2],
                    Observations = "From web",
                    PartitionKey = this.subject,
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
    }
}