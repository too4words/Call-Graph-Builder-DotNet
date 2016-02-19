﻿using Orleans;
using OrleansInterfaces;
using ReachingTypeAnalysis.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReachingTypeAnalysis;

namespace ReachingTypeAnalysis.Analysis
{
	public class OrchestratorGrain : Grain, IOrchestratorGrain
	{
		[NonSerialized]
		private OrchestratorManager orchestratorManager;
		[NonSerialized]
		private ISolutionGrain solutionGrain;

		public override async Task OnActivateAsync()
		{
			await StatsHelper.RegisterActivation("OrchestratorGrain", this.GrainFactory);

			this.solutionGrain = OrleansSolutionManager.GetSolutionGrain(this.GrainFactory);
			this.orchestratorManager = await OrleansOrchestratorManager.CreateAsync(this.GrainFactory, this.solutionGrain);
		}

		public override Task OnDeactivateAsync()
		{
			return StatsHelper.RegisterDeactivation("OrchestratorGrain", this.GrainFactory);
		}

		public Task ProcessMethodsAsync(IEnumerable<MethodDescriptor> methods)
		{
			StatsHelper.RegisterMsg("OrchestratorGrain::ProcessMethods", this.GrainFactory);

			return this.orchestratorManager.ProcessMethodsAsync(methods);
		}

		public Task ProcessEffectsAsync(PropagationEffects effects, PropagationKind propKind)
		{
			StatsHelper.RegisterMsg("OrchestratorGrain::ProcessEffects", this.GrainFactory);

			return this.orchestratorManager.ProcessEffectsAsync(effects, propKind);
		}
	}
}
