using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Accounts.Abstractions;
using Microsoft.Extensions.Logging;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Domain.Queries.Strategies;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Domain.StrategyRecordsStateManager;

public class StrategiesServiceStateManager : 
    IQueryHandler<GetStrategyRecord, Strategy?>,
    IQueryHandler<GetAccount, AccountRecordV6?>
    // , IQueryHandler<GetSubscribedEsas, IReadOnlyCollection<int>>
{
    public readonly StrategiesServiceStateManagerConfig Config;
    private readonly IStrategyRecordsRepositoryReadonly _repository;
    private readonly IAccountRecordsRepositoryReadonly _accountRecordsRepository;
    private readonly ILogger<StrategiesServiceStateManager> _logger;

    private Dictionary<int, Strategy> _strategyRecords;
    // private Dictionary<int, EsaSubscription> _esaSubscriptions;
    private Dictionary<int, List<int>> _esaByStrategyAccountIds;
    private Dictionary<int, AccountRecordV6> _accountRecords;
        
    public StrategiesServiceStateManager(
        StrategiesServiceStateManagerConfig config, 
        IStrategyRecordsRepositoryReadonly repository,
        IAccountRecordsRepositoryReadonly accountRecordsRepository,
        ILogger<StrategiesServiceStateManager> logger
    )
    {
        Config = config;
        _repository = repository;
        _accountRecordsRepository = accountRecordsRepository;
        _logger = logger;
    }
        
    public async Task LoadStrategiesRecordsAsync(CancellationToken cancellationToken)
    {
        _strategyRecords = (await _repository.GetStrategyRecordsByStrategiesServiceNameAsync(Config.StrategiesServiceName))
            .ToDictionary(a => a.StrategyId);

        // var esaSubscriptions = await _repository
        //     .GetExecutableSubaccountsByStrategiesServiceNameAsync(Config.StrategiesServiceName);
        // _esaSubscriptions = esaSubscriptions.ToDictionary(a => a.ExecutableSubaccountId);
        // _esaByStrategyAccountIds = esaSubscriptions
        //     .GroupBy(s => s.StrategyAccountId)
        //     .ToDictionary(
        //         gr => gr.Key,
        //         gr => gr.Select(a => a.ExecutableSubaccountId).ToList()
        //     );
            
        var accountIds = _strategyRecords.Values.Select(s => s.AccountId)
            // .Union(_esaSubscriptions.Values.Select(s => s.ExecutableSubaccountId))
            .Distinct().ToList();
        _accountRecords = (await _accountRecordsRepository.GetAccountRecordsAsync(accountIds))
            .ToDictionary(a => a.AccountId);
            
        var diff = accountIds.Except(_accountRecords.Keys).Union(_accountRecords.Keys.Except(accountIds)).ToList();
        if (diff.Any())
        {
            throw new InvalidOperationException($"Account records mismatch: {string.Join(',', diff)}");
        }
        
        _logger.LogInformation($"Loaded {StrategyRecords.Count} strategy records");
        // _logger.LogInformation($"Loaded {StrategyRecords.Count} strategy records, {_esaSubscriptions.Count} ESA subscriptions");
    }

    internal void SetStrategyRecord(Strategy strategy, AccountRecordV6 account)
    {
        if (strategy.AccountId != account.AccountId)
        {
            throw new InvalidOperationException($"Strategy account {strategy.AccountId} and account {account.AccountId} mismatch");
        }
        _logger.LogInformation($"Adding strategy record {strategy.AccountId} {strategy.Name}, account record {account.AccountId} {account.Name}");
        _strategyRecords[strategy.AccountId] = strategy;
        _accountRecords[account.AccountId] = account;
    }
        
    public IReadOnlyDictionary<int, Strategy> StrategyRecords => _strategyRecords;
    public IReadOnlyDictionary<int, AccountRecordV6> AccountRecords => _accountRecords;
    // public IReadOnlyDictionary<int, EsaSubscription> EsaSubscriptions => _esaSubscriptions;
    
    public Strategy? Handle(GetStrategyRecord query) => _strategyRecords.GetValueOrDefault(query.StrategyId);

    public AccountRecordV6? Handle(GetAccount query) => _accountRecords.GetValueOrDefault(query.AccountId);
    // public IReadOnlyCollection<int> Handle(GetSubscribedEsas query) =>
    //     _esaByStrategyAccountIds.GetValueOrDefault(query.AccountId) ?? new List<int>(0);
}