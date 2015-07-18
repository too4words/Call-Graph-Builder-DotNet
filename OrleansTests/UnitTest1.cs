using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orleans.TestingHost;
using ReachingTypeAnalysis;
using OrleansInterfaces;

namespace OrleansTests
{
    [TestClass]
    public partial class Tests : TestingSiloHost
    {
        [ClassCleanup]
        public static void ClassCleanup()
        {
            // Optional. 
            // By default, the next test class which uses TestignSiloHost will
            // cause a fresh Orleans silo environment to be created.
            StopAllSilos();
        }

        [TestMethod]
        public void TestAAAAAAAAA()
        {
            var solutionGrain = SolutionGrainFactory.GetGrain("Solution");
            solutionGrain.SetSolutionPath("test").Wait();
        }
    }

}
