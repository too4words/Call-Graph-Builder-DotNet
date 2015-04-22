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
            TestSimpleCall(AnalysisStrategy.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestRecursionOnDemandAsync()
        {
            TestRecursion(AnalysisStrategy.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestIfOnDemandAsync()
        {
            TestIf(AnalysisStrategy.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestVirtualCallViaSuperClassOnDemandAsync()
        {
            TestVirtualCallViaSuperClass(AnalysisStrategy.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestCallViaInterfaceOnDemandAsync()
        {
            TestCallViaInterface(AnalysisStrategy.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestForLoopOnDemandAsync()
        {
            TestForLoop(AnalysisStrategy.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestFieldAccessOnDemandAsync()
        {
            TestFieldAccess(AnalysisStrategy.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestCallStaticDelegateOnDemandAsync()
        {
            TestCallStaticDelegate(AnalysisStrategy.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestCallInterfaceDelegateOnDemandAsync()
        {
            TestCallInterfaceDelegate(AnalysisStrategy.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestClassesWithSameFieldNameOnDemandAsync()
        {
            TestClassesWithSameFieldName(AnalysisStrategy.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestFieldLoadInCalleeOnDemandAsync()
        {
            TestFieldLoadInCallee(AnalysisStrategy.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestPropertyOnDemandAsync()
        {
            TestProperty(AnalysisStrategy.ONDEMAND_ASYNC);
        }
    }
}
