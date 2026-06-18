using NodaTime;
using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Domain.Accounts.AccountStateClientManager.Events;

public record AccountMissingVersionEvt(int AccountId, Instant Timestamp) : IEvent
{
    public long EventId => 0;
}