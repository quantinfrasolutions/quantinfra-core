using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Domain.Queries.Strategies;

public record struct GetStrategy(int StrategyId) : IQuery<IStrategy?>;