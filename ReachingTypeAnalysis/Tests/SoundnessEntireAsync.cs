// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ReachingTypeAnalysis
{
    public partial class Tests
    {
        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestSimpleCallEntireAsync()
        {
            TestSimpleCall(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestRecursionEntireAsync()
        {
            TestRecursion(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestIfEntireAsync()
        {
            TestIf(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestVirtualCallViaSuperClassEntireAsync()
        {
            TestVirtualCallViaSuperClass(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestCallViaInterfaceEntireAsync()
        {
            TestCallViaInterface(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestForLoopEntireAsync()
        {
            TestForLoop(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestFieldAccessEntireAsync()
        {
            TestFieldAccess(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestCallStaticDelegateEntireAsync()
        {
            TestCallStaticDelegate(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestCallInterfaceDelegateEntireAsync()
        {
            TestCallInterfaceDelegate(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestClassesWithSameFieldNameEntireAsync()
        {
            TestClassesWithSameFieldName(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestFieldLoadInCalleeEntireAsync()
        {
            TestFieldLoadInCallee(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestPropertyEntireAsync()
        {
            TestProperty(AnalysisStrategyKind.ENTIRE_ASYNC);
        }
    }
}
