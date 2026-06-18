using System;
using System.Text.Json.Serialization;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Domain.Queries.Strategies.AccountsService;

public record GetStrategyState(Guid RequestId, int StrategyId, bool UseMulticast = false) : IAsyncQueryWithMulticast<StrategyStateReadonly?>
{
    [JsonConstructor] public GetStrategyState(int strategyId, bool useMulticast = false) : 
        this(Guid.NewGuid(), strategyId, useMulticast) { }
}