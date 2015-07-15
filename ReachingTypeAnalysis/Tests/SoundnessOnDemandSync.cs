// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;

namespace ReachingTypeAnalysis
{    
    public partial class Tests
    {
        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestSimpleCallOnDemandSync()
        {
            TestSimpleCall(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestRecursionOnDemandSync()
        {
           TestRecursion(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestIfOnDemandSync()
        {
             TestIf(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestVirtualCallViaSuperClassOnDemandSync()
        {
            TestVirtualCallViaSuperClass(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestCallViaInterfaceOnDemandSync()
        {
            TestCallViaInterface(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestForLoopOnDemandSync()
        {
            TestForLoop(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestFieldAccessOnDemandSync()
        {
           TestFieldAccess(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestCallStaticDelegateOnDemandSync()
        {
            TestCallStaticDelegate(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestCallInterfaceDelegateOnDemandSync()
        {
            TestCallInterfaceDelegate(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestClassesWithSameFieldNameOnDemandSync()
        {
            TestClassesWithSameFieldName(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestFieldLoadInCalleeOnDemandSync()
        {
            TestFieldLoadInCallee(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestPropertyOnDemandSync()
        {
            TestProperty(AnalysisStrategyKind.ONDEMAND_SYNC);
        }        
    }
}
