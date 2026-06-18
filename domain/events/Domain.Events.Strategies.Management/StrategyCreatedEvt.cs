using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Domain.Events.Strategies.Management
{
    public record struct StrategyCreatedEvt(
        long EventId,
        int StrategyId,
        Strategy Strategy,
        AccountRecordV6 Account,
        Instant Timestamp
    ) : IEvent;
}

