// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orleans.TestingHost;
using System;

namespace ReachingTypeAnalysis
{
	[TestClass]
	public class IncrementalOrleansTests : TestingSiloHost
	{
		[ClassCleanup]
		public static void ClassCleanup()
		{
			// Optional. 
			// By default, the next test class which uses TestignSiloHost will
			// cause a fresh Orleans silo environment to be created.
			StopAllSilos();
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalOrleans")]
		public void TestRemoveMethodSimpleCallOnDemandOrleans()
		{
			BasicTests.TestRemoveMethodSimpleCall(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalOrleans")]
		public void TestAddMethodSimpleCallOnDemandOrleans()
		{
			BasicTests.TestAddMethodSimpleCall(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalOrleans")]
		public void TestUpdateMethodSimpleCallOnDemandOrleans()
		{
			BasicTests.TestUpdateMethodSimpleCall(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}
	}
}
