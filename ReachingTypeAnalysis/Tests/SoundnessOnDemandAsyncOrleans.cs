// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orleans.TestingHost;
using System;

namespace ReachingTypeAnalysis
{
    [TestClass]
    public partial class OrleansTests : TestingSiloHost
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
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestSimpleCallOnDemandOrleans()
        {
            BasicTests.TestSimpleCall(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestRecursionOnDemandOrleans()
        {
            BasicTests.TestRecursion(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestIfOnDemandOrleans()
        {
            BasicTests.TestIf(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestVirtualCallViaSuperClassOnDemandOrleans()
        {
            BasicTests.TestVirtualCallViaSuperClass(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestCallViaInterfaceOnDemandOrleans()
        {
            BasicTests.TestCallViaInterface(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestForLoopOnDemandOrleans()
        {
            BasicTests.TestForLoop(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestFieldAccessOnDemandOrleans()
        {
            BasicTests.TestFieldAccess(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestCallStaticDelegateOnDemandOrleans()
        {
            BasicTests.TestCallStaticDelegate(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestCallInterfaceDelegateOnDemandOrleans()
        {
            BasicTests.TestCallInterfaceDelegate(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestClassesWithSameFieldNameOnDemandOrleans()
        {
            BasicTests.TestClassesWithSameFieldName(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestFieldLoadInCalleeOnDemandOrleans()
        {
            BasicTests.TestFieldLoadInCallee(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestPropertyOnDemandOrleans()
        {
            BasicTests.TestProperty(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Generated")]
        public void LongGeneratedTestOrleansAsync2()
        {
            BasicTests.LongGeneratedTestAsync2(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }
        [TestMethod]
        [TestCategory("Generated")]
        public void LongGeneratedTestOrleansAsync3()
        {
            BasicTests.LongGeneratedTestAsync3(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }
        [TestMethod]
        [TestCategory("VeryLongRunning")]
        public void LongGeneratedTestOrleansAsync4()
        {
            BasicTests.LongGeneratedTestAsync4(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

    }
}
