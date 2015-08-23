// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ReachingTypeAnalysis
{
    [TestClass]
    public partial class AsyncTests
    {
        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestSimpleCallOnDemandAsync()
        {
            BasicTests.TestSimpleCall(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

		[TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestRecursionOnDemandAsync()
        {
            BasicTests.TestRecursion(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestIfOnDemandAsync()
        {
            BasicTests.TestIf(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestVirtualCallViaSuperClassOnDemandAsync()
        {
            BasicTests.TestVirtualCallViaSuperClass(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestCallViaInterfaceOnDemandAsync()
        {
            BasicTests.TestCallViaInterface(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestForLoopOnDemandAsync()
        {
            BasicTests.TestForLoop(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestFieldAccessOnDemandAsync()
        {
            BasicTests.TestFieldAccess(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestCallStaticDelegateOnDemandAsync()
        {
            BasicTests.TestCallStaticDelegate(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestCallInterfaceDelegateOnDemandAsync()
        {
            BasicTests.TestCallInterfaceDelegate(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestClassesWithSameFieldNameOnDemandAsync()
        {
            BasicTests.TestClassesWithSameFieldName(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestFieldLoadInCalleeOnDemandAsync()
        {
            BasicTests.TestFieldLoadInCallee(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestPropertyOnDemandAsync()
        {
            BasicTests.TestProperty(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("OnDemandAsync")]
		public void TestArrowMethodBodyOnDemandAsync()
		{
			BasicTests.TestArrowMethodBody(AnalysisStrategyKind.ONDEMAND_ASYNC);
		}

		[TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandAsync")]
        public void TestLambdaOnDemandAsync()
        {
            BasicTests.TestLambda(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("OnDemandAsync")]
		public void TestNamedParametersOnDemandAsync()
		{
			BasicTests.TestNamedParameters(AnalysisStrategyKind.ONDEMAND_ASYNC);
		}
	}
}
