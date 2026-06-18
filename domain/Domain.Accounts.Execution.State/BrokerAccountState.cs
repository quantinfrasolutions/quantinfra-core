using Common.Trading.Positions;
using Common.Utils.Collections;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Accounts.AccountStates;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading.ExternalAccounts;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Domain.Account.Execution.State;

public class BrokerAccountState : AccountBaseState, IBrokerAccountStateReadonly
{
    public BrokerAccountState(string accountServiceName,
        int accountId,
        PositionAccounting positionAccounting,
        IReadOnlyDictionary<int, decimal> balances,
        IEnumerable<OrderStatus> orders,
        IEnumerable<Position> positions,
        decimal sharePrice,
        decimal shareCount,
        decimal hwm,
        decimal investment,
        decimal realizedPnLSinceLastMtm,
        long version,
        Instant lastReconciliationDt,
        Instant lastReceivedTradeTs,
        IEnumerable<string> lastReceivedTradeIds,
        IEnumerable<ExecutionReport> pendingFills,
        Instant lastReceivedBalanceOperationTs,
        IEnumerable<string> lastReceivedBalanceOperationIds,
        IEnumerable<ExternalTradeRecord> tradesDeadLetterQueue,
        IEnumerable<string> unmappedExternalContractIds,
        IReadOnlyDictionary<string, Instant> usedContractIds,
        bool isReconciled,
        bool needsOrdersReconciliation,
        bool needsTradesReconciliation,
        IEventBus eventBus, ILoggerFactory loggerFactory
    ) : base(accountServiceName, accountId, positionAccounting, balances, orders, positions, sharePrice, 
        shareCount, hwm, investment, realizedPnLSinceLastMtm, version, eventBus, loggerFactory)
    {
        LastReconciliationDt = lastReconciliationDt;
        LastReceivedTradeTs = lastReceivedTradeTs;
        _lastReceivedTradeIds = lastReceivedTradeIds.ToList();
        _pendingFills = pendingFills.ToDictionary(x => x.ExecId);
        LastReceivedBalanceOperationTs = lastReceivedBalanceOperationTs;
        IsReconciled = isReconciled;
        NeedsOrdersReconciliation = needsOrdersReconciliation;
        NeedsTradesReconciliation = needsTradesReconciliation;
        _lastReceivedBalanceOperationIds = lastReceivedBalanceOperationIds.ToList();
        _tradesDeadLetterQueue = tradesDeadLetterQueue.ToList();
        _unmappedExternalContractIds = unmappedExternalContractIds.ToHashSet();
        _usedContractIds = usedContractIds.CopyAsDictionary();
    }
    
    #region Properties
    
    public Instant LastReconciliationDt { get; private set; }
    public Instant LastReceivedTradeTs { get; private set; }
    
    private readonly List<string> _lastReceivedTradeIds;
    public IReadOnlyCollection<string> LastReceivedTradeIds => _lastReceivedTradeIds;

    private readonly Dictionary<long, ExecutionReport> _pendingFills;
    public IReadOnlyDictionary<long, ExecutionReport> PendingFills => _pendingFills;
    
    public Instant LastReceivedBalanceOperationTs { get; private set; }
    
    private readonly List<string> _lastReceivedBalanceOperationIds;
    public IReadOnlyCollection<string> LastReceivedBalanceOperationIds => _lastReceivedBalanceOperationIds;

    private readonly List<ExternalTradeRecord> _tradesDeadLetterQueue;
    public IReadOnlyCollection<ExternalTradeRecord> TradesDeadLetterQueue => _tradesDeadLetterQueue;
    
    private readonly HashSet<string> _unmappedExternalContractIds;
    public IReadOnlyCollection<string> UnmappedExternalContractIds => _unmappedExternalContractIds;

    private readonly Dictionary<string, Instant> _usedContractIds = new();
    public IReadOnlyDictionary<string, Instant> UsedContractIds => _usedContractIds;
    
