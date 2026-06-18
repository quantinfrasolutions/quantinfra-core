using QuantInfra.Common.Interfaces.Api.Strategies;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Common.Strategies.Abstractions;

public interface IStrategyRecordsRepositoryReadonly
{
    Task<IReadOnlyCollection<Strategy>> GetStrategyRecordsByStrategiesServiceNameAsync(string strategiesServiceName);
    Task<IReadOnlyCollection<Strategy>> GetStrategyRecordsByAccountServiceName(string accountsServiceName);
    // Task<IReadOnlyCollection<EsaSubscription>> GetExecutableSubaccountsByStrategiesServiceNameAsync(string strategiesServiceName);
}

public interface IStrategyRecordsRepository : IStrategyRecordsRepositoryReadonly
{
    Task<(Strategy, AccountRecordV6)> CreateStrategyAsync(CreateStrategyRequest request, int userId);
    Task<Strategy> GetStrategyRecordAsync(int strategyId);
    Task UpdateStrategyStatusAsync(int strategyId, StrategyStatus status);
}