using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.Queries.MarketData;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Accounts.AccountStates;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.Orders;
using QuantInfra.Sdk.Trading.Positions;

namespace QuantInfra.Domain.Accounts.Base;

/// <summary>
/// Base class for all accounts. Provides the following functionality:
/// * Track orders
/// * Track trades
/// * Track positions
/// * Track balances and investment
/// * Run End Of Day, calculate and track returns. TODO: move to a dedicated entity
/// </summary>
public class AccountBase : Processor, IAccount
{
    protected readonly ILogger Logger;
    protected readonly ILogger NotificationsLogger;
    // protected readonly IProfiler Profiler = global::Common.Profiling.Profiler.ActiveProfiler;
    protected readonly Currency AccountCurrency;
        
    protected readonly AccountRecordV6 AccountRecord;
    protected readonly string ServiceName;
    protected readonly AccountBaseState AccountState;
    protected readonly IBalanceOperationIdProvider BalanceOperationsIdProvider;
    protected readonly IOrderIdProvider OrderIdProvider;
    protected readonly IExecIdProvider ExecIdProvider;
    protected readonly ITradeIdProvider TradeIdProvider;
    protected readonly LogLevel LogLevel;
    
    protected bool LoggingEnabled = true;


    public AccountBase(
        AccountRecordV6 accountRecord,
        AccountBaseState accountStateReadonly,
        IEventIdProvider eventIdProvider,
        IBalanceOperationIdProvider balanceOperationsIdProvider,
        IOrderIdProvider orderIdProvider,
        IExecIdProvider execIdProvider,
        ITradeIdProvider tradeIdProvider,
        IEventBus eventBus,
        IQueryBus queryBus,
        ILoggerFactory loggerFactory,
        LogLevel logLevel
    ) : base(eventIdProvider, eventBus, queryBus)
    {
        TradeIdProvider = tradeIdProvider;
        LogLevel = logLevel;
        AccountRecord = accountRecord;
        ServiceName = AccountRecord.AccountServiceName;
        AccountType = accountRecord.AccountType;
        AccountId = accountRecord.AccountId;
        AccountState = accountStateReadonly;
        BalanceOperationsIdProvider = balanceOperationsIdProvider;
        OrderIdProvider = orderIdProvider;
        ExecIdProvider = execIdProvider;
        Logger = loggerFactory.CreateLogger(GetLoggerCategory());
        NotificationsLogger = loggerFactory.CreateLogger(GetLoggerCategory());
        AccountCurrency = queryBus.Query<GetCurrency, Currency?>(new(AccountRecord.CurrencyId))!;
    }
        
    public AccountType AccountType { get; }
    public int AccountId { get; }
    public IAccountStateReadonly AccountStateReadonly => AccountState;
    
    public void DisableLogging() => LoggingEnabled = false;
    public void EnableLogging() => LoggingEnabled = true;

    public decimal? GetBaseTradeSize(int contractId)
    {
        var price = Query<GetLastKnownContractPrice, decimal?>(new(contractId));
        if (price == null) return null;
        
        var contract = Query<GetContract, Contract>(new(contractId));
        decimal? fxRate = 1m;
        if (contract.Template.SettlementCurrency.CurrencyId != AccountCurrency.CurrencyId)
        {
            fxRate = Query<GetConversionRate, decimal?>(new(AccountCurrency.CurrencyId, contract.Template.SettlementCurrency.CurrencyId));
            if (fxRate == null) return null;
        }
        var tradeSize = AccountState.Investment * fxRate / contract.PLCalculator.GetValueInSettlementCcy(price.Value, 1);
        if (AccountType == AccountType.VirtualAccount)
            return Math.Round(tradeSize.Value, 8); // TODO: move VirtualAccountSizeFraction to config
        
        return contract.NormalizeVolume(tradeSize.Value);
    }

    public virtual decimal GetInvestment() => AccountStateReadonly.Investment;
        
