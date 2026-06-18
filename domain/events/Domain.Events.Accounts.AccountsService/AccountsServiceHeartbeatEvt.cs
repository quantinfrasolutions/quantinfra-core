using NodaTime;
using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Domain.Events.Accounts.AccountsService;

public record AccountsServiceHeartbeatEvt(long EventId, Instant Timestamp) : IEvent;