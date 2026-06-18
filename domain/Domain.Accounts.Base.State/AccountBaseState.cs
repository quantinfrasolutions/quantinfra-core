using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Common.Trading;
using Common.Trading.Positions;
using Common.Utils.Collections;
using Microsoft.Extensions.Logging;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.Events.Accounts.AccountsService.Projections;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Accounts.AccountStates;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading.Orders;
using QuantInfra.Sdk.Trading.Positions;

[assembly:InternalsVisibleTo("Domain.Accounts.Esa.Tests")]

namespace QuantInfra.Domain.Accounts.Base.State;

// TODO: current if (emit) Emit logic sends projection updates before updating the state in derived classes

public class AccountBaseState : Aggregate, IAccountStateReadonly
{ 
    [JsonConstructor]
    public AccountBaseState(string accountServiceName,
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
        IEventBus eventBus,
        ILoggerFactory loggerFactory
    ) : base(version)
    {
        AccountServiceName = accountServiceName;
        AccountId = accountId;
        PositionAccounting = positionAccounting;
        _balances = balances.ToDictionary(x => x.Key, x => x.Value);
        Orders = orders;
        AccountId = accountId;
        SharePrice = sharePrice;
        ShareCount = shareCount;
        HWM = hwm;
        Investment = investment;
        _positions = positions.ToDictionary(p => p.OpenTradeId);
        RealizedPnLSinceLastMtm = realizedPnLSinceLastMtm;
        Initialize(eventBus, loggerFactory.CreateLogger($"AccountState.{AccountId}"));
    }
        
    #region Properties
    
    [JsonPropertyName("accountServiceName")] public string AccountServiceName { get; }
    [JsonPropertyName("accountId")] public int AccountId { get; }
    [JsonPropertyName("positionAccounting")] public PositionAccounting PositionAccounting { get; }

    [JsonIgnore] private Dictionary<int, decimal> _balances;
    [JsonPropertyName("balances")] public IReadOnlyDictionary<int, decimal> Balances
    {
        get => _balances;
        internal set => _balances = value.CopyAsDictionary();
    }

    [JsonIgnore] private Dictionary<long, OrderStatus> _orders;
    [JsonPropertyName("orders")] public IEnumerable<OrderStatus> Orders
    {
        get => _orders.Values;
        internal set
        {
            _orders = value.ToDictionary(o => o.OrderId);
            ClOrdIds = _orders.Values
                .Where(o => !string.IsNullOrEmpty(o.ClOrdId) && o.AccountId == AccountId) // External orders on broker accounts may have the same ClOrdId
                .Select(o => o.ClOrdId!).ToHashSet();
        }
    }

    [JsonIgnore] public HashSet<string> ClOrdIds { get; internal set; } = new();

    [JsonIgnore] private Dictionary<long, Position> _positions;
    [JsonPropertyName("positions")] public IEnumerable<Position> Positions
    {
        get => _positions.Values;
        internal set => _positions = value.ToDictionary(p => p.OpenTradeId);
    }

    [JsonPropertyName("sharePrice")] public decimal SharePrice { get; private set; }
    [JsonPropertyName("shareCount")] public decimal ShareCount { get; private set; }
    [JsonPropertyName("hwm")] public decimal HWM { get; private set; }
    [JsonPropertyName("investment")] public decimal Investment { get; private set; }

    [JsonPropertyName("realizedPnLSinceLastMtm")] public decimal RealizedPnLSinceLastMtm { get; private set; }

    #endregion
    

    #region Primary events
    public virtual void Apply(BalanceOperationProcessedEvt evt, bool emit) => ApplyInternal(evt, emit);
    
