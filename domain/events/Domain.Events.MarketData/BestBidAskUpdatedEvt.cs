using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Domain.Events.MarketData;

public record struct BestBidAskUpdatedEvt(
    int ContractId,
    BookLevel? Bid,
    BookLevel? Ask,
    Instant Timestamp
) : IEvent
{
    public long EventId => 0;
}