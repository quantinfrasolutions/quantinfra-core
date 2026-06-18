using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Domain.Account.Execution.State;
using QuantInfra.Domain.Accounts.AccountStateClientManager.Events;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.Events.Accounts.AccountsService;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Accounts.AccountStates;

[assembly:InternalsVisibleTo("Tests.v6.E2E")]

namespace QuantInfra.Domain.Accounts.AccountStateClientManager;

public class AccountsStateManager : 
    IExternalEventHandler<AccountEndOfDayEvt>,
    IExternalEventHandler<BalanceOperationProcessedEvt>,
    IExternalEventHandler<ExecutionReportEvt>,
    IExternalEventHandler<ExternalExecutionReportEvt>,
    IExternalEventHandler<OrderCancelRejectEvt>,
    IExternalEventHandler<ShareCountUpdatedEvt>,
    IExternalEventHandler<SharePriceUpdatedEvt>,
    IExternalEventHandler<TradeEvt>,
    IExternalEventHandler<NewOrderSingleExternalCreatedEvt>,
    IExternalEventHandler<OrderCancelRequestExternalCreatedEvt>,
    IExternalEventHandler<OrderReplaceRequestExternalCreatedEvt>,
    IExternalEventHandler<NewTradeInDeadLetterQueueEvt>,
    IExternalEventHandler<NewUnmappedContractRegisteredEvt>,
    IExternalEventHandler<AccountReconciliationStatusChangedEvt>,
    IExternalEventHandler<BrokerAccountNeedsOrdersReconciliationEvt>,
    IExternalEventHandler<BrokerAccountOrdersReconciledEvt>,
    IExternalEventHandler<BrokerAccountNeedsTradesReconciliationEvt>,
    IExternalEventHandler<BrokerAccountTradesReconciledEvt>,

    IAsyncQueryResponseHandler<GetAccountState, AccountStateReadonly?>,
    IAsyncQueryResponseHandler<GetBrokerAccountState, BrokerAccountStateReadonly?>,
    
    IQueryHandler<GetAccount, IAccountStateReadonly?>,
    IQueryHandler<GetAccount, IBrokerAccountStateReadonly?>

