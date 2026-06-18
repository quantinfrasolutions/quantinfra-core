using System;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Account.Execution.State;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.Events.Accounts.Management;
using QuantInfra.Domain.Events.Strategies.Management;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Domain.Strategies;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Accounts.AccountStates;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Services.AccountsCore.State;

namespace QuantInfra.Services.AccountsCore.EventHandlers;

public class ManagementEvents : 
    IEventHandler<AccountCreatedEvt>,
    IEventHandler<StrategyCreatedEvt>,
    IEventHandler<TradingClientConfigurationChangedEvt>
{
    private readonly AccountServiceState _state;
    private readonly IClock _clock;
    private readonly IEventBus _eventBus;
    private readonly IQueryBus _queryBus;
    private readonly ILoggerFactory _loggerFactory;
    private readonly AccountsFactory _accountsFactory;
    // private readonly IExecutionServiceClient _executionServiceClient;
    private bool _isRealtime;

    public ManagementEvents(AccountServiceState state, IClock clock, 
        IEventBus eventBus, IQueryBus queryBus,  ILoggerFactory loggerFactory, AccountsFactory accountsFactory
        // , IExecutionServiceClient executionServiceClient 
    )
    {
        _state = state;
        _clock = clock;
        _queryBus = queryBus;
        _eventBus = eventBus;
        _loggerFactory = loggerFactory;
        _accountsFactory = accountsFactory;
        // _executionServiceClient = executionServiceClient;
    }
    
    public void EnableRealtime() => _isRealtime = true;
    
    public void Handle(AccountCreatedEvt evt)
    {
        var state = evt.Account.AccountType switch
        {
            AccountType.VirtualAccount => AccountBaseState.CreateNewState(evt.Account, _eventBus, _loggerFactory),
            AccountType.BrokerAccount => BrokerAccountState.CreateNewState(evt.Account, evt.Timestamp, _eventBus, _loggerFactory),
            AccountType.StrategySubAccount => AccountBaseState.CreateNewState(evt.Account, _eventBus, _loggerFactory),
            // AccountType.ExecutableSubAccount => ExecutableSubaccountState.CreateNewState(e.Account),
            _ => null
        };

        if (state != null)
        {
            // state.Initialize(_eventBus, _loggerFactory.CreateLogger($"Account.{state.AccountId}"));
            _state.AccountStates.Add(evt.AccountId, state);
            
            var account = _accountsFactory.GetAccount(state.AccountId);
            account.CreateAccount(evt.Timestamp);
        }
        
        // if (_isRealtime && evt.Account.AccountType == AccountType.BrokerAccount && evt.Account.TradingClientConfig is not null)
        // {
        //     _executionServiceClient.SubscribeToExternalAccountExecutions(evt.AccountId,
        //         evt.Account.TradingClientConfig.ExecutionServiceName, false, 0).RunSynchronously();
        // }
    }

    internal void InstantiateAccountState(IAccountStateReadonly accountState, AccountRecordV6 account)
    {
        var state = account.AccountType switch
        {
            AccountType.VirtualAccount => AccountBaseState.FromAccountStateReadonly(accountState, _eventBus, _loggerFactory),
            AccountType.BrokerAccount => BrokerAccountState.FromAccountStateReadonly((IBrokerAccountStateReadonly)accountState, _eventBus, _loggerFactory),
            AccountType.StrategySubAccount => AccountBaseState.FromAccountStateReadonly(accountState, _eventBus, _loggerFactory),
            // AccountType.ExecutableSubAccount => ExecutableSubaccountState.CreateNewState(e.Account),
            _ => throw new NotSupportedException($"Account type {account.AccountType} not supported")
        };

        _state.AccountStates.Add(state.AccountId, state);
    }

    internal void InstantiateStrategyState(IStrategyStateReadonly strategyState)
    {
        var state = StrategyState.FromStrategyStateReadonly(strategyState, _eventBus, _loggerFactory);
        _state.StrategyStates.Add(state.StrategyId, state);
    }
    
    public void Handle(TradingClientConfigurationChangedEvt evt)
    {
        // if (_isRealtime && evt.Config is not null)
        // {
        //     _executionServiceClient.SubscribeToExternalAccountExecutions(evt.AccountId,
        //         evt.Config.ExecutionServiceName, false, 0).RunSynchronously();
        // }
    }

    public void Handle(StrategyCreatedEvt evt)
    {
        var account = _queryBus.Query<GetAccount, AccountRecordV6?>(new(evt.Strategy.AccountId));
        if (account == null) return;
        
        var state = StrategyState.CreateNewState(evt.Strategy, _clock.GetCurrentInstant(), _eventBus, _loggerFactory);
        _state.StrategyStates.Add(evt.StrategyId, state);
        // _responseBus.HandleAsyncQuery<GetStrategyState, StrategyStateReadonly?>(new(evt.StrategyId));
    }
}