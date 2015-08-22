using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using ReachingTypeAnalysis.Communication;
using System.Linq;
using Orleans;
using Orleans.Runtime.Host;
using System.Diagnostics;
using ReachingTypeAnalysis;
using ReachingTypeAnalysis.Analysis;

namespace ReachingTypeAnalysis
{
    public static class CallGraphQueryInterface
    {
        /// <summary>
        /// Same as the next method. We currently ignore the Project parameeter
        /// </summary>
        /// <param name="strategy"></param>
        /// <param name="methodDescriptor"></param>
        /// <param name="invocationPosition"></param>
        /// <param name="projectName"></param>
        /// <returns></returns>
        public static Task<ISet<MethodDescriptor>> GetCalleesAsync(ISolutionManager solutionManager, MethodDescriptor methodDescriptor, int invocationPosition, string projectName)
        {
            return GetCalleesAsync(solutionManager, methodDescriptor, invocationPosition);
        }

        /// These 2 method can work either with Orleans or OndDemandAsync Strategy
        /// <summary>
        /// Return the calless for a given call site
        /// </summary>
        /// <param name="solutionManager"></param>
        /// <param name="methodDescriptor"></param>
        /// <param name="invocationPosition"></param>
        /// <returns></returns>
        public static async Task<ISet<MethodDescriptor>> GetCalleesAsync(ISolutionManager solutionManager, MethodDescriptor methodDescriptor, int invocationPosition)
        {
            var totalStopWatch = Stopwatch.StartNew();
            var stopWatch = Stopwatch.StartNew();

            var entityWithPropagator = await solutionManager.GetMethodEntityAsync(methodDescriptor);
            Meausure("GetMethodEntityProp", stopWatch);

            var result = await entityWithPropagator.GetCalleesAsync(invocationPosition);
            Meausure("entProp.GetCalleesAsync", stopWatch);

            Meausure("Total GetCalleesAsync", totalStopWatch);
            return result;
        }

		public static async Task<ISet<MethodDescriptor>> GetCalleesAsync(ISolutionManager solutionManager, MethodDescriptor methodDescriptor)
		{
			var totalStopWatch = Stopwatch.StartNew();
			var stopWatch = Stopwatch.StartNew();

			var entityWithPropagator = await solutionManager.GetMethodEntityAsync(methodDescriptor);
			Meausure("GetMethodEntityProp", stopWatch);

			var result = await entityWithPropagator.GetCalleesAsync();

			Meausure("entProp.GetCalleesAsync", stopWatch);

			Meausure("Total GetCalleesAsync", totalStopWatch);
			return result;
		}

        /// <summary>
        ///  Return the numnber of calls sites for a 
        /// </summary>
        /// <param name="methodDescriptor"></param>
        /// <param name="projectName"></param>
        /// <returns></returns>
        public static async Task<int> GetInvocationCountAsync(ISolutionManager solutionManager, MethodDescriptor methodDescriptor)
        {
            var totalStopWatch = Stopwatch.StartNew();
            var stopWatch = Stopwatch.StartNew();

			var entityWithPropagator = await solutionManager.GetMethodEntityAsync(methodDescriptor);
            Meausure("GetMethodEntityProp", stopWatch);

            var result = await entityWithPropagator.GetInvocationCountAsync();
            Meausure("entProp.GetInvocationCountAsync", stopWatch);

            Meausure("Total GetInvocationCountAsync", totalStopWatch);
            return result;
        }

        private static void Meausure(string label, Stopwatch timer)
        {
            timer.Stop();
            var timeMS = timer.ElapsedMilliseconds;
            var ticks = timer.ElapsedTicks;
			if (GrainClient.IsInitialized)
			{
				Orleans.Runtime.Logger log = GrainClient.Logger;

				Logger.LogVerbose(log, "", "Measure", "{0}: {1} ms, {2} ticks", label, timeMS, ticks);
			}

            Debug.WriteLine("{0}: {1} ms, {2} ticks", label, timeMS, ticks);
            timer.Reset();
            timer.Start();
        }

        /// <summary>
        /// Compute all the calless of this method entities
        /// </summary>
        /// <returns></returns>
		//internal static async Task<ISet<MethodDescriptor>> GetCalleesAsync(MethodEntity methodEntity, IProjectCodeProvider codeProvider)
		//{
		//	var result = new HashSet<MethodDescriptor>();

		//	foreach (var callNode in methodEntity.PropGraph.CallNodes)
		//	{
		//		result.UnionWith(await GetCalleesAsync(methodEntity, callNode, codeProvider));
		//	}

		//	return result;
		//}

        /// <summary>
        /// Computes all the potential callees for a particular method invocation
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        internal static async Task<ISet<MethodDescriptor>> GetCalleesAsync(MethodEntity methodEntity, AnalysisCallNode node, IProjectCodeProvider codeProvider)
        {
            var stopWatch = Stopwatch.StartNew();

            ISet<MethodDescriptor> result;
            var calleesForNode = new HashSet<MethodDescriptor>();
            var invExp = methodEntity.PropGraph.GetInvocationInfo((AnalysisCallNode)node);

            Meausure("ME.PG.GetInvocationInfo", stopWatch);

            Contract.Assert(invExp != null);
            Contract.Assert(codeProvider != null);

            var calleeResult = await methodEntity.PropGraph.ComputeCalleesForNodeAsync(invExp, codeProvider);

            Meausure("ME.PG.ComputeCalleesForNode", stopWatch);

            calleesForNode.UnionWith(calleeResult);

            //calleesForNode.UnionWith(invExp.ComputeCalleesForNode(this.MethodEntity.PropGraph,this.codeProvider));

            result = calleesForNode;
            return result;
        }

        /// <summary>
        /// Generates a dictionary invocationNode -> potential callees
        /// This is used for example by the demo to get the caller / callee info
        /// </summary>
        /// <returns></returns>
        internal static async Task<IDictionary<AnalysisCallNode, ISet<MethodDescriptor>>> GetCalleesInfo(MethodEntity methodEntity, IProjectCodeProvider codeProvider)
        {
            var calleesPerEntity = new Dictionary<AnalysisCallNode, ISet<MethodDescriptor>>();

            foreach (var calleeNode in methodEntity.PropGraph.CallNodes)
            {
                calleesPerEntity[calleeNode] = await GetCalleesAsync(methodEntity, calleeNode, codeProvider);
            }

            return calleesPerEntity;
        }
    }
}
