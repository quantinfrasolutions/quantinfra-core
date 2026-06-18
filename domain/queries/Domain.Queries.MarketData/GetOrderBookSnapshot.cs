using System;
using System.Text.Json.Serialization;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Domain.Queries.MarketData;

public record GetOrderBookSnapshot(
    Guid RequestId,
    int ContractId,
    bool UseMulticast
) : IAsyncQueryWithMulticast<OrderBookSnapshot?>
{
    [JsonConstructor] public GetOrderBookSnapshot(int contractId, bool useMulticast) : this(Guid.NewGuid(), contractId, useMulticast) {}
}