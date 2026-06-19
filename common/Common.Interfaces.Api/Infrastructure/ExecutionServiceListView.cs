using QuantInfra.Common.Infrastructure.Abstractions;

namespace QuantInfra.Common.Interfaces.Api.Infrastructure;

public class ExecutionServiceListView : StrategiesServiceInstance
{
    public int TradingClientsCount { get; init; }
}