    public void CreateAccount(Instant dt)
    {
        if (AccountRecord.EnableSharePriceTracking)
        {
            var spEvt = new SharePriceUpdatedEvt(
                EventIdProvider.GetNextEventId(),
                AccountId,
                0,
                1,
                0,
                AccountState.GetNextVersion(),
                dt,
                dt
            );
            AccountState.Apply(spEvt, true);
        }
    }

    public virtual void ProcessBalanceOperation(NewBalanceOperation request, Instant processingDt, Guid? requestId = null) 
        => ProcessBalanceOperationInternal(request, processingDt, requestId);

    protected BalanceOperation? ProcessBalanceOperationInternal(
        NewBalanceOperation request,
        Instant processingDt,
        Guid? requestId
    )
    {
        if (LoggingEnabled && Logger.IsEnabled(LogLevel.Information)) 
            Logger.LogInformation($"ProcessBalanceOperation, request={request}, requestId={requestId}");
        
        if (request.AccountId != AccountId) throw new InvalidOperationException("AccountId doesn't match the account");

        if (request.DelayUntilNextEndOfDay) throw new NotImplementedException();

        decimal fxRate = 1m;
        
        // TODO: request asset to confirm its existence and fail the command if it doesn't exist

        if (request.AssetId != AccountCurrency.CurrencyId)
        {
            var retrievedFxRate = request.FxRate
                ?? Query<GetConversionRate, decimal?>(new(request.AssetId, AccountCurrency.CurrencyId));

            if (retrievedFxRate == null)
            {
                if (LoggingEnabled && Logger.IsEnabled(LogLevel.Warning))
                    Logger.LogWarning("Cannot retrieve conversion rate from {assetId} to {currencyId}, balance operation was not created", request.AssetId, AccountCurrency.CurrencyId);
                throw new MissingFxRateException($"Cannot retrieve conversion rate from {request.AssetId} to {AccountCurrency.CurrencyId}");
            }
            fxRate = retrievedFxRate.Value;
        }
        
        var valueInAccountCcy = Math.Round(request.Amount * fxRate, AccountCurrency.Decimals);
            
        var bo = new BalanceOperation(request, ServiceName, BalanceOperationsIdProvider.GetNextBalanceOperationId(), 
            processingDt, fxRate, valueInAccountCcy
        );            
            
        var evt = new BalanceOperationProcessedEvt(
            EventIdProvider.GetNextEventId(),
            AccountId, 
            AccountState.GetNextVersion(),
            bo,
            processingDt,
            requestId
        );
        AccountState.Apply(evt, true);

        if (AccountRecord.EnableSharePriceTracking && bo.AffectsShareCount)
        {
            // TODO: calculate current share price
            var sharePrice = AccountState.SharePrice;
            if (sharePrice != 0)
            {
                var delta = Math.Round(valueInAccountCcy / sharePrice, 4);
                var scEvt = new ShareCountUpdatedEvt(EventIdProvider.GetNextEventId(), AccountId, delta, bo.BalanceOperationId,
                    AccountState.GetNextVersion(), processingDt);
                AccountState.Apply(scEvt, true);
            }
            
        }
            
        if (LoggingEnabled && Logger.IsEnabled(LogLevel.Debug)) 
            Logger.LogDebug("Exit ProcessBalanceOperation");

        return bo;
    }
        
    public virtual void PlaceOrder(NewOrderSingle order, Instant processingDt)
    {
        if (order.AccountId != AccountId)
            throw new InvalidOperationException("Order's AccountId doesn't match the account");
            
        var (er, _) = ValidateOrder(order, processingDt);
        var evt = new ExecutionReportEvt(EventIdProvider.GetNextEventId(), AccountId, AccountState.GetNextVersion(),
            AccountType, er, processingDt);
        AccountState.Apply(evt, true);
    }

