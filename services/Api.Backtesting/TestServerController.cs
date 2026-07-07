using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QuantInfra.Common.Backtesting.Abstractions;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Sdk.Backtesting;

namespace QuantInfra.Services.Api.Backtesting;

[ApiController]
[Route("api/test-server")]
public class TestServerController(ITestServer testServer)
{
    [HttpGet, Route("strategies")]
    [EndpointName(nameof(GetSupportedStrategies))]
    [Produces("application/json")]
    public async Task<IEnumerable<StrategyTypeDescription>> GetSupportedStrategies()
    {
        return await testServer.GetStrategiesAsync();
    }
    
    [HttpGet, Route("actions")]
    [EndpointName(nameof(GetActions))]
    [Produces("application/json")]
    public async Task<IEnumerable<string>> GetActions()
    {
        return await testServer.GetSupportedActionsAsync();
    }
    
    [HttpGet, Route("actions/sample-params/{actionName}")]
    [EndpointName(nameof(GetSampleParams))]
    [Produces("application/json")]
    public async Task<string> GetSampleParams(string actionName)
    {
        return await testServer.GetSampleActionParamsAsync(actionName);
    }
    
    [HttpGet, Route("calculators")]
    [EndpointName(nameof(GetCalculators))]
    [Produces("application/json")]
    public async Task<IEnumerable<string>> GetCalculators()
    {
        return await testServer.GetSupportedMetricsCalculatorsAsync();
    }
    
    [HttpGet, Route("calculators/sample-params/{calculator}")]
    [EndpointName(nameof(GetSampleCalculatorOptions))]
    [Produces("application/json")]
    public async Task<string> GetSampleCalculatorOptions(string calculator)
    {
        return await testServer.GetSampleMetricsCalculatorOptionsAsync(calculator);
    }

    [HttpPost, Route("validate-market-data/{unitId:guid}")]
    [EndpointName(nameof(ValidateMarketData))]
    [Produces("application/json")]
    public async Task<IEnumerable<RequiredMarketDataUnit>> ValidateMarketData(Guid unitId)
    {
        return await testServer.ValidateRequiredMarketDataAsync(unitId);
    }

    [HttpPost, Route("validate-params")]
    [EndpointName(nameof(ValidateParams))]
    [Produces("application/json")]
    public async Task<ActionParamsValidationResult> ValidateParams(string action, [FromBody] string? @params)
    {
        return await testServer.ValidateParamsAsync(action, @params);
    }

    [HttpPost, Route("run/{unitId:guid}")]
    [EndpointName(nameof(RunTest))]
    public async Task RunTest(Guid unitId)
    {
        await testServer.RunAsync(unitId);
    }
}