using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Abstractions;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Domain.Events.Strategies.Management;
using QuantInfra.Domain.Queries.Strategies;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Domain.StrategyRecordsStateManager;

public class AccountsServiceStateManager : IQueryHandler<GetStrategyRecord, Strategy?>
{
    public readonly AccountsServiceStateManagerConfig Config;
    private readonly IStrategyRecordsRepositoryReadonly _repository;
    private readonly ILogger _logger;
    private readonly IStrategyRecordsStore _store;
    private readonly IManagementNotificationsClient _client;
        
    public AccountsServiceStateManager(
        AccountsServiceStateManagerConfig config, 
        IStrategyRecordsStore store,
        IStrategyRecordsRepositoryReadonly repository,
        ILogger<AccountsServiceStateManager> logger, 
        IManagementNotificationsClient client
    )
    {
        Config = config;
        _store = store;
        _repository = repository;
        _logger = logger;
        _client = client;
    }
        
    public async Task LoadStrategiesRecordsAsync(Instant processingDt)
    {
        var existingStrategies = _store.StrategyRecords.Keys.ToList();
        _logger.LogInformation("Existing strategy records: {count}", existingStrategies.Count);
        var strategyRecords = await _repository.GetStrategyRecordsByAccountServiceName(Config.AccountsServiceName);

        foreach (var strategy in strategyRecords)
        {
            if (!existingStrategies.Contains(strategy.StrategyId))
            {
                _logger.LogInformation("Adding missing strategy record {strategyId} {name}", strategy.StrategyId,
                    strategy.Name);
                _client.PublishMessage(
                    new StrategyCreatedEvt(strategy.StrategyId, strategy.StrategyId, strategy, null, processingDt),
                    processingDt); // TODO: hack
            }
            else
            {
                var existingStr = _store.StrategyRecords[strategy.StrategyId];
                if (existingStr.Status != strategy.Status)
                {
                    // TODO
                }
            }
        }

        _logger.LogInformation($"Loaded {StrategyRecords.Count} strategy records");
    }

    internal void SetStrategyRecord(Strategy strategy)
    {
        _logger.LogInformation($"Adding strategy record {strategy.AccountId} {strategy.Name}");
        _store.StrategyRecords[strategy.StrategyId] = strategy;
    }
        
    public IReadOnlyDictionary<int, Strategy> StrategyRecords => _store.StrategyRecords;
    
    public Strategy? Handle(GetStrategyRecord query) => _store.StrategyRecords.GetValueOrDefault(query.StrategyId);

    public Task<Strategy> HandleAsync(GetStrategyRecord query)
    {
        throw new System.NotImplementedException();
    }
}