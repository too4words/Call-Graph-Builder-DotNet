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
            TestSimpleCall(AnalysisStrategy.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestRecursionEntireAsync()
        {
            TestRecursion(AnalysisStrategy.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestIfEntireAsync()
        {
            TestIf(AnalysisStrategy.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestVirtualCallViaSuperClassEntireAsync()
        {
            TestVirtualCallViaSuperClass(AnalysisStrategy.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestCallViaInterfaceEntireAsync()
        {
            TestCallViaInterface(AnalysisStrategy.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestForLoopEntireAsync()
        {
            TestForLoop(AnalysisStrategy.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestFieldAccessEntireAsync()
        {
            TestFieldAccess(AnalysisStrategy.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestCallStaticDelegateEntireAsync()
        {
            TestCallStaticDelegate(AnalysisStrategy.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestCallInterfaceDelegateEntireAsync()
        {
            TestCallInterfaceDelegate(AnalysisStrategy.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestClassesWithSameFieldNameEntireAsync()
        {
            TestClassesWithSameFieldName(AnalysisStrategy.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestFieldLoadInCalleeEntireAsync()
        {
            TestFieldLoadInCallee(AnalysisStrategy.ENTIRE_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireAsync")]
        public void TestPropertyEntireAsync()
        {
            TestProperty(AnalysisStrategy.ENTIRE_ASYNC);
        }
    }
}
