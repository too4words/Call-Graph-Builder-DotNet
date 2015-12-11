// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace ReachingTypeAnalysis.Test
{
    public partial class Tests
    {
        [TestMethod]
        [TestCategory("Concurrency")]
        public void TestTaskStart(AnalysisStrategyKind strategy)
        {
            var task = Task<string>.Factory.StartNew(() => { return "hello"; });
            Assert.IsTrue(task.Result.Equals("hello"));
        }

        [TestMethod]
        [TestCategory("Concurrency")]
        public void TestContinueWith(AnalysisStrategyKind strategy)
        {
            var task = Task<string>.Factory.StartNew(() => { return "hello"; });
            task.ContinueWith((t) => t.Result + " ben" );
            task.Wait();
            Assert.IsTrue(task.Result.Equals("hello ben"));
        }

        [TestMethod]
        [TestCategory("Concurrency")]
        public void TestContinueWith2(AnalysisStrategyKind strategy)
        {
            var task = Task<string>.Factory.StartNew(() => { return "hello"; });
            var task2 = task.ContinueWith((t) => t.Result + " ben");
            task2.Wait();
            Assert.IsTrue(task2.Result.Equals("hello ben"));
        }
    }
}