    public bool IsReconciled { get; private set; }
    public bool NeedsOrdersReconciliation { get; private set; }
    public bool NeedsTradesReconciliation { get; private set; }
    
    #endregion

    #region Events
    
    public override void Apply(ExecutionReportEvt evt, bool emit)
    {
        if (base.ApplyInternal(evt, emit) && evt.ExecutionReport.ExecType == ExecType.Fill)
        {
            _pendingFills.Add(evt.ExecutionReport.ExecId, evt.ExecutionReport);
        }
    }
    
    public void Apply(ExternalExecutionReportEvt evt, bool emit)
    {
        if (!base.Apply(evt)) return;
        ApplyExecutionReport(evt.ExecutionReport);
        if (evt.ExecutionReport.ExecType == ExecType.Fill)
        {
            _pendingFills.Add(evt.ExecutionReport.ExecId, evt.ExecutionReport);            
        }
        if (evt.BrokerType == BrokerType.BinanceUsdmFutures && !string.IsNullOrEmpty(evt.ExternalContractId))
        {
            _usedContractIds[evt.ExternalContractId] = evt.ExecutionReport.TransactTime;
            // TODO: cleanup contracts older than 7 days
        }
        
        if (emit) Emit(evt);
    }

    public override void Apply(TradeEvt evt, bool emit)
    {
        if (!base.ApplyInternal(evt, emit)) return;
        
        if (evt.Trade.Dt < LastReceivedTradeTs)
        {
            throw new InvalidOperationException("Trade timestamp must be greater than or equal to LastReceivedTradeTs");
        }
        
        if (evt.Trade.Dt > LastReceivedTradeTs)
        {
            _lastReceivedTradeIds.Clear();
            LastReceivedTradeTs = evt.Trade.Dt;
        }

        if (evt.Trade.ExternalTradeId != null)
        {
            _lastReceivedTradeIds.Add(evt.Trade.ExternalTradeId);
        }

        if (evt.Trade.ExecId is not null)
        {
            _pendingFills.Remove(evt.Trade.ExecId.Value);
        }
    }

    public override void Apply(BalanceOperationProcessedEvt evt, bool emit)
    {
        if (!base.ApplyInternal(evt, emit)) return;
        
        if (evt.BalanceOperation.Dt < LastReceivedBalanceOperationTs)
        {
            throw new InvalidOperationException("Balance operation timestamp must be greater than or equal to LastReceivedBalanceOperationTs");
        }
        
        if (evt.BalanceOperation.Dt > LastReceivedBalanceOperationTs)
        {
            _lastReceivedBalanceOperationIds.Clear();
            LastReceivedBalanceOperationTs = evt.BalanceOperation.Dt;
        }
        
        if (evt.BalanceOperation.ExternalId != null)
        {
            _lastReceivedBalanceOperationIds.Add(evt.BalanceOperation.ExternalId);
        }
        
    }

    public void Apply(NewOrderSingleExternalCreatedEvt evt, bool emit)
    {
        if (!base.Apply(evt)) return;
        ApplyExecutionReport(evt.ExecutionReport);
        if (evt.BrokerType == BrokerType.BinanceUsdmFutures)
        {
            _usedContractIds[evt.Order.ExternalContractId] = evt.ExecutionReport.TransactTime;
            // TODO: cleanup contracts older than 7 days
        }
        if (emit) Emit(evt);
    }

    public void Apply(OrderCancelRequestExternalCreatedEvt evt, bool emit)
    {
        if (!base.Apply(evt)) return;
        ApplyExecutionReport(evt.ExecutionReport);
        if (emit) Emit(evt);   
    }

    public void Apply(OrderReplaceRequestExternalCreatedEvt evt, bool emit)
    {
        if (!base.Apply(evt)) return;
        ApplyExecutionReport(evt.ExecutionReport);
        if (emit) Emit(evt);
    }

    public void Apply(NewUnmappedContractRegisteredEvt evt, bool emit)
    {
        if (!base.Apply(evt)) return;
        _unmappedExternalContractIds.Add(evt.ExternalContractId);
        if (emit) Emit(evt);
    }