    protected (ExecutionReport, Contract?) ValidateOrder(NewOrderSingle nos, Instant processingDt)
    {
        if (LoggingEnabled && Logger.IsEnabled(LogLevel.Information)) Logger.LogInformation("PlaceOrder, order={nos}", nos);

        if (!string.IsNullOrEmpty(nos.ClOrdId))
        {
            var sameClOrdIdExists = AccountState.ClOrdIds.Contains(nos.ClOrdId); //AccountState.Orders.Any(o => o.ClOrdId == nos.ClOrdId && o.AccountId == AccountId); // On broker accounts, there might be orders with different account IDs and the same ClOrdId
            if (sameClOrdIdExists)
            {
                if (LoggingEnabled && Logger.IsEnabled(LogLevel.Information))
                    Logger.LogInformation("Duplicate order with ClOrdId={clOrdId}", nos.ClOrdId);
                
                var rejectEr = ExecutionReport.OutrightReject(nos, ServiceName, ExecIdProvider.GetNextExecId(), RejectReason.DuplicateOrder, null, processingDt);
                // var evt = new ExecutionReportEvt(EventIdProvider.GetNextEventId(), AccountId, _accountState.Version,
                //     AccountType, rejectEr, processingDt);
                // _accountState.Apply(evt);
                // Emit(evt);
                return (rejectEr, null);
            }
        }
        
        var contract = Query<GetContract, Contract?>(new(nos.ContractId));

        if (contract is null)
        {
            var err = $"Order rejected: contract not found {nos.ContractId}";
            if (LoggingEnabled) Logger.LogWarning(err);
            var rejectEr = ExecutionReport.OutrightReject(nos, ServiceName, ExecIdProvider.GetNextExecId(),
                RejectReason.UnknownSymbol, err, processingDt
            );
            return (rejectEr, null);
            // ProcessExecutionReport(rejectEr, processingDt);
            // return rejectEr;
        }

        if (contract.IsSynthetic() && AccountType != AccountType.VirtualAccount)
        {
            var rejectEr = ExecutionReport.OutrightReject(nos, ServiceName, ExecIdProvider.GetNextExecId(), RejectReason.UnknownSymbol, 
                "Synthetics can be placed only to virtual accounts", processingDt);
            var evt = new ExecutionReportEvt(EventIdProvider.GetNextEventId(), AccountId, AccountState.GetNextVersion(),
                AccountType, rejectEr, processingDt);
            // _accountState.Apply(evt);
            // Emit(evt);
            return (rejectEr, contract);
        }
        // If the order is defined the account is a virtual account or the contract is a synthetic
        // TODO: this needs to be overridable. E.g. when placing an account to a broker account that doesn't support certain order types or characteristics, it can be made virtual
        var isVirtual = AccountType == AccountType.VirtualAccount || contract.SyntheticContractType.HasValue;
        var er = Order.CreateOrder(nos, ServiceName, OrderIdProvider.GetNextOrderId(), ExecIdProvider.GetNextExecId(), processingDt, isVirtual);
        // ProcessExecutionReportInternal(er, processingDt, emitEvents);
            
        if (LoggingEnabled && Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug("Exit PlaceOrder");

        return (er, contract);
    }

    public void CancelOrder(OrderCancelRequest request, Instant processingDt)
    {
        if (LoggingEnabled) Logger.LogInformation("Cancel, request={request}", request);
            
        var order = AccountState.Orders.GetOrder(request.OrderId, request.OrigClOrdId);
        if (order == null)
        {
            var ocr = new OrderCancelReject(AccountId, request.OrderId, request.ClOrdId, CxlRejReason.UnknownOrder);
            var evt = new OrderCancelRejectEvt(AccountId, EventIdProvider.GetNextEventId(), ocr, processingDt, AccountState.GetNextVersion());
            AccountState.Apply(evt, true);
            return;
        }
        
        if (order.AccountId != AccountId)
        {
            var ocr = new OrderCancelReject(AccountId, request.OrderId, request.ClOrdId, CxlRejReason.Other, "OCR must be sent from the order's account");
            var evt = new OrderCancelRejectEvt(AccountId, EventIdProvider.GetNextEventId(), ocr, processingDt, AccountState.GetNextVersion());
            AccountState.Apply(evt, true);
            return;
        }
            
        if (LoggingEnabled && Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug("Found order {order}", order);
        
        CancelOrder(order, processingDt, request.ClOrdId);
            
        if (LoggingEnabled && Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug("Exit CancelOrder");
    }

    protected virtual void CancelOrder(OrderStatus order, Instant processingDt, string? requestId = null) =>
        CancelOrderInternal(order, processingDt, requestId);

    protected ExecutionReport? CancelOrderInternal(OrderStatus order, Instant processingDt, string? requestId = null)
    {
        var er = order.CancelOrder(ExecIdProvider, processingDt, requestId: requestId);
        if (er?.IsSuspended == true) er = er.ConfirmCancellation(ExecIdProvider, processingDt);
            
        ProcessExecutionReport(er, processingDt);
        return er;
    }

    public void ReplaceOrder(OrderReplaceRequest request, Instant processingDt)
    {
        if (LoggingEnabled && Logger.IsEnabled(LogLevel.Information))
            Logger.LogInformation($"ReplaceOrder, request={request}");
            
        var order = AccountState.Orders.GetOrder(request.OrderId, request.OrigClOrdId);
        if (order == null)
        {
            var ocr = new OrderCancelReject(AccountId, request.OrderId, request.RequestId, CxlRejReason.UnknownOrder);
            var evt = new OrderCancelRejectEvt(AccountId, EventIdProvider.GetNextEventId(), ocr, processingDt, AccountState.GetNextVersion());
            AccountState.Apply(evt, true);
            return;
        }
        
        if (order.AccountId != AccountId)
        {
            var ocr = new OrderCancelReject(AccountId, request.OrderId, request.RequestId, CxlRejReason.Other, "OCRR must be sent from the order's account");
            var evt = new OrderCancelRejectEvt(AccountId, EventIdProvider.GetNextEventId(), ocr, processingDt, AccountState.GetNextVersion());
            AccountState.Apply(evt, true);
            return;
        }

        var somethingModified =
            (request.Side.HasValue && request.Side != order.Side)
            || (request.Price.HasValue && request.Price != order.Price)
            || (request.StopPx.HasValue && request.StopPx != order.StopPx)
            || (request.OrderQty.HasValue && request.OrderQty != order.OrderQty);

        if (!somethingModified)
        {
            var ocr = new OrderCancelReject(AccountId, request.OrderId, request.RequestId, CxlRejReason.OrderUnchanged, "No need to modify the order");
            var evt = new OrderCancelRejectEvt(AccountId, EventIdProvider.GetNextEventId(), ocr, processingDt, AccountState.GetNextVersion());
            AccountState.Apply(evt, true);
            return;
        }
            
        if (LoggingEnabled && Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug($"Found order {order}");
        
        ReplaceOrder(order, request, processingDt);
            
        if (LoggingEnabled && Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug("Exit ReplaceOrder");
    }

    protected virtual void ReplaceOrder(OrderStatus order, OrderReplaceRequest request, Instant processingDt) =>
        ReplaceOrderInternal(order, request, processingDt);

    protected ExecutionReport? ReplaceOrderInternal(OrderStatus order, OrderReplaceRequest request, Instant processingDt)
    {
        var er = order.RequestReplace(ExecIdProvider, request, processingDt);
        if (er is { IsSuspended : true }) er = er.ConfirmReplace(ExecIdProvider.GetNextExecId(), request, processingDt);
        ProcessExecutionReport(er, processingDt);
        return er;
    }

    public virtual void ProcessExecutionReport(ExecutionReport? er, Instant processingDt) =>
        ProcessExecutionReportInternal(er, processingDt, true);
        
    protected void ProcessExecutionReportInternal(ExecutionReport? er, Instant processingDt, bool emitEvents)
    {
        if (er == null) return;
            
        if (LoggingEnabled && Logger.IsEnabled(LogLevel.Information))
            Logger.LogInformation($"ProcessExecutionReport, {er.GetLogString()}");
            
        var erEvt = new ExecutionReportEvt(
            EventIdProvider.GetNextEventId(),
            AccountId,
            AccountState.GetNextVersion(),
            AccountType,
            er,
            processingDt
        );
            
        AccountState.Apply(erEvt, emitEvents);
        
        if (er is { ExecType: ExecType.Canceled or ExecType.Rejected, PositionEffect: PositionEffect.Open })
        {
            foreach (var linked in er.LinkedOrders)
            {
                if (linked.Value == LinkType.OneCancelsOther) continue;
                if (LoggingEnabled && Logger.IsEnabled(LogLevel.Information))
                    Logger.LogInformation("Canceling linked order {clOrdId} because of cancellation of the main order", linked.Key);
                CancelOrder(new OrderCancelRequest() { OrigClOrdId = linked.Key }, processingDt);
            }
        }
        
        if (er is { ExecType: ExecType.Fill, LinkedOrders.Count: > 0 })
        {
            foreach (var linked in er.LinkedOrders)
            {
                var linkedOrder = AccountState.Orders.SingleOrDefault(o => o.ClOrdId == linked.Key);
                if (linkedOrder is null) continue;
                // TODO: cancel order qty
                if (linked.Value == LinkType.OneCancelsOther)
                {
                    if (LoggingEnabled && Logger.IsEnabled(LogLevel.Information))
                        Logger.LogInformation("Canceling OCO {clOrdId}", linked.Key); 
                    CancelOrder(new OrderCancelRequest { OrderId = linkedOrder.OrderId}, processingDt);
                }
        
                // TODO: activated order qty
                if (linked.Value == LinkType.OneTriggersOther)
                {
                    if (LoggingEnabled && Logger.IsEnabled(LogLevel.Information))
                        Logger.LogInformation("Activating OTO {clOrdId} ({orderId})", linked.Key, linkedOrder.OrderId);
                        
                    var activatedEr = linkedOrder.ActivateSuspendedOrder(ExecIdProvider, processingDt);
                    if (activatedEr != null)
                    {
                        ProcessExecutionReport(activatedEr, processingDt);
                    }
                }
            }
        }
            
        if (LoggingEnabled && Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug("Exit ProcessExecutionReport");
    }

    public virtual void ProcessTrade(Trade trade, Instant processingDt)
        => ProcessTradeInternal(trade, processingDt);

    protected void ProcessTradeInternal(Trade trade, Instant processingDt)
    {
        if (trade == null)
            throw new ArgumentException($"Trade cannot be null, accountId={AccountId}");
            
        if (LoggingEnabled && Logger.IsEnabled(LogLevel.Information))
            Logger.LogInformation($"ProcessTrade, tradeId={trade.TradeId}, contractId={trade.ContractId}, signedVolume={trade.SignedVolume}, price={trade.Price}");
        
        var contract = Query<GetContract, Contract?>(new(trade.ContractId));
            
        var tradeEvt = new TradeEvt(
            EventIdProvider.GetNextEventId(),
            AccountId,
            trade, 
            AccountState.GetNextVersion(),
            processingDt,
            contract.Asset?.AssetId ?? contract.Template.Asset!.AssetId,
            contract.GetSettlementCurrencyPrecision(),
            contract.Template.SecurityType,
            AccountCurrency.Decimals
        );
        AccountState.Apply(tradeEvt, true);
            
        // var spInfo = AccountState.SharePrice;
        // var investment = GetInvestment();
        // #if !FAST
        // Logger.LogDebug($"Got share price info {spInfo}, investment {investment}");
        // #endif
        
        if (LoggingEnabled && Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug("Exit ProcessTrade");
    }
    
    public virtual void OnHeartbeat(Instant dt)
    {
        if (LoggingEnabled && Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebug("OnHeartbeat {dt}", dt);
            
        var orders = AccountState.Orders.ToList();

        foreach (var order in orders)
        {
            if (order.AccountId != AccountId) continue;
            if (order is { TimeInForce: TimeInForce.GoodTillTime, ExpireDt: not null } && order.ExpireDt <= dt)
            {
                if (LoggingEnabled && Logger.IsEnabled(LogLevel.Information))
                    Logger.LogInformation($"Canceling order {order.OrderId}, expireDt={order.ExpireDt}");
                CancelOrder(order, dt);
            }
            else if (order is { IsSuspended: true, ActivationDt: not null } && order.ActivationDt.Value <= dt)
            {
                if (LoggingEnabled && Logger.IsEnabled(LogLevel.Information))
                    Logger.LogInformation($"Activating suspended order {order.OrderId}, activationDt={order.ActivationDt}");
                var er = order.ActivateSuspendedOrder(ExecIdProvider, dt);
                ProcessExecutionReport(er, dt);
            }
        }
    }

    public (decimal dailyReturn, decimal currentDrawdown) GetLiquidationInfo(IReadOnlyDictionary<int, double> lastPrices, Instant referenceDt)
    {
        throw new NotImplementedException();
        // #if !FAST
        // Logger.LogInformation($"GetLiquidationInfo");
        // #endif
        // var spInfo = AccountState.GetSharePriceInfo();
        //
        // if (referenceDt == spInfo.Dt) return (spInfo.DailyReturn, -Math.Min(0, spInfo.SharePrice / spInfo.HWM - 1));
        // if (referenceDt < spInfo.Dt)
        // {
        //     #if !FAST
        //     Logger.LogError($"referenceDt={referenceDt}, last mtm dt={spInfo.Dt}, exiting");
        //     #endif
        //     throw new Exception("Cannot get liquidation info as of before the last MTM");
        // }
        //
        // var investment = GetInvestment();
        // // var dailyReturn =  AccountsRepository.GetClosedPositionsReturnForEOD(AccountId, spInfo.Dt, referenceDt);
        // var dailyReturn = AccountState.GetReturnSinceLastMtm();
        // #if !FAST
        // Logger.LogDebug($"Got share price info {spInfo}, investment  {investment}");
        // #endif
        //
        // var positions = _accountState.GetPositions();
        // foreach (var gr in positions.GroupBy(p => p.ContractId))
        // {
        //     var contractId = gr.Key;
        //     if (lastPrices.ContainsKey(contractId))
        //     {
        //         var contract = StaticDataProvider.GetContract(gr.Key);
        //         var price = contract.NormalizePrice(lastPrices[contractId]);
        //
        //         foreach (var p in gr)
        //         {
        //             // Use case:
        //             // - Previous day bar is not closed until the first minute bar from the new day comes
        //             // - The strategy uses minute bars
        //             // - First, the strategy timeframe is closed and the position opens
        //             // - Next, the daily timeframe used for MTM is closed and the position is gets marked using the previous day close price
        //             if (p.OpenDt > referenceDt)
        //             {
        //                 continue;
        //             }
        //
        //             (var actualPosition, var historyRecord) = p.MarkToMarket(
        //                 referenceDt,
        //                 price,
        //                 contract.PLCalculator, 
        //                 GetReturnCalculationBase(investment, contract.ContractTemplate.SettlementCurrencyId, referenceDt),
        //                 returnRounding: SharePricePrecision
        //             );
        //             
        //             dailyReturn += historyRecord.GrossReturn;
        //         }
        //     }
        // }
        //
        // var currentSP = spInfo.SharePrice + dailyReturn;
        // var dd = -Math.Min(0, currentSP / spInfo.HWM - 1);
        //
        // return (dailyReturn, dd);
    }

    public virtual void MarkToMarketEod(
        IReadOnlyDictionary<int, decimal> eodPrices,
        Instant referenceDt,
        Instant processingDt
    )
    {
        if (LoggingEnabled && Logger.IsEnabled(LogLevel.Information))
            Logger.LogInformation($"MarkToMarketEOD, referenceDt={referenceDt}, processingDt={processingDt}");
        
        var (positionValues, balanceValues, success) = MarkToMarket(eodPrices, referenceDt);

        var evt = new AccountEndOfDayEvt(
            EventIdProvider.GetNextEventId(),
            AccountId, 
            AccountState.GetNextVersion(),
            positionValues,
            balanceValues,
            success,
            referenceDt,
            processingDt
        );
        
        AccountState.Apply(evt, true);

        if (AccountRecord.EnableSharePriceTracking)
        {
            var shareCount = AccountState.ShareCount;
            if (shareCount == 0) return;

            if (success)
            {
                decimal equity = balanceValues.Count == 0 ? 0 : balanceValues.Values.Sum(v => v.TotalValue);
                // This is outdated. Now, the position values are added to the balances aggregate.
                // if (AccountRecord.IncludeUnrealizedPnLToMtm)
                // {
                //     equity += positionValues.Count == 0
                //         ? 0
                //         : positionValues.Values.Sum(pv => pv.EquityValueInAccountCcy);
                // }
                // else
                // {
                //     throw new NotImplementedException();
                // }

                var sharePrice = Math.Round(equity / shareCount, 4);
                var spEvt = new SharePriceUpdatedEvt(
                    EventIdProvider.GetNextEventId(),
                    AccountId,
                    equity,
                    sharePrice,
                    sharePrice - AccountState.SharePrice,
                    AccountState.GetNextVersion(),
                    referenceDt,
                    processingDt
                );
                AccountState.Apply(spEvt, true);
            }
            else
            {
                var spEvt = new SharePriceUpdatedEvt(
                    EventIdProvider.GetNextEventId(),
                    AccountId,
                    0m,
                    AccountState.SharePrice,
                    0,
                    AccountState.GetNextVersion(),
                    referenceDt,
                    processingDt
                );
                AccountState.Apply(spEvt, true);
            }
        }
    }

    public (IReadOnlyDictionary<long, PositionValue> positionValues, IReadOnlyDictionary<int, BalanceValue> balanceValues, bool success) MarkToMarket(Instant dt)
    {
        var prices = Query<GetLastKnownContractPrices, IReadOnlyDictionary<int, decimal>>(new());
        return MarkToMarket(prices, dt);
    }
    
    public (IReadOnlyDictionary<long, PositionValue> positionValues, IReadOnlyDictionary<int, BalanceValue> balanceValues, bool success) MarkToMarket(IReadOnlyDictionary<int, decimal> prices, Instant dt)
    {
        var success = true;

        Dictionary<int, PositionSum> values = new();
        
        var positionValues = AccountState.Positions.ToDictionary(
            p => p.OpenTradeId,
            p =>
            {
                var contract = Query<GetContract, Contract>(new(p.ContractId));
                decimal? price = null;
                decimal value;
                if (prices.ContainsKey(p.ContractId))
                {
                    price = prices[p.ContractId];
                    value = contract.PLCalculator.GetValueInSettlementCcy(prices[p.ContractId], p.Volume,
                        contract.Template.SettlementCurrency.Decimals) * p.Side.GetSign();
                }
                else
                {
                    value = p.TotalSettlPayments;
                }

                var successfulConversion = true;
                var settlCcyId = contract.Template.SettlementCurrency.CurrencyId;
                decimal fxRate = 1m;
                var valueInAccountCcy = settlCcyId == AccountCurrency.CurrencyId 
                    ? value 
                    : ConvertToAccountCurrency(value, contract.Template.SettlementCurrency.CurrencyId, prices, out successfulConversion, out fxRate);
                success &= successfulConversion;
                
                decimal equityValueInAccountCcy;
                switch (contract.Template.SecurityType)
                {
                    case SecurityType.Stock:
                        // For stocks, the balance of the account decreases by the CalculatedCcyLastQty at the booking of the trade,
                        // so the equity includes the full current value of the position
                        equityValueInAccountCcy = valueInAccountCcy;

                        values.TryAdd(settlCcyId, new());
                        values[settlCcyId].Holdings += p.TotalOpenPayments * p.Side.GetSign();
                        values[settlCcyId].UnrealizedPnL += value - (p.TotalOpenPayments * p.Side.GetSign());
                        break;
                    case SecurityType.CFD or SecurityType.Futures:
                        // For futures and CFDs there is no upfront payment, and the balance doesn't decrease.
                        // The value included into equity is hence the floating PnL
                        equityValueInAccountCcy = value - p.TotalOpenPayments * p.Side.GetSign();
                        if (settlCcyId != AccountCurrency.CurrencyId)
                        {
                            equityValueInAccountCcy = ConvertToAccountCurrency(equityValueInAccountCcy, settlCcyId, prices, out successfulConversion, out _);
                            success &= successfulConversion;
                        }
                        
                        values.TryAdd(contract.Template.SettlementCurrency.CurrencyId, new());
                        values[settlCcyId].FuturesVariationMargin += equityValueInAccountCcy;
                        
                        break;
                    // There are no positions for spot FX contracts
                    // case SecurityType.FX:
                    //     // FX spot contracts update the balances upon booking a trade, so the positions must not be added to equity
                    //     equityValueInAccountCcy = 0;
                    //     break;
                    default:
                        equityValueInAccountCcy = 0;
                        break;
                }
                
                return new PositionValue(AccountId, p.OpenTradeId, dt, price, value, fxRate, valueInAccountCcy, equityValueInAccountCcy);
            }
        );
        
        var currencyIds = AccountState.Balances.Keys.Union(values.Keys).Distinct().ToList();
        
        var balanceValues = currencyIds.ToDictionary(
            ccyId => ccyId,
            ccyId =>
            {
                var cashBalance = AccountState.Balances.GetValueOrDefault(ccyId);
                var val = values.GetValueOrDefault(ccyId);
                var holdings = val?.Holdings ?? 0m;
                var unrealizedPnL = val?.UnrealizedPnL ?? 0m;
                var futuresVM = val?.FuturesVariationMargin ?? 0m;
                var totalAmount = cashBalance + holdings + unrealizedPnL + futuresVM;
                
                var successfulConversion = true;
                decimal value, fxRate;
                if (ccyId == AccountCurrency.CurrencyId)
                {
                    value = totalAmount;
                    fxRate = 1m;
                }
                else
                {
                    value = ConvertToAccountCurrency(totalAmount, ccyId, prices, out successfulConversion, out fxRate);
                }
                success &= successfulConversion;
                return new BalanceValue(AccountId, ccyId, dt, cashBalance, holdings, unrealizedPnL, futuresVM,
                    totalAmount, value, fxRate);
            });

        return (positionValues, balanceValues, success);
    }

    class PositionSum
    {
        public decimal Holdings { get; set; }
        public decimal UnrealizedPnL { get; set; } = 0m;
        public decimal FuturesVariationMargin { get; set; } = 0m;
    }
    
    protected decimal ConvertToAccountCurrency(decimal amount, int currencyId, IReadOnlyDictionary<int, decimal> prices, out bool successfulConversion, out decimal fxRate)
    {
        bool success = true;
        if (currencyId == AccountCurrency.CurrencyId)
        {
            successfulConversion = true;
            fxRate = 1m;
            return amount;
        }
        
        var conversionPath = Query<GetConversionPath, IReadOnlyCollection<FxConversionStep>>(new(currencyId, AccountCurrency.CurrencyId));
        if (conversionPath.Count == 0)
        {
            successfulConversion = false;
            fxRate = 0;
            return 0;
        }

        fxRate = 1m;
        foreach (var c in conversionPath)
        {
            if (!prices.TryGetValue(c.ContractId, out var price))
            {
                successfulConversion = false;
                fxRate = 0;
                return 0m;
            }
            amount *= c.IsDirect ? price : 1 / price;
            fxRate *= c.IsDirect ? price : 1 / price;
        }
        successfulConversion = success;
        return Math.Round(amount, AccountCurrency.Decimals);
    }
        
    protected string GetLoggerCategory(string? type = null) => string.IsNullOrEmpty(type)
        ? $"Account {AccountRecord.Name} ({AccountId})"
        : $"{type } account {AccountRecord.Name} ({AccountId})";
}