using QuantInfra.Common.Infrastructure.Abstractions;

namespace Common.Infrastructure.Abstractions.Api;

public class StrategiesServiceListView : StrategiesServiceInstance
{
    public int ActiveStrategiesCount { get; init; }
}