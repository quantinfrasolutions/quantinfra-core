using System.Collections.Generic;
using NodaTime;
using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Domain.Events.MarketData;

public record struct AggregatedOrderbookUpdateEvt(
    int ContractId,
    IReadOnlyDictionary<decimal, decimal> UpdatedBids,
    IReadOnlyDictionary<decimal, decimal> UpdatedAsks,
    Instant ExchangeTs,
    Instant Timestamp
) : IEvent
{
    public long EventId => 0;
}