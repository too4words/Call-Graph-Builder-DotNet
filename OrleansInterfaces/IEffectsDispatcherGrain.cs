using Orleans;
using Orleans.Streams;
using ReachingTypeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansInterfaces
{
	public interface IEffectsDispatcherGrain : IGrainWithStringKey, IEffectsDispatcher, IAsyncObserver<PropagationEffects>
	{
	}
}
