using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Domain.Queries.Strategies;

public record struct GetStrategyState(int StrategyId) : IQuery<IStrategyStateReadonly?>;