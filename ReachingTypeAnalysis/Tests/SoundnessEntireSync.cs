// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ReachingTypeAnalysis
{
    [TestClass]
    public partial class EntireTests
    {
        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestSimpleCallEntireSync()
        {
            BasicTests.TestSimpleCall(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestRecursionEntireSync()
        {
            BasicTests.TestRecursion(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestIfEntireSync()
        {
            BasicTests.TestIf(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestVirtualCallViaSuperClassEntireSync()
        {
            BasicTests.TestVirtualCallViaSuperClass(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestCallViaInterfaceEntireSync()
        {
            BasicTests.TestCallViaInterface(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestForLoopEntireSync()
        {
            BasicTests.TestForLoop(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestFieldAccessEntireSync()
        {
            BasicTests.TestFieldAccess(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestCallStaticDelegateEntireSync()
        {
            BasicTests.TestCallStaticDelegate(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestCallInterfaceDelegateEntireSync()
        {
            BasicTests.TestCallInterfaceDelegate(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestClassesWithSameFieldNameEntireSync()
        {
            BasicTests.TestClassesWithSameFieldName(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestFieldLoadInCalleeEntireSync()
        {
            BasicTests.TestFieldLoadInCallee(AnalysisStrategyKind.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestPropertyEntireSync()
        {
            BasicTests.TestProperty(AnalysisStrategyKind.ENTIRE_SYNC);
        }
    }

}
