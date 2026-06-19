using QuantInfra.Common.Infrastructure.Abstractions;

namespace QuantInfra.Common.Interfaces.Api.Infrastructure;

public class StrategiesServiceListView : StrategiesServiceInstance
{
    public int ActiveStrategiesCount { get; init; }
}