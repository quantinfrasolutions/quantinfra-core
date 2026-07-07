using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Sdk.Backtesting;

namespace QuantInfra.Common.Backtesting.Abstractions;

public interface ITestServer
{
    Task<IReadOnlyCollection<string>> GetSupportedActionsAsync();
    Task<IReadOnlyCollection<string>> GetSupportedMetricsCalculatorsAsync();
    Task<IReadOnlyCollection<StrategyTypeDescription>> GetStrategiesAsync();
    Task<string> GetSampleActionParamsAsync(string action);
    Task<string> GetSampleMetricsCalculatorOptionsAsync(string calculator);
    
    Task<IReadOnlyCollection<RequiredMarketDataUnit>> ValidateRequiredMarketDataAsync(Guid unitId);
    Task<ActionParamsValidationResult> ValidateParamsAsync(string action, string? options);
    Task RunAsync(Guid unitId);
}