    public void Apply(NewTradeInDeadLetterQueueEvt evt, bool emit)
    {
        if (!base.Apply(evt)) return;
        _tradesDeadLetterQueue.Add(evt.Trade);
        if (emit) Emit(evt);
    }
    
    public void Apply(AccountReconciliationStatusChangedEvt evt, bool emit)
    {
        if (!base.Apply(evt)) return;
        IsReconciled = !evt.NeedsReconciliation;
        if (emit) Emit(evt);
    }
    
    public void Apply(BrokerAccountNeedsTradesReconciliationEvt evt, bool emit)
    {
        if (!base.Apply(evt)) return;
        NeedsTradesReconciliation = true;
        if (emit) Emit(evt);
    }
    
    public void Apply(BrokerAccountTradesReconciledEvt evt, bool emit)
    {
        if (!base.Apply(evt)) return;
        NeedsTradesReconciliation = false;
        if (emit) Emit(evt);
    }

    public void Apply(BrokerAccountNeedsOrdersReconciliationEvt evt, bool emit)
    {
        if (!base.Apply(evt)) return;
        NeedsOrdersReconciliation = true;
        if (emit) Emit(evt);
    }
    
    public void Apply(BrokerAccountOrdersReconciledEvt evt, bool emit)
    {
        if (!base.Apply(evt)) return;
        NeedsOrdersReconciliation = false;
        if (emit) Emit(evt);
    }
    
    #endregion
    
    public new static BrokerAccountState CreateNewState(AccountRecordV6 account, Instant createdAt, IEventBus eventBus, ILoggerFactory loggerFactory) 
        => new(
            account.AccountServiceName, account.AccountId, account.PositionAccounting, 
            new Dictionary<int, decimal>(), 
            Array.Empty<OrderStatus>(), 
            Array.Empty<Position>(), 
            1, 0, 1, 0, 0, 0,
            Instant.MinValue, createdAt, new List<string>(),
            Array.Empty<ExecutionReport>(), createdAt,
            Array.Empty<string>(), Array.Empty<ExternalTradeRecord>(), 
            Array.Empty<string>(), new Dictionary<string, Instant>(), 
            false, false, false,
            eventBus, loggerFactory
        );

    public new BrokerAccountStateReadonly ToAccountStateReadonly() => new(AccountServiceName, AccountId, PositionAccounting, 
        Balances.Copy(),
        Positions.ToList(), Orders.ToList(), Investment, SharePrice, ShareCount, HWM, RealizedPnLSinceLastMtm, Version,
        LastReconciliationDt, LastReceivedTradeTs, LastReceivedTradeIds.ToList(), PendingFills.Copy(), 
        LastReceivedBalanceOperationTs,
        LastReceivedBalanceOperationIds.ToList(),
        TradesDeadLetterQueue.ToList(), UnmappedExternalContractIds.ToList(), UsedContractIds.Copy(), 
        IsReconciled, NeedsOrdersReconciliation, NeedsTradesReconciliation
    );
    
    public static new BrokerAccountState FromAccountStateReadonly(IBrokerAccountStateReadonly state, IEventBus eventBus, ILoggerFactory loggerFactory) =>
        new(state.AccountServiceName, state.AccountId, state.PositionAccounting, state.Balances, state.Orders,
            state.Positions, state.SharePrice, state.ShareCount, state.HWM, state.Investment,
            state.RealizedPnLSinceLastMtm, state.Version, state.LastReconciliationDt, state.LastReceivedTradeTs,
            state.LastReceivedTradeIds, state.PendingFills.Values, state.LastReceivedBalanceOperationTs,
            state.LastReceivedBalanceOperationIds, state.TradesDeadLetterQueue, state.UnmappedExternalContractIds,
            state.UsedContractIds, state.IsReconciled, state.NeedsOrdersReconciliation, state.NeedsTradesReconciliation,
            eventBus, loggerFactory
        );
}