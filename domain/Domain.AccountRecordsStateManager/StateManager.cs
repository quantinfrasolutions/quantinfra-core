using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Abstractions;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.Accounts.Abstractions;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Domain.AccountRecords.AccountRecordsClientStateManager;
using QuantInfra.Domain.Events.Accounts.Management;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Domain.AccountRecordsStateManager;


public class StateManager : 
    IQueryHandler<GetAccount, AccountRecordV6?>,
    IExternalEventHandler<AccountCreatedEvt>,
    IExternalEventHandler<SubaccountAssignedEvt>,
    IExternalEventHandler<TradingClientConfigurationChangedEvt>,

    IQueryHandler<GetBrokerAccountForSsa, int?>,
    IQueryHandler<GetSsaIdsForBrokerAccount, IReadOnlyCollection<int>>
{
    public readonly StateManagerConfig Config;
    private readonly IAccountRecordsRepositoryReadonly _repository;
    private readonly IAccountRecordsStore _store;
    private readonly ILogger<StateManager> _logger;
    private readonly IManagementNotificationsClient _client;


    public StateManager(StateManagerConfig config, IAccountRecordsRepositoryReadonly repository, IAccountRecordsStore store, 
        ILogger<StateManager> logger, IManagementNotificationsClient client)
    {
        Config = config;
        _repository = repository;
        _store = store;
        _logger = logger;
        _client = client;
    }
        
    public async Task LoadAccountRecordsAsync(Instant processingDt)
    {
        var existingAccounts = _store.AccountRecords.Keys.ToHashSet();
        _logger.LogInformation("Existing account records: {count}", existingAccounts.Count);
        var accountRecords = await _repository.GetAccountRecordsAsync(Config.AccountServiceName);

        foreach (var account in accountRecords)
        {
            if (!existingAccounts.Contains(account.AccountId))
            {
                _logger.LogInformation("Adding missing account record {accountId} {name}", account.AccountId,
                    account.Name);
                _client.PublishMessage(
                    new AccountCreatedEvt(account.AccountId, account.AccountId, account, processingDt),
                    processingDt);
            }
            else
            {
                var existingAcc = _store.AccountRecords[account.AccountId];
                // AS cares only about existence/non-existence of the trading client 
                if ((existingAcc.TradingClientConfig == null && account.TradingClientConfig != null) ||
                    (existingAcc.TradingClientConfig != null && account.TradingClientConfig == null))
                {
                    _logger.LogInformation("Updating account record {accountId} {name}", account.AccountId, account.Name);
                    _client.PublishMessage(new TradingClientConfigurationChangedEvt(account.AccountId,
                        account.AccountId, new(account.TradingClientConfig) { TradingClientSecret = null }, processingDt), processingDt);
                }
            }
        }

        var subaccounts = await _repository.GetSubaccountsAsync(Config.AccountServiceName);
        _logger.LogInformation("Existing subaccounts: {count}", subaccounts.Count);

        foreach (var sa in subaccounts)
        {
            if (!_store.Subaccounts.TryGetValue(sa.AccountId, out var classifications)
                || !classifications.TryGetValue(sa.Classifier, out var accounts)
                || accounts.All(a => a.SubaccountId != sa.SubaccountId)
               ) // A new subaccount created
            {
                _logger.LogInformation("Adding subaccount {saId} to account {accountId}", sa.SubaccountId,
                    sa.AccountId);
                _client.PublishMessage(
                    new SubaccountAssignedEvt(sa.AccountId, Config.AccountServiceName, sa.AccountId, sa,
                        processingDt), processingDt);
            }
        }

        // _store.Subaccounts = subaccounts
        //     .GroupBy(a => a.AccountId)
        //     .ToDictionary(
        //         g => g.Key, 
        //         g => g.ToDictionary(
        //             a => a.Classifier, 
        //             a => g.ToList()
        //         )
        //     );
        // _store.ReverseSubaccounts = GetReverseSubaccounts(subaccounts);
                
    }

    public static Dictionary<int, Dictionary<SubaccountType, List<int>>> GetReverseSubaccounts(
        IEnumerable<Subaccount> subaccounts) =>
        subaccounts
            .GroupBy(sa => sa.SubaccountId)
            .ToDictionary(
                gr => gr.Key,
                gr => gr
                    .GroupBy(sa => sa.Classifier)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(sa => sa.AccountId).ToList()
                    )
            );

    internal void SetAccountRecord(AccountRecordV6 account)
    {
        _logger.LogInformation($"Adding account record {account.AccountId} {account.Name}");
        _store.AccountRecords[account.AccountId] = account;
    }
        
    public IReadOnlyDictionary<int, AccountRecordV6> AccountRecords => _store.AccountRecords;

        
    public AccountRecordV6? Handle(GetAccount query) => _store.AccountRecords.GetValueOrDefault(query.AccountId);
        
    public IReadOnlyCollection<int> Handle(GetSsaIdsForBrokerAccount query)
    {
        if (_store.ReverseSubaccounts.TryGetValue(query.BrokerAccountId, out var classifications)
            && classifications.TryGetValue(SubaccountType.Broker, out var brokerAccounts))
        {
            return brokerAccounts;
        }
        return Array.Empty<int>();
    }

    public void Apply(AccountCreatedEvt e)
    {
        if (e.Account.AccountServiceName == Config.AccountServiceName)
            SetAccountRecord(e.Account);
    }
        
    public void Apply(TradingClientConfigurationChangedEvt e)
    {
        if (!_store.AccountRecords.TryGetValue(e.AccountId, out var account)) return;
        var acc = new AccountRecordV6(account) { TradingClientConfig = e.Config };
        SetAccountRecord(acc);
    }

    public int? Handle(GetBrokerAccountForSsa query)
    {
        if (_store.Subaccounts.TryGetValue(query.StrategySubaccountId, out var classifications)
            && classifications.TryGetValue(SubaccountType.Broker, out var brokerAccounts))
        {
            return brokerAccounts.SingleOrDefault(b => b.BrokerId == query.BrokerId)?.SubaccountId;
        }
        return null;
    }
        
    public void Apply(SubaccountAssignedEvt e)
    {
        _store.Subaccounts.TryAdd(e.AccountId, new());
        _store.Subaccounts[e.AccountId].TryAdd(e.Subaccount.Classifier, new());
        _store.Subaccounts[e.AccountId][e.Subaccount.Classifier].Add(e.Subaccount);

        _store.ReverseSubaccounts.TryAdd(e.Subaccount.SubaccountId, new());
        _store.ReverseSubaccounts[e.Subaccount.SubaccountId].TryAdd(e.Subaccount.Classifier, new());
        _store.ReverseSubaccounts[e.Subaccount.SubaccountId][e.Subaccount.Classifier].Add(e.AccountId);
    }
        
}