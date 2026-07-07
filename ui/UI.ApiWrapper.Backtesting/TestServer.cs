using QuantInfra.Common.Backtesting.Abstractions;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Sdk.Backtesting;

namespace QuantInfra.UI.ApiWrapper.Backtesting;

public partial class ApiRepository : ITestServer
{
    public Task<IReadOnlyCollection<string>> GetSupportedActionsAsync() =>
        Retrieve("actions", async () => (IReadOnlyCollection<string>)(await _wrapper.Client.GetActionsAsync()).ToList());

    public Task<IReadOnlyCollection<string>> GetSupportedMetricsCalculatorsAsync() =>
        Retrieve("calculators", async () => (IReadOnlyCollection<string>)(await _wrapper.Client.GetCalculatorsAsync()));

    public Task<IReadOnlyCollection<StrategyTypeDescription>> GetStrategiesAsync() =>
        Retrieve("strategies", async () => (IReadOnlyCollection<StrategyTypeDescription>)(await _wrapper.Client.GetSupportedStrategiesAsync()).ToList());

    public Task<string> GetSampleActionParamsAsync(string action) =>
        Retrieve("sample params", () => _wrapper.Client.GetSampleParamsAsync(action));

    public Task<string> GetSampleMetricsCalculatorOptionsAsync(string calculator) =>
        Retrieve("sample options", () => _wrapper.Client.GetSampleCalculatorOptionsAsync(calculator));

    public Task<IReadOnlyCollection<RequiredMarketDataUnit>> ValidateRequiredMarketDataAsync(Guid unitId) =>
        Retrieve("market data validated", "failed to validate market data", 
            async s => (IReadOnlyCollection<RequiredMarketDataUnit>)(await _wrapper.Client.ValidateMarketDataAsync(unitId)).ToList());

    public Task<ActionParamsValidationResult> ValidateParamsAsync(string action, string? options) =>
        Retrieve("params validated", "params validation failed", s => _wrapper.Client.ValidateParamsAsync(action, options));

    public Task RunAsync(Guid unitId) =>
        Call("test run", "test failed", () => _wrapper.Client.RunTestAsync(unitId));
}