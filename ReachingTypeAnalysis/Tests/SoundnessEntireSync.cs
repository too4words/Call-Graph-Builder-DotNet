// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ReachingTypeAnalysis
{
    public partial class Tests
    {
        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestSimpleCallEntireSync()
        {
            TestSimpleCall(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestRecursionEntireSync()
        {
            TestRecursion(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestIfEntireSync()
        {
            TestIf(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestVirtualCallViaSuperClassEntireSync()
        {
            TestVirtualCallViaSuperClass(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestCallViaInterfaceEntireSync()
        {
            TestCallViaInterface(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestForLoopEntireSync()
        {
            TestForLoop(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestFieldAccessEntireSync()
        {
            TestFieldAccess(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestCallStaticDelegateEntireSync()
        {
            TestCallStaticDelegate(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestCallInterfaceDelegateEntireSync()
        {
            TestCallInterfaceDelegate(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestClassesWithSameFieldNameEntireSync()
        {
            TestClassesWithSameFieldName(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestFieldLoadInCalleeEntireSync()
        {
            TestFieldLoadInCallee(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestPropertyEntireSync()
        {
            TestProperty(AnalysisStrategyKind.ENTIRE_SYNC);
        }
    }

}
