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
            TestSimpleCall(AnalysisStrategy.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestRecursionOnDemandOrleans()
        {
            TestRecursion(AnalysisStrategy.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestIfOnDemandOrleans()
        {
            TestIf(AnalysisStrategy.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestVirtualCallViaSuperClassOnDemandOrleans()
        {
            TestVirtualCallViaSuperClass(AnalysisStrategy.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestCallViaInterfaceOnDemandOrleans()
        {
            TestCallViaInterface(AnalysisStrategy.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestForLoopOnDemandOrleans()
        {
            TestForLoop(AnalysisStrategy.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestFieldAccessOnDemandOrleans()
        {
            TestFieldAccess(AnalysisStrategy.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestCallStaticDelegateOnDemandOrleans()
        {
            TestCallStaticDelegate(AnalysisStrategy.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestCallInterfaceDelegateOnDemandOrleans()
        {
            TestCallInterfaceDelegate(AnalysisStrategy.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestClassesWithSameFieldNameOnDemandOrleans()
        {
            TestClassesWithSameFieldName(AnalysisStrategy.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestFieldLoadInCalleeOnDemandOrleans()
        {
            TestFieldLoadInCallee(AnalysisStrategy.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestPropertyOnDemandOrleans()
        {
            TestProperty(AnalysisStrategy.ONDEMAND_ORLEANS);
        }
    }
}
