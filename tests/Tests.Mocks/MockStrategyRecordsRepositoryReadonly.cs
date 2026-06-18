using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Tests.Mocks;

public class MockStrategyRecordsRepositoryReadonly : IStrategyRecordsRepositoryReadonly
{
    public List<Strategy> Strategies { get; set; } = new();

    public async Task<IReadOnlyCollection<Strategy>> GetStrategyRecordsByStrategiesServiceNameAsync(string strategiesServiceName) =>
        await Task.Run(() => Strategies.Where(s => s.StrategyServiceName == strategiesServiceName).ToList());

    public async Task<IReadOnlyCollection<Strategy>> GetStrategyRecordsByAccountServiceName(string accountsServiceName) =>
        await Task.Run(() => Strategies);

    // public Task<IReadOnlyCollection<EsaSubscription>> GetExecutableSubaccountsByStrategiesServiceNameAsync(string strategiesServiceName) =>
    //     Task.FromResult((IReadOnlyCollection<EsaSubscription>)Array.Empty<EsaSubscription>());
    
}