    protected bool ApplyInternal(BalanceOperationProcessedEvt evt, bool emit)
    {
        if (!base.Apply(evt)) return false;
        
        var bo = evt.BalanceOperation;

        RealizedPnLAccruedEvt? rplEvt = null;
        if (bo.AffectsPnL)
        {
            rplEvt = new RealizedPnLAccruedEvt(
                evt.EventId, 
                AccountId, 
                bo.ValueInAccountCcy, 
                null, 
                bo.BalanceOperationId,
                bo.Dt
            );
            Apply(rplEvt);
        }

        BalanceHistoryProjectionEvt? balanceEvt = null;
        if (bo.AffectsBalance)
        {
            balanceEvt = new BalanceHistoryProjectionEvt(evt.EventId,
                AccountId,
                bo.AssetId,
                bo.Amount,
                _balances.GetValueOrDefault(bo.AssetId, 0) + bo.Amount,
                bo.BalanceOperationId,
                null,
                evt.Timestamp
            );
            Apply(balanceEvt);
        }
        
        if (bo.AffectsInvestment)
        {
            Investment += bo.ValueInAccountCcy;
        }

        if (emit)
        {
            Emit(evt);
            RegisterProjectionUpdate(rplEvt);
            RegisterProjectionUpdate(balanceEvt);
        }

        return true;
    }
    
    public virtual void Apply(ExecutionReportEvt evt, bool emit) => ApplyInternal(evt, emit);

    protected bool ApplyInternal(ExecutionReportEvt evt, bool emit)
    {
        if (!base.Apply(evt)) return false;
        ApplyExecutionReport(evt.ExecutionReport);
        if (emit) Emit(evt);
        return true;
    }

    protected void ApplyExecutionReport(ExecutionReport er)
    {
        if (er.OrdStatus.IsTerminal())
        {
            _orders.Remove(er.OrderId);
            if (!string.IsNullOrEmpty(er.ClOrdId) && er.AccountId == AccountId) ClOrdIds.Remove(er.ClOrdId);
        }
        else
        {
            _orders[er.OrderId] = er;
            if (!string.IsNullOrEmpty(er.ClOrdId) && er.AccountId == AccountId) ClOrdIds.Add(er.ClOrdId);
        }
    }
    
    public void Apply(OrderCancelRejectEvt evt, bool emit)
    {
        if (!base.Apply(evt)) return;
        if (emit) Emit(evt);
    }

    public virtual void Apply(TradeEvt tradeEvt, bool emit) => ApplyInternal(tradeEvt, emit);
    
