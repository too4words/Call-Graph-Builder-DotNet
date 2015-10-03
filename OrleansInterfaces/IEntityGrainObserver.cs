using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansInterfaces
{
	public enum EntityGrainState
	{
		Busy,
		Ready
	}

	public interface IEntityGrainObserver : IGrainObserver
	{
		void OnStateChanged(IGrain sender, EntityGrainState newState);
	}
}
