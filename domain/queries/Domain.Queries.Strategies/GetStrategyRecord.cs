using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Domain.Queries.Strategies;

public record class GetStrategyRecord(int StrategyId) : IQuery<Strategy?>;