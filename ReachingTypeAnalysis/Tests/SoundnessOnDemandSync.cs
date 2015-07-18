// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;

namespace ReachingTypeAnalysis
{    
    [TestClass]
    public partial class SyncTests
    {
        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestSimpleCallOnDemandSync()
        {
            BasicTests.TestSimpleCall(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestRecursionOnDemandSync()
        {
            BasicTests.TestRecursion(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestIfOnDemandSync()
        {
            BasicTests.TestIf(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestVirtualCallViaSuperClassOnDemandSync()
        {
            BasicTests.TestVirtualCallViaSuperClass(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestCallViaInterfaceOnDemandSync()
        {
            BasicTests.TestCallViaInterface(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestForLoopOnDemandSync()
        {
            BasicTests.TestForLoop(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestFieldAccessOnDemandSync()
        {
            BasicTests.TestFieldAccess(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestCallStaticDelegateOnDemandSync()
        {
            BasicTests.TestCallStaticDelegate(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestCallInterfaceDelegateOnDemandSync()
        {
            BasicTests.TestCallInterfaceDelegate(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestClassesWithSameFieldNameOnDemandSync()
        {
            BasicTests.TestClassesWithSameFieldName(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestFieldLoadInCalleeOnDemandSync()
        {
            BasicTests.TestFieldLoadInCallee(AnalysisStrategyKind.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestPropertyOnDemandSync()
        {
            BasicTests.TestProperty(AnalysisStrategyKind.ONDEMAND_SYNC);
        }        
    }
}
