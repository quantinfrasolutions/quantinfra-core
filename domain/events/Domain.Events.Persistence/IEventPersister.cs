using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.Events.Accounts.AccountsService.Projections;
using QuantInfra.Domain.Events.Accounts.Management;
using QuantInfra.Domain.Events.Strategies.AccountsService;
using QuantInfra.Domain.Events.Strategies.Management;

namespace QuantInfra.Domain.Events.Persistence;

public interface IEventPersister : IDisposable
{
    // Account primary events
    void RecordEvent(string asName, AccountCreatedEvt ac);
    void RecordEvent(string asName, SubaccountAssignedEvt sa);
    void RecordEvent(string accountServiceName, AccountEndOfDayEvt evt);
    void RecordEvent(string asName, BalanceOperationProcessedEvt evt);
    void RecordEvent(string asName, ExecutionReportEvt evt);
    void RecordEvent(string asName, OrderCancelRejectEvt evt);
    void RecordEvent(string asName, ShareCountUpdatedEvt evt);
    void RecordEvent(string asName, SharePriceUpdatedEvt evt);
    void RecordEvent(string accountServiceName, TradeEvt evt);
    void RecordEvent(string asName, NewOrderSingleExternalCreatedEvt evt);
    void RecordEvent(string asName, OrderCancelRequestExternalCreatedEvt evt);
    void RecordEvent(string asName, NewTradeInDeadLetterQueueEvt evt);
    void RecordEvent(string asName, NewUnmappedContractRegisteredEvt evt);
    void RecordEvent(string asName, AccountReconciliationStatusChangedEvt evt);
    void RecordEvent(string asName, BrokerAccountNeedsOrdersReconciliationEvt evt);
    void RecordEvent(string asName, BrokerAccountOrdersReconciledEvt evt);
    void RecordEvent(string asName, BrokerAccountNeedsTradesReconciliationEvt evt);
    void RecordEvent(string asName, BrokerAccountTradesReconciledEvt evt);
    
    // Account projection events
    void RecordProjection(string asName, BalanceHistoryProjectionEvt evt);
    void RecordProjection(string asName, PositionChangedEvt evt);
    void RecordProjection(RealizedPnLAccruedEvt evt);
    void RecordProjection(string asName, SharePriceHistoryProjectionEvt evt);
    void RecordProjection(UnrealizedPnLAccruedEvt evt);
    
    // Strategy primary events
    void RecordEvent(string asName, StrategyCreatedEvt strat);
    void RecordEvent(string asName, StrategyLastCalculationTsUpdatedEvt evt);
    void RecordEvent(string asName, StrategyInternalStateUpdatedEvt evt);
    void RecordEvent(string asName, ExternalExecutionReportEvt evt);
    void RecordEvent(string asName, TradingClientConfigurationChangedEvt evt);
    void RecordEvent(string asName, OrderReplaceRequestExternalCreatedEvt evt);
}

public interface IEventPersisterFactory
{
    IEventPersister Create();
    Task<long> GetLastSavedEventIdAsync(string accountServiceName);
}