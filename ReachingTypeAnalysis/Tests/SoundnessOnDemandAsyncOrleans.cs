// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ReachingTypeAnalysis
{
    public partial class Tests
    {
        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestSimpleCallOnDemandOrleans()
        {
            TestSimpleCall(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestRecursionOnDemandOrleans()
        {
            TestRecursion(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestIfOnDemandOrleans()
        {
            TestIf(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestVirtualCallViaSuperClassOnDemandOrleans()
        {
            TestVirtualCallViaSuperClass(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestCallViaInterfaceOnDemandOrleans()
        {
            TestCallViaInterface(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestForLoopOnDemandOrleans()
        {
            TestForLoop(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestFieldAccessOnDemandOrleans()
        {
            TestFieldAccess(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestCallStaticDelegateOnDemandOrleans()
        {
            TestCallStaticDelegate(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestCallInterfaceDelegateOnDemandOrleans()
        {
            TestCallInterfaceDelegate(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestClassesWithSameFieldNameOnDemandOrleans()
        {
            TestClassesWithSameFieldName(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestFieldLoadInCalleeOnDemandOrleans()
        {
            TestFieldLoadInCallee(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestPropertyOnDemandOrleans()
        {
            TestProperty(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }
    }
}
