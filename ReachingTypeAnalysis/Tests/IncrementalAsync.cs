// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ReachingTypeAnalysis
{
	[TestClass]
	public class IncrementalAsyncTests
	{
		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalAsync")]
		public void TestRemoveMethodSimpleCallOnDemandAsync()
		{
			BasicTests.TestRemoveMethodSimpleCall(AnalysisStrategyKind.ONDEMAND_ASYNC);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalAsync")]
		public void TestUpdateMethodSimpleCallOnDemandAsync()
		{
			BasicTests.TestUpdateMethodSimpleCall(AnalysisStrategyKind.ONDEMAND_ASYNC);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalAsync")]
		public void TestAddMethodSimpleCallOnDemandAsync()
		{
			BasicTests.TestAddMethodSimpleCall(AnalysisStrategyKind.ONDEMAND_ASYNC);
		}
	}
}
