// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ReachingTypeAnalysis
{
    [TestClass]
    public partial class EntireAsyncTests
    {
        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestSimpleCallEntireAsync()
        {
            BasicTests.TestSimpleCall(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestRecursionEntireAsync()
        {
            BasicTests.TestRecursion(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestIfEntireAsync()
        {
            BasicTests.TestIf(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestVirtualCallViaSuperClassEntireAsync()
        {
            BasicTests.TestVirtualCallViaSuperClass(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestCallViaInterfaceEntireAsync()
        {
            BasicTests.TestCallViaInterface(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestForLoopEntireAsync()
        {
            BasicTests.TestForLoop(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestFieldAccessEntireAsync()
        {
            BasicTests.TestFieldAccess(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestCallStaticDelegateEntireAsync()
        {
            BasicTests.TestCallStaticDelegate(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestCallInterfaceDelegateEntireAsync()
        {
            BasicTests.TestCallInterfaceDelegate(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestClassesWithSameFieldNameEntireAsync()
        {
            BasicTests.TestClassesWithSameFieldName(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestFieldLoadInCalleeEntireAsync()
        {
            BasicTests.TestFieldLoadInCallee(AnalysisStrategyKind.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestPropertyEntireAsync()
        {
            BasicTests.TestProperty(AnalysisStrategyKind.ENTIRE_ASYNC);
        }
    }
}
