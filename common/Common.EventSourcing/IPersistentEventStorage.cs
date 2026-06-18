using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuantInfra.Common.EventSourcing;

public interface IPersistentEventStorage<TState>
{
    Task<TState?> GetLatestStateSnapshot(string instanceName);
    Task<IReadOnlyList<IEvent>> GetEventsSinceLastSnapshot(string instanceName, int limit, int offset);
    IEvent? GetEvent(string accountServiceName, long eventId);
}