namespace QuantInfra.Common.Interfaces.Api.Strategies;

public class ValidateStrategyResult
{
    public bool Success { get; init; }
    public List<string>? Errors { get; init; }
    public StrategyViewBrief Strategy { get; init; }
}