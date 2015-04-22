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
            TestSimpleCall(AnalysisStrategy.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestRecursionEntireSync()
        {
            TestRecursion(AnalysisStrategy.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestIfEntireSync()
        {
            TestIf(AnalysisStrategy.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestVirtualCallViaSuperClassEntireSync()
        {
            TestVirtualCallViaSuperClass(AnalysisStrategy.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestCallViaInterfaceEntireSync()
        {
            TestCallViaInterface(AnalysisStrategy.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestForLoopEntireSync()
        {
            TestForLoop(AnalysisStrategy.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestFieldAccessEntireSync()
        {
            TestFieldAccess(AnalysisStrategy.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestCallStaticDelegateEntireSync()
        {
            TestCallStaticDelegate(AnalysisStrategy.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestCallInterfaceDelegateEntireSync()
        {
            TestCallInterfaceDelegate(AnalysisStrategy.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestClassesWithSameFieldNameEntireSync()
        {
            TestClassesWithSameFieldName(AnalysisStrategy.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestFieldLoadInCalleeEntireSync()
        {
            TestFieldLoadInCallee(AnalysisStrategy.ENTIRE_SYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("EntireSync")]
        public void TestPropertyEntireSync()
        {
            TestProperty(AnalysisStrategy.ENTIRE_SYNC);
        }
    }

}
