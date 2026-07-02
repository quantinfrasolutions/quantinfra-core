using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Sdk.Backtesting;

namespace QuantInfra.Common.Backtesting.Abstractions;

public interface ITestServer
{
    Task<IReadOnlyCollection<string>> GetSupportedActionsAsync();
    Task<IReadOnlyCollection<StrategyTypeDescription>> GetStrategiesAsync();
    Task<string> GetSampleParams(string action);
    
    Task<IReadOnlyCollection<RequiredMarketDataUnit>> ValidateRequiredMarketData(Guid unitId);
    Task<ActionParamsValidationResult> ValidateParams(string action, string? options);
    Task RunAsync(Guid unitId);
}