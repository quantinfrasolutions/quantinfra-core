using QuantInfra.Common.Infrastructure.Abstractions;

namespace Common.Infrastructure.Abstractions.Api;

public class ExecutionServiceListView : StrategiesServiceInstance
{
    public int TradingClientsCount { get; init; }
}