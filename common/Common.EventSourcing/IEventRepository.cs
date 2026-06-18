using System.Collections.Generic;

namespace QuantInfra.Common.EventSourcing
{
	public interface IEventRepository
	{		
		void PersistEvent(IEvent @event);
		void PersistEvents(IEnumerable<IEvent> events);
	}
}