    protected bool ApplyInternal(TradeEvt tradeEvt, bool emit)
    {
        if (!base.Apply(tradeEvt)) return false;

        var trade = tradeEvt.Trade;
        Position? actualPosition = null, historyRecord = null;
        
        RealizedPnLAccruedEvt? pnlEvt = null;
        PositionChangedEvt? positionEvt = null;
        
        if (tradeEvt.SecurityType != SecurityType.FX) // Positions are not tracked for spot FX conversions
        {
            var strategyPositionId = PositionAccounting == PositionAccounting.Netted
                ? string.Empty
                : trade.StrategyPositionId;
            var effect = PositionChangeType.Open;


            var position = Positions.GetPosition(trade.ContractId, strategyPositionId);

            if (position != null)
            {
                if (trade.Side != position.Side)
                {
                    effect = PositionChangeType.Close;
                }

                (actualPosition, historyRecord) = position
                    .ChangePosition(trade.TradeId, trade.SignedVolume, trade.Price, trade.CalculatedCcyLastQty,
                        trade.FxRate,
                        tradeEvt.AccountCcyPrecision, trade.Dt, trade.Commission, tradeEvt.SettlCcyPrecision,
                        trade.ParentPositionId);
            }
            else
            {
                actualPosition = Position.OpenPosition(
                    trade.TradeId,
                    trade.ContractId,
                    AccountId,
                    trade.Volume,
                    trade.Side,
                    trade.CalculatedCcyLastQty,
                    trade.FxRate,
                    tradeEvt.AccountCcyPrecision,
                    trade.Dt,
                    trade.IsSynthetic,
                    strategyPositionId: strategyPositionId,
                    commission: trade.Commission,
                    signalGroupId: trade.SignalGroupId,
                    parentPositionId: trade.ParentPositionId);
            }

            positionEvt = new PositionChangedEvt(
                tradeEvt.EventId,
                AccountId,
                actualPosition,
                historyRecord,
                effect,
                tradeEvt.Timestamp
            );
            Apply(positionEvt);
            
            if (!trade.IsSynthetic && (effect == PositionChangeType.Close || trade.Commission != 0))
            {
                var rpnl = (historyRecord?.RealizedPnL ?? 0) - trade.Commission;
                pnlEvt = new RealizedPnLAccruedEvt(tradeEvt.EventId, AccountId, rpnl, trade.TradeId, null,
                    tradeEvt.Timestamp);
                Apply(pnlEvt);
            }
        }

        var settlementCurrencyId = trade.PaymentCurrencyId;
        var balance = _balances.GetValueOrDefault(settlementCurrencyId, 0m);
        var change = -trade.Commission;

        var assetId = tradeEvt.AssetId;
        var assetBalance = _balances.GetValueOrDefault(assetId, 0m);
        var assetChange = 0m;
        
        if (tradeEvt.SecurityType == SecurityType.Stock || tradeEvt.SecurityType == SecurityType.FX) // TODO: change to settlement type
        {
            change -= trade.CalculatedCcyLastQty * trade.Side.GetSign();

            if (tradeEvt.SecurityType == SecurityType.FX) assetChange += trade.SignedVolume; // For spot FX trades, update the base currency balance immediately
        }
        else if (tradeEvt.SecurityType == SecurityType.Futures || tradeEvt.SecurityType == SecurityType.CFD)
        {
            change += historyRecord?.RealizedPnL ?? 0;
        }
        else
        {
            throw new NotSupportedException($"Security type {tradeEvt.SecurityType} is not supported");
        }

        BalanceHistoryProjectionEvt? settlBalanceEvt = null, assetBalanceEvt = null;
        if (change != 0m)
        {
            settlBalanceEvt = new BalanceHistoryProjectionEvt(tradeEvt.EventId, AccountId, settlementCurrencyId,
                change, balance + change, null, trade.TradeId, tradeEvt.Timestamp);
            Apply(settlBalanceEvt);
        }

        if (assetChange != 0m)
        {
            assetBalanceEvt = new BalanceHistoryProjectionEvt(tradeEvt.EventId, AccountId, assetId,
                assetChange, assetBalance + assetChange, null, trade.TradeId, tradeEvt.Timestamp);
            Apply(assetBalanceEvt);
        }


        if (emit)
        {
            Emit(tradeEvt);
            RegisterProjectionUpdate(positionEvt);
            RegisterProjectionUpdate(pnlEvt);
            RegisterProjectionUpdate(settlBalanceEvt);
            RegisterProjectionUpdate(assetBalanceEvt);
        }
        
        return true;
    }

    protected void RemoveNewOrder(long orderId) => _orders.Remove(orderId);
    
    public void Apply(AccountEndOfDayEvt evt, bool emit)
    {
        if (!base.Apply(evt)) return;

        var positions = _positions.Values.ToList();
        var posEvents = new List<PositionChangedEvt>(positions.Count);
        foreach (var position in positions)
        {
            Position actualPosition, positionHistory;
            if (evt.PositionValues.TryGetValue(position.OpenTradeId, out var value))
            {
                (actualPosition, positionHistory) = position.MarkToMarket(evt.ReferenceDt, value.Price, value.SignedValue, value.SignedValueInAccountCcy);
            }
            else
            {
                Logger.LogWarning("Position {openTradeid} not found in EOD", position.OpenTradeId);
                (actualPosition, positionHistory) = position.MarkToMarket(evt.ReferenceDt, position.SettlPrice, position.TotalSettlPayments, position.TotalSettlPaymentsInAccountCcy);
            }
            var posEvt = new PositionChangedEvt(evt.EventId, AccountId, actualPosition, positionHistory, PositionChangeType.MTM, evt.Timestamp, value);
            Apply(posEvt);
            posEvents.Add(posEvt);
        }

        var dt = evt.ReferenceDt;
        RealizedPnLSinceLastMtm = 0;

        if (emit)
        {
            Emit(evt);
            foreach (var e in posEvents) RegisterProjectionUpdate(e);
            foreach (var e in evt.BalanceValues) 
                RegisterProjectionUpdate(
                    new BalanceMarkedToMarketProjectionEvt(evt.EventId, AccountId, e.Value, dt, evt.Timestamp)
                );
        }
    }
    
