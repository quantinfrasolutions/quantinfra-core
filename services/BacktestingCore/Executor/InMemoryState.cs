using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Accounts.Base;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.Events.Accounts.Management;
using QuantInfra.Domain.Events.Strategies.Management;
using QuantInfra.Domain.MarketData;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Domain.Queries.Strategies;
using QuantInfra.Domain.Strategies;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Accounts.AccountStates;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Services.BacktestingCore.Executor;

internal sealed class InMemoryState : 
    IEventIdProvider,
    IExecIdProvider,
    IBalanceOperationIdProvider,
    IOrderIdProvider,
    ITradeIdProvider,
    
    ILastContractPricesStore,
    
    IEventHandler<AccountCreatedEvt>,
    IEventHandler<StrategyCreatedEvt>,
    
    IQueryHandler<GetStrategyRecord, QuantInfra.Sdk.Strategies.Strategy?>,
    IQueryHandler<GetStrategyState, IStrategyStateReadonly?>,
    IQueryHandler<GetStrategy, IStrategy?>,

    IQueryHandler<GetAccount, IAccountStateReadonly?>,
    IQueryHandler<GetAccount, IAccount?>,
    IQueryHandler<GetAccount, ITradingAccount?>,
    IQueryHandler<GetAccount, AccountRecordV6?>,

    IQueryHandler<GetAccountIdsForEndOfDay, IReadOnlyCollection<int>>,
    IQueryHandler<GetActiveVirtualExecutorOrders, IReadOnlyCollection<OrderStatus>>
{
    private readonly IEventBus _eventBus;
    private readonly IQueryBus _queryBus;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Config _config;
    private readonly IClock _clock;

    private readonly Dictionary<int, AccountRecordV6> _accountRecords = new();
    private readonly Dictionary<int, AccountBaseState> _accountStates = new();
    private readonly Dictionary<int, VirtualAccount> _accounts = new();
    private readonly Dictionary<int, QuantInfra.Sdk.Strategies.Strategy> _strategyRecords = new();
    private readonly Dictionary<int, StrategyState> _strategyStates = new();
    private readonly Dictionary<int, Domain.Strategies.Strategy> _strategies = new();
    

    public InMemoryState(Config config, IClock clock, IEventBus eventBus, IQueryBus queryBus, ILoggerFactory loggerFactory)
    {
        _config = config;
        _clock = clock;
        _eventBus = eventBus;
        _queryBus = queryBus;
        _loggerFactory = loggerFactory;
    }
    
    public IReadOnlyDictionary<int, AccountRecordV6> AccountRecords => _accountRecords;
    public IReadOnlyDictionary<int, AccountBaseState> AccountStates => _accountStates;
    public IReadOnlyDictionary<int, QuantInfra.Sdk.Strategies.Strategy> StrategyRecords => _strategyRecords;
    public IReadOnlyDictionary<int, StrategyState> StrategyStates => _strategyStates;

    public void Handle(AccountCreatedEvt evt)
    {
        if (evt.Account.AccountType != AccountType.VirtualAccount) 
            throw new InvalidOperationException("Only virtual accounts are supported for backtesting");
        _accountRecords.Add(evt.AccountId, evt.Account);
        var state = AccountBaseState.CreateNewState(evt.Account, _eventBus, _loggerFactory);
        _accountStates.Add(evt.Account.AccountId, state);
        var va = new VirtualAccount(evt.Account, state, 
            this, this, this, this, this,
            _eventBus, _queryBus, _loggerFactory, _config.LogLevel);
        _accounts.Add(evt.AccountId, va);
        va.CreateAccount(evt.Timestamp);
    }

    public void Handle(StrategyCreatedEvt evt)
    {
        _strategyRecords.Add(evt.StrategyId, evt.Strategy);
        var state = StrategyState.CreateNewState(evt.Strategy, _clock.GetCurrentInstant(), _eventBus, _loggerFactory);
        _strategyStates.Add(evt.StrategyId, state);

        var strategy = new Domain.Strategies.Strategy(state, this, _eventBus, _queryBus, _clock);
        _strategies.Add(evt.StrategyId, strategy);
    }

    public IReadOnlyCollection<int> Handle(GetAccountIdsForEndOfDay query) => _accountRecords.Keys;

    public IReadOnlyCollection<OrderStatus> Handle(GetActiveVirtualExecutorOrders query) => _accountStates.Values
        .SelectMany(s => s.Orders.Where(o => o.IsVirtual && !o.IsSuspended))
        .ToList();

    public QuantInfra.Sdk.Strategies.Strategy? Handle(GetStrategyRecord query) => _strategyRecords.GetValueOrDefault(query.StrategyId);
    public IStrategyStateReadonly? Handle(GetStrategyState query) => _strategyStates.GetValueOrDefault(query.StrategyId);
    public IStrategy? Handle(GetStrategy query) => _strategies.GetValueOrDefault(query.StrategyId);
    
    IAccountStateReadonly? IQueryHandler<GetAccount, IAccountStateReadonly?>.Handle(GetAccount query) => _accountStates.GetValueOrDefault(query.AccountId);
    public IAccount? Handle(GetAccount query) => _accounts.GetValueOrDefault(query.AccountId);
    ITradingAccount? IQueryHandler<GetAccount, ITradingAccount?>.Handle(GetAccount query) => _accounts.GetValueOrDefault(query.AccountId);
    AccountRecordV6? IQueryHandler<GetAccount, AccountRecordV6?>.Handle(GetAccount query) => _accountRecords.GetValueOrDefault(query.AccountId);
    
    
    private long _eventId = 100000000;
    public long GetNextEventId() => _eventId++;
    
    private long _execId = 10000000;
    public long GetNextExecId() => _execId++;
    
    private int _balanceOperationId = 10000;
    public int GetNextBalanceOperationId() => _balanceOperationId++;

    private long _orderId = 1000000;
    public long GetNextOrderId() => _orderId++;

    private long _tradeId = 100000;
    public long GetNextTradeId() => _tradeId++;
    
    public Dictionary<int, LastPrice> LastPrices { get; } = new();
}

