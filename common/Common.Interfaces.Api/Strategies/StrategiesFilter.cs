using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Common.Interfaces.Api.Strategies;

public class StrategiesFilter : PagingFilter
{
    public List<StrategyStatus>? Status { get; set; }
    public List<string>? ClassNames { get; set; }
    public List<int>? StrategyIds { get; set; } = null;
}