{
    protected readonly IEventBus EventBus;
    private readonly IQueryBus _queryBus;
    private readonly IAccountsServiceApiReadonly _serviceApi;
    protected readonly ILoggerFactory LoggerFactory;
    private readonly ILogger _logger;
    protected readonly IClock Clock;
    
    private readonly AsyncRequestsManager<int> _requests = new();
    protected readonly Dictionary<int, AccountBaseState> Accounts = new();

    public AccountsStateManager(
        IEventBus eventBus, 
        IQueryBus queryBus, 
        IAccountsServiceApiReadonly serviceApi, 
        ILoggerFactory loggerFactory, 
        IClock clock
    )
    {
        EventBus = eventBus;
        _queryBus = queryBus;
        _serviceApi = serviceApi;
        LoggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<AccountsStateManager>();
        Clock = clock;
    }

    public IReadOnlyDictionary<int, IAccountStateReadonly> AccountStates => 
        Accounts.ToDictionary(kv => kv.Key, kv => (IAccountStateReadonly)kv.Value);
    
    public void Apply(BalanceOperationProcessedEvt e) => ProcessEvent(e, s => s.Apply(e, false));
    public void Apply(ExecutionReportEvt e) => ProcessEvent(e, s => s.Apply(e, false));
    public void Apply(TradeEvt e) => ProcessEvent(e, s => s.Apply(e, false));
    public void Apply(OrderCancelRejectEvt e) => ProcessEvent(e, s => s.Apply(e, false));
    public void Apply(AccountEndOfDayEvt e) => ProcessEvent(e, s => s.Apply(e, false));
    public void Apply(ShareCountUpdatedEvt e) => ProcessEvent(e, s => s.Apply(e, false));
    public void Apply(SharePriceUpdatedEvt e) => ProcessEvent(e, s => s.Apply(e, false));
    
    public void Apply(NewOrderSingleExternalCreatedEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, false));
    public void Apply(OrderCancelRequestExternalCreatedEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, false));
    public void Apply(OrderReplaceRequestExternalCreatedEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, false));
    
    public void Apply(ExternalExecutionReportEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, false));
    public void Apply(NewTradeInDeadLetterQueueEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, false));
    public void Apply(NewUnmappedContractRegisteredEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, false));
    public void Apply(AccountReconciliationStatusChangedEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, false));
    public void Apply(BrokerAccountNeedsOrdersReconciliationEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, false));
    public void Apply(BrokerAccountOrdersReconciledEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, false));
    public void Apply(BrokerAccountNeedsTradesReconciliationEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, false));
    public void Apply(BrokerAccountTradesReconciledEvt e) => ProcessBrokerAccountEvent(e, s => s.Apply(e, false));

    protected virtual void OnMissingVersion(AccountBaseState state, long receivedVersion)
    {
        var accountId = state.AccountId;
        _logger.LogInformation($"Account {accountId} is outdated (expected={state.Version}, received {receivedVersion})");
        Accounts.Remove(accountId); // State is obsolete
            
        // If there are no existing requests, send a new one
        if (_requests.TryCreateRequest(accountId, out var requestId))
        {
            _logger.LogInformation($"Requesting state for account {accountId}, requestId={requestId}");
            _queryBus.SendAsyncQuery<GetAccountState, AccountStateReadonly?>(new(requestId, state.AccountServiceName, accountId, true));
            EventBus.Emit(new AccountMissingVersionEvt(accountId, Clock.GetCurrentInstant()));
        }
    }
    
    private void ProcessEvent(IAccountEventBase evt, Action<AccountBaseState> applyMethod)
    {
        var accountId = evt.AccountId;
        var state = Accounts.GetValueOrDefault(accountId);
        if (state == null) 
            return;
        
        try
        {
            applyMethod(state);
        }
        catch (MissingVersionException)
        {
            OnMissingVersion(state, evt.Version);
        }
    }

    private void ProcessBrokerAccountEvent(IAccountEventBase evt, Action<BrokerAccountState> applyMethod)
    {
        var accountId = evt.AccountId;
        var state = Accounts.GetValueOrDefault(accountId);
        if (state == null || state is not BrokerAccountState baState) return;
        
        try
        {
            applyMethod(baState);
        }
        catch (MissingVersionException)
        {
            OnMissingVersion(state, evt.Version);
        }
    }

    public void Handle(AsyncQueryResponse<GetAccountState, AccountStateReadonly?> response)
    {
        _logger.LogDebug($"Received account state for account {response.Result?.AccountId}, version={response.Result?.Version}");
        
        var newState = response.Result;
        if (newState == null) return;

        OnSnapshotReceived(newState, response.RequestId);
    }
    
    public void Handle(AsyncQueryResponse<GetBrokerAccountState, BrokerAccountStateReadonly?> response)
    {
        _logger.LogDebug($"Received broker account state for account {response.Result?.AccountId}, version={response.Result?.Version}");
        
        var newState = response.Result;
        if (newState == null) return;

        OnSnapshotReceived(newState, response.RequestId);
    }

    private void OnSnapshotReceived(AccountStateReadonly newState, Guid requestId)
    {
        var accountId = newState.AccountId;
        
        var existingState = Accounts.GetValueOrDefault(accountId);
        if (existingState?.Version == newState.Version)
        {
            _logger.LogDebug($"Account {accountId} is up to date, version={newState.Version}");
            CompleteAccountStateRequest(requestId, newState);
            return;
        }

        var account = _queryBus.Query<GetAccount, AccountRecordV6?>(new(accountId));
        if (account == null)
        {
            CompleteAccountStateRequest(requestId, newState);
            return;
        }

        var accState = InstantiateAccount(accountId, account, newState);
        Accounts[accountId] = accState;
        _logger.LogDebug($"Added account {accountId}, version={newState.Version}");
        CompleteAccountStateRequest(requestId, newState);
        EventBus.Emit(new AccountStateReconciledEvt(accountId, Clock.GetCurrentInstant()));
    }
    
    protected virtual AccountBaseState InstantiateAccount(int accountId, AccountRecordV6 account, AccountStateReadonly receivedState) => receivedState switch
    {
        BrokerAccountStateReadonly ba => new BrokerAccountState(ba.AccountServiceName, ba.AccountId, ba.PositionAccounting, ba.Balances,
            ba.Orders, ba.Positions, ba.SharePrice, ba.ShareCount, ba.HWM, ba.Investment, ba.RealizedPnLSinceLastMtm,
            ba.Version, ba.LastReconciliationDt, ba.LastReceivedTradeTs, ba.LastReceivedTradeIds, ba.PendingFills.Values,
            ba.LastReceivedBalanceOperationTs, ba.LastReceivedBalanceOperationIds,
            ba.TradesDeadLetterQueue, ba.UnmappedExternalContractIds, ba.UsedContractIds, 
            ba.IsReconciled, ba.NeedsOrdersReconciliation, ba.NeedsTradesReconciliation, 
            EventBus, LoggerFactory),
        _ => new AccountBaseState(account.AccountServiceName, accountId, receivedState.PositionAccounting,
            receivedState.Balances, receivedState.Orders, receivedState.Positions, receivedState.SharePrice, receivedState.ShareCount, 
            receivedState.HWM, receivedState.Investment, receivedState.RealizedPnLSinceLastMtm, receivedState.Version,
            EventBus, LoggerFactory)
    };

    private void CompleteAccountStateRequest(Guid requestId, AccountStateReadonly? result)
    {
        _serviceApi.OnAccountSnapshot(result, requestId);
        // Remove request, if it exists
        _requests.RemoveRequest(requestId);
    }

    IAccountStateReadonly? IQueryHandler<GetAccount, IAccountStateReadonly?>.Handle(GetAccount query) => 
        Accounts.GetValueOrDefault(query.AccountId);

    public IBrokerAccountStateReadonly? Handle(GetAccount query)
    {
        var account = Accounts.GetValueOrDefault(query.AccountId);
        return account is BrokerAccountState ba ? ba : null;
    }
}