internal static class InMemoryStateConfigurationExtensions
{
    public static IServiceCollection AddInMemoryState(this IServiceCollection sc) => sc
        .AddSingleton<InMemoryState>()
        
        .AddSingleton<IEventIdProvider>(sp => sp.GetRequiredService<InMemoryState>())
        .AddSingleton<IExecIdProvider>(sp => sp.GetRequiredService<InMemoryState>())
        .AddSingleton<IBalanceOperationIdProvider>(sp => sp.GetRequiredService<InMemoryState>())
        .AddSingleton<IOrderIdProvider>(sp => sp.GetRequiredService<InMemoryState>())
        .AddSingleton<ITradeIdProvider>(sp => sp.GetRequiredService<InMemoryState>())
        .AddSingleton<ILastContractPricesStore>(sp => sp.GetRequiredService<InMemoryState>())
        
        .AddSingleton<IEventHandler<AccountCreatedEvt>>(sp => sp.GetRequiredService<InMemoryState>())
        .AddSingleton<IEventHandler<StrategyCreatedEvt>>(sp => sp.GetRequiredService<InMemoryState>())
        
        .AddSingleton<IQueryHandler<GetStrategyRecord, QuantInfra.Sdk.Strategies.Strategy?>>(sp => sp.GetRequiredService<InMemoryState>())
        .AddSingleton<IQueryHandler<GetStrategyState, IStrategyStateReadonly?>>(sp => sp.GetRequiredService<InMemoryState>())
        .AddSingleton<IQueryHandler<GetStrategy, IStrategy?>>(sp => sp.GetRequiredService<InMemoryState>())
        
        .AddSingleton<IQueryHandler<GetAccount, IAccountStateReadonly?>>(sp => sp.GetRequiredService<InMemoryState>())
        .AddSingleton<IQueryHandler<GetAccount, IAccount?>>(sp => sp.GetRequiredService<InMemoryState>())
        .AddSingleton<IQueryHandler<GetAccount, ITradingAccount?>>(sp => sp.GetRequiredService<InMemoryState>())
        .AddSingleton<IQueryHandler<GetAccount, AccountRecordV6?>>(sp => sp.GetRequiredService<InMemoryState>())
        
        .AddSingleton<IQueryHandler<GetAccountIdsForEndOfDay, IReadOnlyCollection<int>>>(sp => sp.GetRequiredService<InMemoryState>())
        .AddSingleton<IQueryHandler<GetActiveVirtualExecutorOrders, IReadOnlyCollection<OrderStatus>>>(sp => sp.GetRequiredService<InMemoryState>());
}
