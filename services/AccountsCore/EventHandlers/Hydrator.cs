using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Account.Execution.State;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.Events.Accounts.AccountsService;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.Events.Accounts.Management;
using QuantInfra.Domain.Events.Strategies.AccountsService;
using QuantInfra.Domain.Events.Strategies.Management;
using QuantInfra.Domain.Strategies;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Services.AccountsCore.State;

namespace QuantInfra.Services.AccountsCore.EventHandlers;

public class Hydrator : 
    IExternalEventHandler<AccountCreatedEvt>,
    IExternalEventHandler<SubaccountAssignedEvt>,
    IExternalEventHandler<TradingClientConfigurationChangedEvt>,
    
    IExternalEventHandler<AccountEndOfDayEvt>,
    IExternalEventHandler<AccountReconciliationStatusChangedEvt>,
    IExternalEventHandler<BalanceOperationProcessedEvt>,
    IExternalEventHandler<ExecutionReportEvt>,
    IExternalEventHandler<ExternalExecutionReportEvt>,
    IExternalEventHandler<NewOrderSingleExternalCreatedEvt>,
    IExternalEventHandler<NewTradeInDeadLetterQueueEvt>,
    IExternalEventHandler<NewUnmappedContractRegisteredEvt>,
    IExternalEventHandler<OrderCancelRejectEvt>,
    IExternalEventHandler<OrderCancelRequestExternalCreatedEvt>,
    IExternalEventHandler<OrderReplaceRequestExternalCreatedEvt>,
    IExternalEventHandler<ShareCountUpdatedEvt>,
    IExternalEventHandler<SharePriceUpdatedEvt>,
    IExternalEventHandler<TradeEvt>,
    IExternalEventHandler<BrokerAccountNeedsOrdersReconciliationEvt>,
    IExternalEventHandler<BrokerAccountOrdersReconciledEvt>,
    IExternalEventHandler<BrokerAccountNeedsTradesReconciliationEvt>,
    IExternalEventHandler<BrokerAccountTradesReconciledEvt>,
    
    IExternalEventHandler<StrategyCreatedEvt>,
    IExternalEventHandler<StrategyInternalStateUpdatedEvt>,
    IExternalEventHandler<StrategyLastCalculationTsUpdatedEvt>
{
    private readonly AccountServiceState _state;
    private readonly ILoggerFactory _loggerFactory;
    private long _expectedEventId = -1;
    private readonly ServiceProvider _serviceProvider;
    private readonly IEventBus _eventBus;

    public Hydrator(AccountServiceState state, ILoggerFactory loggerFactory)
    {
        _state = state;
        _loggerFactory = loggerFactory;

        _serviceProvider = new ServiceCollection()
            .UseSingletonInMemoryBus()
            .AddSingleton(this)
            .AddSingleton<IExternalEventHandler<AccountCreatedEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<SubaccountAssignedEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<TradingClientConfigurationChangedEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<AccountEndOfDayEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<AccountReconciliationStatusChangedEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<BalanceOperationProcessedEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<ExecutionReportEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<ExternalExecutionReportEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<OrderCancelRejectEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<ShareCountUpdatedEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<SharePriceUpdatedEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<TradeEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<NewOrderSingleExternalCreatedEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<OrderCancelRequestExternalCreatedEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<OrderReplaceRequestExternalCreatedEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<NewTradeInDeadLetterQueueEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<NewUnmappedContractRegisteredEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<BrokerAccountNeedsOrdersReconciliationEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<BrokerAccountOrdersReconciledEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<BrokerAccountNeedsTradesReconciliationEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<BrokerAccountTradesReconciledEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<StrategyCreatedEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<StrategyInternalStateUpdatedEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .AddSingleton<IExternalEventHandler<StrategyLastCalculationTsUpdatedEvt>>(sp => sp.GetRequiredService<Hydrator>())
            .BuildServiceProvider();
        
        
        _eventBus = _serviceProvider.GetRequiredService<IEventBus>();
    }

    public void ProcessBatch(IReadOnlyList<IEvent> events)
    {
        var eventBus = _serviceProvider.GetRequiredService<IEventBus>();
        foreach (var e in events) eventBus.ApplyAnonymousExternalEvent(e);
    }

    public void Apply(AccountCreatedEvt e)
    {
        ValidateExpectedEventId(e);
        _state.AccountRecords.Add(e.AccountId, e.Account);
        var state = e.Account.AccountType switch
        {
            AccountType.VirtualAccount or AccountType.StrategySubAccount => AccountBaseState.CreateNewState(e.Account, _eventBus, _loggerFactory),
            AccountType.BrokerAccount => BrokerAccountState.CreateNewState(e.Account, e.Timestamp, _eventBus, _loggerFactory),
            _ => throw new NotSupportedException($"Account type {e.Account.AccountType} is not supported")
        };
        _state.AccountStates.Add(e.AccountId, state);
        // state.Initialize(_serviceProvider.GetRequiredService<IEventBus>(), null!);
    }
    
    public void Apply(SubaccountAssignedEvt e)
    {
        ValidateExpectedEventId(e);
        _state.UpdateEventId(e.EventId);
    }

    public void Apply(TradingClientConfigurationChangedEvt e)
    {
        ValidateExpectedEventId(e);
        _state.UpdateEventId(e.EventId);
    }
    
    // All accounts
    public void Apply(BalanceOperationProcessedEvt e) => ProcessAccountEvent(e, s =>
    {
        s.Apply(e, true);
        _state.UpdateBalanceOperationId(e.BalanceOperation.BalanceOperationId);
    });
    
    public void Apply(ExecutionReportEvt e) => ProcessAccountEvent(e, s =>
    {
        s.Apply(e, true);
        _state.UpdateExecId(e.ExecutionReport.ExecId);
        _state.UpdateOrderId(e.ExecutionReport.OrderId);
    });
    
    public void Apply(TradeEvt e) => ProcessAccountEvent(e, s =>
    {
        s.Apply(e, true);
        _state.UpdateTradeId(e.Trade.TradeId);
    });
    
    public void Apply(OrderCancelRejectEvt e) => ProcessAccountEvent(e, s => s.Apply(e, true));
    public void Apply(AccountEndOfDayEvt e) => ProcessAccountEvent(e, s => s.Apply(e, true));
    public void Apply(ShareCountUpdatedEvt e) => ProcessAccountEvent(e, s => s.Apply(e, true));
    public void Apply(SharePriceUpdatedEvt e) => ProcessAccountEvent(e, s => s.Apply(e, true));

    // Broker accounts
    public void Apply(NewOrderSingleExternalCreatedEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, true));
    public void Apply(OrderCancelRequestExternalCreatedEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, true));
    public void Apply(OrderReplaceRequestExternalCreatedEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, true));
    public void Apply(NewTradeInDeadLetterQueueEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, true));
    public void Apply(NewUnmappedContractRegisteredEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, true));
    public void Apply(AccountReconciliationStatusChangedEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, true));
    public void Apply(ExternalExecutionReportEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, true));
    public void Apply(BrokerAccountNeedsOrdersReconciliationEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, true));
    public void Apply(BrokerAccountOrdersReconciledEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, true));
    public void Apply(BrokerAccountNeedsTradesReconciliationEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, true));
    public void Apply(BrokerAccountTradesReconciledEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, true));
    
    
    public void Apply(StrategyCreatedEvt e)
    {
        ValidateExpectedEventId(e);
        _state.StrategyRecords.Add(e.StrategyId, e.Strategy);
        var state = StrategyState.CreateNewState(e.Strategy, e.Timestamp, _eventBus, _loggerFactory);
        _state.StrategyStates.Add(e.StrategyId, state);
        // state.Initialize(_serviceProvider.GetRequiredService<IEventBus>(), null!);
    }
    
    public void Apply(StrategyInternalStateUpdatedEvt evt) => ProcessStrategyEvent(evt, s => s.Apply(evt));
    public void Apply(StrategyLastCalculationTsUpdatedEvt evt) => ProcessStrategyEvent(evt, s => s.Apply(evt));

    private void ProcessAccountEvent(IAccountEventBase e, Action<AccountBaseState> applyMethod)
    {
        ValidateExpectedEventId(e);
        _state.UpdateEventId(e.EventId);
        var state = _state.AccountStates[e.AccountId];
        if (state.Version + 1 != e.Version)
        {
            throw new InvalidOperationException($"Expected version {state.Version + 1}, got {e.Version}, accountId={e.AccountId}, eventId={e.EventId}");
        }
        applyMethod.Invoke(state);
    }
    
    private void ProcessBrokerAccountEvent(IAccountEventBase e, Action<BrokerAccountState> applyMethod)
    {
        ValidateExpectedEventId(e);
        var state = (BrokerAccountState)_state.AccountStates[e.AccountId];
        if (state.Version + 1 != e.Version)
        {
            throw new InvalidOperationException($"Expected version {state.Version}, got {e.Version}, accountId={e.AccountId}, eventId={e.EventId}");
        }
        applyMethod.Invoke(state);
    }
    
    private void ProcessStrategyEvent(IStrategyEventBase e, Action<StrategyState> applyMethod)
    {
        ValidateExpectedEventId(e);
        _state.UpdateEventId(e.EventId);
        var state = _state.StrategyStates[e.StrategyId];
        if (state.Version + 1 != e.Version)
        {
            throw new InvalidOperationException($"Expected version {state.Version}, got {e.Version}, strategyId={e.StrategyId}, eventId={e.EventId}");
        }
        applyMethod.Invoke(state);
    }
    
    private void ValidateExpectedEventId(IEvent e)
    {
        if (_expectedEventId == -1) _expectedEventId = e.EventId;
        if (e.EventId != _expectedEventId)
            throw new InvalidOperationException($"Expected event id {_expectedEventId}, got {e.EventId}");
        _expectedEventId++;
    }
}