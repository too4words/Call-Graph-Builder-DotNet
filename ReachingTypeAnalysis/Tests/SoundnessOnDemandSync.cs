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
            TestSimpleCall(AnalysisStrategy.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestRecursionOnDemandSync()
        {
           TestRecursion(AnalysisStrategy.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestIfOnDemandSync()
        {
             TestIf(AnalysisStrategy.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestVirtualCallViaSuperClassOnDemandSync()
        {
            TestVirtualCallViaSuperClass(AnalysisStrategy.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestCallViaInterfaceOnDemandSync()
        {
            TestCallViaInterface(AnalysisStrategy.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestForLoopOnDemandSync()
        {
            TestForLoop(AnalysisStrategy.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestFieldAccessOnDemandSync()
        {
           TestFieldAccess(AnalysisStrategy.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestCallStaticDelegateOnDemandSync()
        {
            TestCallStaticDelegate(AnalysisStrategy.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestCallInterfaceDelegateOnDemandSync()
        {
            TestCallInterfaceDelegate(AnalysisStrategy.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestClassesWithSameFieldNameOnDemandSync()
        {
            TestClassesWithSameFieldName(AnalysisStrategy.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestFieldLoadInCalleeOnDemandSync()
        {
            TestFieldLoadInCallee(AnalysisStrategy.ONDEMAND_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandSync")]
        public void TestPropertyOnDemandSync()
        {
            TestProperty(AnalysisStrategy.ONDEMAND_SYNC);
        }        
    }
}
