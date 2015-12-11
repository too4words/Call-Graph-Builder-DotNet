// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using OrleansInterfaces;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Orleans.Concurrency;

namespace ReachingTypeAnalysis.Analysis
{
    //[StorageProvider(ProviderName = "FileStore")]
    //[StorageProvider(ProviderName = "MemoryStore")]
	[Reentrant]
	public class RtaGrain : Grain, IRtaGrain
    {
        private OrleansRtaManager rtaManager;

        public override async Task OnActivateAsync()
        {
			await StatsHelper.RegisterActivation("RTAGrain", this.GrainFactory);
            this.rtaManager = new OrleansRtaManager();
		}

		public override Task OnDeactivateAsync()
		{
			return StatsHelper.RegisterDeactivation("RTAGrain", this.GrainFactory); 
		}

		public Task AddInstantiatedTypesAsync(IEnumerable<TypeDescriptor> types)
        {
			StatsHelper.RegisterMsg("RtaGrain::AddInstantiatedTypes", this.GrainFactory);

            return this.rtaManager.AddInstantiatedTypesAsync(types);
        }

        public Task<ISet<TypeDescriptor>> GetInstantiatedTypesAsync()
        {
			StatsHelper.RegisterMsg("SolutionGrain::GetInstantiatedTypes", this.GrainFactory);

			return this.rtaManager.GetInstantiatedTypesAsync();
        }
	}    
}
