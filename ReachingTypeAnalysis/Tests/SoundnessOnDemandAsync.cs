// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ReachingTypeAnalysis
{
    public partial class Tests
    {
        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestSimpleCallOnDemandAsync()
        {
            TestSimpleCall(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestRecursionOnDemandAsync()
        {
            TestRecursion(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestIfOnDemandAsync()
        {
            TestIf(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestVirtualCallViaSuperClassOnDemandAsync()
        {
            TestVirtualCallViaSuperClass(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestCallViaInterfaceOnDemandAsync()
        {
            TestCallViaInterface(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestForLoopOnDemandAsync()
        {
            TestForLoop(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestFieldAccessOnDemandAsync()
        {
            TestFieldAccess(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestCallStaticDelegateOnDemandAsync()
        {
            TestCallStaticDelegate(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestCallInterfaceDelegateOnDemandAsync()
        {
            TestCallInterfaceDelegate(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestClassesWithSameFieldNameOnDemandAsync()
        {
            TestClassesWithSameFieldName(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestFieldLoadInCalleeOnDemandAsync()
        {
            TestFieldLoadInCallee(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestPropertyOnDemandAsync()
        {
            TestProperty(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }
    }
}
