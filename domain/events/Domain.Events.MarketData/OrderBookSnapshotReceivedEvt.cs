using NodaTime;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Domain.Events.MarketData;

public record struct OrderBookSnapshotReceivedEvt(
    int ContractId, 
    OrderBookSnapshot Snapshot,
    Instant Timestamp
);