    public void Apply(SharePriceUpdatedEvt evt, bool emit)
    {
        if (!base.Apply(evt)) return;
        
        SharePrice = evt.SharePrice;
        HWM = Math.Max(HWM, SharePrice);

        if (emit)
        {
            Emit(evt);
            var projEvt = new SharePriceHistoryProjectionEvt(evt.EventId, AccountId,
                new(AccountId, evt.ReferenceDt, SharePrice, ShareCount, evt.DailyReturn, HWM, Investment,
                    SharePriceHistoryChangeType.EndOfDay)
            );
            RegisterProjectionUpdate(projEvt);
        }
    }
    
    public void Apply(ShareCountUpdatedEvt evt, bool emit)
    {
        if (!base.Apply(evt)) return;
        
        ShareCount += evt.Change;

        if (emit)
        {
            Emit(evt);
            var projEvt = new SharePriceHistoryProjectionEvt(evt.EventId, AccountId,
                new(AccountId, evt.Timestamp, SharePrice, ShareCount, 0, HWM, Investment,
                    SharePriceHistoryChangeType.BalanceOperation));
            RegisterProjectionUpdate(projEvt);
        }
    }
        
    #endregion
    
    
    #region Projections
    
    private void Apply(BalanceHistoryProjectionEvt evt)
    {
        if (evt.Balance == 0) _balances.Remove(evt.CurrencyId);
        else _balances[evt.CurrencyId] = evt.Balance;
    }

    private void Apply(RealizedPnLAccruedEvt evt)
    {
        RealizedPnLSinceLastMtm += evt.PnL;
    }
    
    private void Apply(PositionChangedEvt evt)
    {
        if (evt.PositionHistoryRecord != null) _positions.Remove(evt.PositionHistoryRecord.OpenTradeId);
        if (evt.ActualPosition != null) _positions[evt.ActualPosition.OpenTradeId] = evt.ActualPosition;
    }
    
    #endregion

    public static AccountBaseState CreateNewState(AccountRecordV6 account, IEventBus eventBus, ILoggerFactory loggerFactory) => new(
        account.AccountServiceName, account.AccountId, account.PositionAccounting, 
        new Dictionary<int, decimal>(), 
        new List<OrderStatus>(),
        new List<Position>(), 
        1, 0, 1, 0, 0, 0, eventBus, loggerFactory);
    
    // public static AccountBaseState FromAccountStateReadonly(IAccountStateReadonly state) => 
    //     new(state.AccountServiceName, state.AccountId, state.PositionAccounting, state.Balances, state.Orders, state.Positions, state.SharePrice, state.ShareCount, state.HWM, state.Investment, state.RealizedPnLSinceLastMtm, state.Version);
    

    public virtual AccountStateReadonly ToAccountStateReadonly() => new(AccountServiceName, AccountId, PositionAccounting, Balances.Copy(),
        Positions.ToList(), Orders.ToList(), Investment, SharePrice, ShareCount, HWM, RealizedPnLSinceLastMtm, Version);

    public static AccountBaseState FromAccountStateReadonly(IAccountStateReadonly state, IEventBus eventBus, ILoggerFactory loggerFactory) =>
        new(state.AccountServiceName, state.AccountId, state.PositionAccounting, state.Balances, state.Orders,
            state.Positions, state.SharePrice, state.ShareCount, state.HWM, state.Investment,
            state.RealizedPnLSinceLastMtm, state.Version, eventBus, loggerFactory);
}