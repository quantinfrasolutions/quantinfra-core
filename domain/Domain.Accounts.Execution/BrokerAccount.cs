using System;
using System.Collections.Generic;
using System.Linq;
using Common.Trading;
using Common.Trading.Positions;
using Common.Utils.Collections;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Account.Execution.State;
using QuantInfra.Domain.Accounts.Base;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Domain.Queries.MarketData;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Accounts.ExternalAccounts;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading.ExternalAccounts;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Domain.Accounts.Execution;

public class BrokerAccount : AccountBase, IBrokerAccount
{
    private readonly BrokerAccountState _accountState;
    private readonly int _brokerId;
    public BrokerType BrokerType { get; }
    private IBroker _broker;

    public BrokerAccount(
        AccountRecordV6 accountRecord,
        BrokerAccountState accountStateReadonly,
        IEventIdProvider eventIdProvider,
        IBalanceOperationIdProvider balanceOperationsIdProvider,
        IOrderIdProvider orderIdProvider,
        IExecIdProvider execIdProvider,
        ITradeIdProvider tradeIdProvider,
        IEventBus eventBus,
        IQueryBus queryBus,
        ILoggerFactory loggerFactory,
        LogLevel logLevel
    ) : base(accountRecord, accountStateReadonly, eventIdProvider, balanceOperationsIdProvider, orderIdProvider,
        execIdProvider, tradeIdProvider, eventBus, queryBus, loggerFactory, logLevel)
    {
        _accountState = accountStateReadonly;
        _brokerId = accountRecord.BrokerId!.Value;
        var broker = Query<GetBroker, Broker?>(new(accountRecord.BrokerId!.Value));
        BrokerType = broker.BrokerType;
        _broker = Brokers.GetBroker(BrokerType);
    }

    #region Order management

    public override void PlaceOrder(NewOrderSingle order, Instant processingDt)
    {
        if (order.AccountId != AccountId)
            throw new InvalidOperationException("Order's AccountId doesn't match the account");
            
        var (er, contract) = ValidateOrder(order, processingDt);
        er = new(er) { BrokerAccountId = AccountId };

        if (!_broker.SupportedOrderTypes.Contains(er.OrdType))
        {
            er = new(er) { IsVirtual = true }; // This forwards the order to VE immediately
        }

        ProcessExecutionReport(er, processingDt);
        if (er is { IsVirtual: false, IsSuspended: false, OrdStatus: OrdStatus.PendingNew }) PlaceExternalOrder(er, contract!, processingDt);
    }

    public void PlaceExternalOrder(ExecutionReport er, Instant processingDt) =>
        PlaceExternalOrder(er, Query<GetContract, Contract?>(new(er.ContractId)), processingDt);
    
    private void PlaceExternalOrder(ExecutionReport er, Contract contract, Instant processingDt)
    {
        if (er.OrdStatus != OrdStatus.PendingNew) throw new InvalidOperationException("OrdStatus must be PendingNew");
        if (er.IsVirtual) return;
        
        var accountId = er.AccountId;
        IAccount? account = accountId == AccountId ? this : Query<GetAccount, IAccount?>(new(accountId));
        if (account is null) throw new ArgumentNullException(nameof(account));
        
        if (contract.Template.Broker.BrokerId != _brokerId)
        {
            var rejectEr = er.RejectOrder(
                ExecIdProvider.GetNextExecId(),
                RejectReason.UnknownAccount,
                $"Contract {contract.ContractId} broker is {contract.Template.Broker.BrokerId}, which doesn't match the account broker {_brokerId}",
                processingDt
            );
            account.ProcessExecutionReport(rejectEr, processingDt);
            
            return;
        }
        
        var externalContractId = contract.ExternalContractId;
        if (string.IsNullOrEmpty(externalContractId))
        {
            var err = $"Contract mapping not defined for contract {er.ContractId} and broker {AccountRecord.BrokerId}";
            if (LoggingEnabled) Logger.LogError(err);

            var rejectEr = er.RejectOrder(
                ExecIdProvider.GetNextExecId(),
                RejectReason.UnknownSymbol,
                err,
                processingDt
            );
            account.ProcessExecutionReport(rejectEr, processingDt);
            return;
        }
        
        if (!_broker.SupportedOrderTypes.Contains(er.OrdType))
        {
            er = new ExecutionReport(er) { IsVirtual = true, ExecId = ExecIdProvider.GetNextExecId() }; // This sends the order to VE
            account.ProcessExecutionReport(er, processingDt);
            return;
        }

        var nos = new NewOrderSingleExternal(er, AccountId, externalContractId, clOrdId: er.OrderId.ToString());
        var evt = new NewOrderSingleExternalCreatedEvt(EventIdProvider.GetNextEventId(), AccountId, nos, er, BrokerType, _accountState.GetNextVersion(), processingDt);
        _accountState.Apply(evt, true);
    }

    protected override void CancelOrder(OrderStatus order, Instant processingDt, string? requestId = null)
    {
        var er = base.CancelOrderInternal(order, processingDt, requestId);
        if (er is { IsVirtual: false }) CancelExternalOrder(er, processingDt, true);
    }

    // This method gets called from SSA, so there is no need to notify SSA about the changed status
    public void CancelExternalOrder(ExecutionReport er, Instant processingDt) => 
        CancelExternalOrder(er, processingDt, false);
    
    private void CancelExternalOrder(ExecutionReport er, Instant processingDt, bool notifySsa)
    {
        if (er is { OrdStatus: OrdStatus.PendingCancel, IsVirtual: false })
        {
            var contract = Query<GetContract, Contract?>(new(er.ContractId));
            if (er.ExternalId is not null)
            {
                var ocr = new OrderCancelRequestExternal(er.OrderId, AccountId, contract.ExternalContractId,
                    er.ExternalId);
                var evt = new OrderCancelRequestExternalCreatedEvt(EventIdProvider.GetNextEventId(), AccountId, ocr,
                    er, _accountState.GetNextVersion(), processingDt);
                _accountState.Apply(evt, true);
                if (notifySsa && er.AccountId != AccountId)
                {
                    var ssa = Query<GetAccount, IAccount?>(new(er.AccountId));
                    ssa.ProcessExecutionReport(er, processingDt);
                }
            }
            else
            {
                // TODO: if broker supports cancelation by ClOrdId, send the external cancel request
                // Else, the CancelExternalOrder must be called again upon receiving the first External ER from the broker
            }
        }
    }

    protected override void ReplaceOrder(OrderStatus order, OrderReplaceRequest request, Instant processingDt)
    {
        var er = base.ReplaceOrderInternal(order, request, processingDt);
        if (er is { IsVirtual: false }) ReplaceExternalOrder(request, er, processingDt, true);
    }

    public void ReplaceExternalOrder(OrderReplaceRequest req, ExecutionReport er, Instant processingDt) =>
        ReplaceExternalOrder(req, er, processingDt, false);
    
    private void ReplaceExternalOrder(OrderReplaceRequest req, ExecutionReport er, Instant processingDt, bool notifySsa)
    {
        if (er is { OrdStatus: OrdStatus.PendingReplace, IsVirtual: false })
        {
            var contract = Query<GetContract, Contract?>(new(er.ContractId));
            if (string.IsNullOrEmpty(contract.ExternalContractId))
            {
                // TODO: reject replace
                return;
            }

            if (BrokerType == BrokerType.BinanceUsdmFutures || BrokerType == BrokerType.Ibkr)
            {
                if (er.OrdType != OrdType.Limit)
                {
                    var ocr = new OrderCancelReject(AccountId, req.OrderId, req.RequestId, CxlRejReason.Other, $"Only Limit orders can be replaced on {BrokerType}");
                    var evt = new OrderCancelRejectEvt(AccountId, EventIdProvider.GetNextEventId(), ocr, processingDt, AccountState.GetNextVersion());
                    _accountState.Apply(evt, true);
                    return;
                }
                
                req = new(req)
                {
                    Side = er.Side, // Binance and Ibkr required OrderQty to be provided
                    OrderQty = req.OrderQty ?? er.OrderQty, // Binance and Ibkr require Side to be provided
                    Price = req.Price ?? er.Price,
                };
            }
            
            if (er.ExternalId is not null)
            {
                var ocr = new OrderReplaceRequestExternal(req, contract.ExternalContractId, er.ExternalId, er.OrdType);
                var evt = new OrderReplaceRequestExternalCreatedEvt(EventIdProvider.GetNextEventId(), AccountId, ocr,
                    er, _accountState.GetNextVersion(), processingDt);
                _accountState.Apply(evt, true);
                if (notifySsa && er.AccountId != AccountId)
                {
                    var ssa = Query<GetAccount, IAccount?>(new(er.AccountId));
                    ssa.ProcessExecutionReport(er, processingDt);
                }
            }
            else
            {
                // TODO: if broker supports cancelation by ClOrdId, send the external cancel request
                // // Else, the CancelExternalOrder must be called again upon receiving the first External ER from the broker
            }
        }
    }
    
    #endregion

    #region Trades management

    public void AllocateTrade(Trade trade, int accountId, Instant processingDt)
    {
        if (LoggingEnabled && LogLevel <= LogLevel.Information)
            Logger.LogInformation($"AllocateTrade, tradeId={trade.TradeId}, accountId={accountId}");
        var ssaTrade = trade.CreateAllocation(TradeIdProvider.GetNextTradeId(), accountId);
        var ssa = Query<GetAccount, IAccount?>(new(accountId));
        ssa.ProcessTrade(ssaTrade, processingDt);
    }

    #endregion
    
    #region Reconciliation events

    public void OnExecutionServiceMissedVersionEvt(Instant processingDt)
    {
        var evt = new BrokerAccountNeedsOrdersReconciliationEvt(EventIdProvider.GetNextEventId(), AccountId, 
            _accountState.GetNextVersion(), processingDt);
        _accountState.Apply(evt, true);
    }

    public void OnExternalAccountConnectionRestoredEvt(Instant processingDt)
    {
        var evt = new BrokerAccountNeedsTradesReconciliationEvt(EventIdProvider.GetNextEventId(), AccountId,
            _accountState.UsedContractIds, _accountState.LastReceivedBalanceOperationTs, _accountState.GetNextVersion(), 
            processingDt);
        
        _accountState.Apply(evt, true);
    }
    
    #endregion
    
    #region Single updates
    
    public void OnExternalExecutionReport(ExternalExecutionReport externalEr, Instant processingDt)
    {
        var order = externalEr.OrderId.HasValue 
            ? AccountStateReadonly.Orders.SingleOrDefault(o => o.OrderId == externalEr.OrderId.Value)
            : AccountStateReadonly.Orders.SingleOrDefault(o => o.ExternalId == externalEr.ExternalId);
        
        if (order is null && LoggingEnabled && LogLevel <= LogLevel.Warning)
            Logger.LogWarning($"Order not found by orderId={externalEr.OrderId} and externalId={externalEr.ExternalId}, externalER={externalEr}");
        
        OnExternalExecutionReport(order, externalEr, processingDt, true);
    }

    /// <summary>
    /// Returns an execution report with ExecType = Restated if the external update doesn't match the current order
    /// </summary>
    private ExecutionReport? OnExternalExecutionReport(OrderStatus? order, ExternalExecutionReport externalEr, Instant processingDt, bool emitExternalAccountsEvents)
    {
        if (LoggingEnabled && LogLevel <= LogLevel.Information)
            Logger.LogInformation($"OnExternalExecutionReport, externalER={externalEr}, processingDt={processingDt}, emitExternalAccountsEvents={emitExternalAccountsEvents}");

        if (order is null && externalEr.OrdStatus.IsTerminal())
        {
            if (LoggingEnabled && LogLevel <= LogLevel.Warning)
                Logger.LogWarning("External execution report is in terminal status and active order was not found, ignoring");

            return null;
        }
        
        var contract = order is null
            ? Query<GetContractByExternalId, Contract?>(new (_brokerId, externalEr.ExternalContractId))
            : Query<GetContract, Contract?>(new(order.ContractId));
        
        if (contract == null && order is null)
        {
            // This scenario is covered above
            // if (externalEr.OrdStatus.IsTerminal())
            // {
            //     if (LoggingEnabled && LogLevel <= LogLevel.Error) 
            //         Logger.LogWarning($"Received an unknown order {externalEr.ExternalId} for an unknown contract {externalEr.ExternalContractId}, skipping");
            //     return null;
            // }

            if (LoggingEnabled && LogLevel <= LogLevel.Warning)
                Logger.LogWarning("Unknown order for an unknown contract, placing order into the dead letter queue");

            var externalContractId = externalEr.ExternalContractId;
            if (!string.IsNullOrEmpty(externalContractId) && !_accountState.UnmappedExternalContractIds.Contains(externalContractId))
            {
                var evt = new NewUnmappedContractRegisteredEvt(EventIdProvider.GetNextEventId(), AccountId, externalContractId, _accountState.GetNextVersion(), processingDt);
                _accountState.Apply(evt, true);
            }
            return null;
        }

        ExecutionReport er;
        
        if (order is null)
        {
            if (LoggingEnabled && LogLevel <= LogLevel.Warning)
                Logger.LogWarning($"Cannot match the incoming order with an internal order");
            
            if (externalEr.OrdStatus.IsTerminal())
            {
                if (LoggingEnabled && LogLevel <= LogLevel.Warning)
                    Logger.LogWarning($"Status of the received order {externalEr.ExternalId} is {externalEr.OrdStatus}, skipping");
                return null;
            }

            decimal? lastQtyOverride = null, lastCalculatedCcyOverride = null;

            if (BrokerType == BrokerType.Ibkr && externalEr.ExecType == ExecType.Fill)
            {
                lastQtyOverride = externalEr.CumQty;
                lastCalculatedCcyOverride = 0;
            }
            
            er = externalEr.ToExecutionReport(ServiceName, ExecIdProvider.GetNextExecId(), OrderIdProvider.GetNextOrderId(), 
                contract!.ContractId, 
                lastQtyOverride: lastQtyOverride, lastCalculatedCcyOverride: lastCalculatedCcyOverride, 
                execTypeOverride: ExecType.New
            );
            ProcessExecutionReport(er, processingDt);
            return er;
        }
        
        if (BrokerType == BrokerType.Ibkr)
        {
            // This happens when we received an order status for an already inactive order.
            // In this case, the order might be returned from AccountsRepository.GetOrderByExternalId
            // in the terminal status (which is required for it to be mapped to a trade, when it arrives).
            // Since both statuses (incoming and internal) are terminal, it is safe just to ignore the incoming ER.
            if (order.OrdStatus.IsTerminal() && externalEr.OrdStatus.IsTerminal())
            {
                if (LoggingEnabled && LogLevel <= LogLevel.Information)
                    Logger.LogInformation("Received a terminal status of an order in the terminal status, ignoring");
                 return null;
            }
            
            // TODO
            // At least on the demo accounts, IBKR notifies about the status of a filled order twice.
            // This leads to duplication of execution reports with ExecType == Fill.
            // To fix this, check if there was already a fill with the same characteristics.
            // var orders = Query<GetOrdersHistory, IReadOnlyCollection<ExecutionReport>>(new (new ()
            // {
            //     AccountId = order?.AccountId ?? AccountId,
            //     FromDt = externalEr.TransactTime.Minus(Duration.FromSeconds(5)),
            //     ToDt = externalEr.TransactTime,
            //     ExternalId = externalEr.ExternalId,
            //     OrdStatus = externalEr.OrdStatus,
            //     OrderId = order.OrderId
            // })).ToList();
            //
            // if (orders.Any())
            // {
            //     var matchingOrder = orders.SingleOrDefault(o =>
            //         o.CumQty == externalEr.CumQty
            //     );
            //
            //     if (matchingOrder != null)
            //     {
            //         _logger.LogWarning($"Found a possible duplicate of the exec, placing incoming ER into the dead letter queue: {matchingOrder}");
            //         // TODO: dead letter queue
            //         return null;
            //     }
            // }
        }


        var orderRestated = false;

        if (!order.OrdStatus.IsTerminal() && order.OrdStatus != OrdStatus.PendingReplace)
        {
            var validationErrors = new List<string>(10);
            
            if (order.ContractId != contract.ContractId)
            {
                validationErrors.Add($"Contract: expected={order.ContractId}, actual={contract.ContractId}");
                orderRestated = true;
            }

            if (externalEr.OrdType.HasValue && order.OrdType != externalEr.OrdType)
            {
                validationErrors.Add($"OrdType: expected={order.OrdType}, actual={externalEr.OrdType}");
                orderRestated = true;
            }

            if (externalEr.OrderQty.HasValue && order.OrderQty != externalEr.OrderQty)
            {
                validationErrors.Add($"OrderQty: expected={order.OrderQty}, actual={externalEr.OrderQty}");
                orderRestated = true;
            }

            if (externalEr.Side.HasValue && order.Side != externalEr.Side)
            {
                validationErrors.Add($"Side: expected={order.Side}, actual={externalEr.Side}");
                orderRestated = true;
            }

            if (externalEr.Price.HasValue && order.Price != externalEr.Price)
            {
                validationErrors.Add($"Price: expected={order.Price}, actual={externalEr.Price}");
                orderRestated = true;
            }

            if (externalEr.StopPx.HasValue && order.StopPx != externalEr.StopPx)
            {
                validationErrors.Add($"StopPx: expected={order.StopPx}, actual={externalEr.StopPx}");
                orderRestated = true;
            }
            
            if (orderRestated && LoggingEnabled && LogLevel <= LogLevel.Warning)
            {
                Logger.LogWarning($"Order {order.OrderId} restated by external execution report: {string.Join(',', validationErrors)}");
            }
        }
        
        // TODO: check other meaning fields and apply new ER only if they differ from the current

        er = orderRestated 
            ? order.ApplyExternalExecutionReport(externalEr, ExecIdProvider.GetNextExecId(), contract.ContractId, ExecType.Restated)
            : order.ApplyExternalExecutionReport(externalEr, ExecIdProvider.GetNextExecId(), contract.ContractId);
        
        if (er.AccountId == AccountId)
        {
            ProcessExecutionReport(er, processingDt);
        }
        else
        {
            var ssa = Query<GetAccount, IAccount?>(new(er.AccountId));
            ssa.ProcessExecutionReport(er, processingDt);
            // Need to update account state without recording the event:
            var extEvt = new ExternalExecutionReportEvt(EventIdProvider.GetNextEventId(), AccountId, _accountState.GetNextVersion(),
                BrokerType, externalEr.ExternalContractId, er, processingDt);
            _accountState.Apply(extEvt, true);
        }
        
        // If the order was PendingCancel, and we received the external ID from the broker
        if (externalEr.ExecTypeReason == ExecTypeReason.ExternalOrderIdAssignedToOrder && order.OrdStatus == OrdStatus.PendingCancel
            && !er.OrdStatus.IsTerminal())
        {
            // TODO: avoid this if the broker supports cancellations by ClOrdId — in this case the cancellation will be requested immediately
            er = er.CancelOrder(ExecIdProvider.GetNextExecId(), processingDt);
            CancelExternalOrder(er, processingDt, true);
        }
        
        // If the order was PendingReplace, and we received the external ID from the broker
        if (externalEr.ExecTypeReason == ExecTypeReason.ExternalOrderIdAssignedToOrder && order.OrdStatus == OrdStatus.PendingReplace
            && !er.OrdStatus.IsTerminal())
        {
            // TODO: avoid this if the broker supports cancellations by ClOrdId — in this case the replacement will be requested immediately
            if (er.OrderQty != order.OrderQty || er.Price != order.Price || er.StopPx != order.StopPx)
            {
                var request = new OrderReplaceRequest()
                {
                    AccountId = AccountId,
                    OrderId = er.OrderId,
                    OrderQty = order.OrderQty,
                    Price = order.Price,
                    StopPx = order.StopPx,
                    Side = order.Side,
                };
                var replace = er.RequestReplace(ExecIdProvider, request, processingDt);
                if (replace != null) ReplaceExternalOrder(request, replace, processingDt, true);
            }
        }
        
        if (LoggingEnabled && Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug("Exit OnExternalExecutionReport");
        
        if (LoggingEnabled && er.ExecType == ExecType.Canceled)
        {
            NotificationsLogger.LogWarning($"Order canceled: {contract.ContractId} ({contract.Ticker}) qty={er.OrderQty}, filled={er.CumQty}: {er.RejectText}");
        }
        if (LoggingEnabled && er.ExecType == ExecType.Rejected)
        {
            NotificationsLogger.LogWarning($"Order rejected: {contract.ContractId} ({contract.Ticker}) qty={er.OrderQty}, filled={er.CumQty}: {er.RejectText}");
        }

        return orderRestated ? er : null;
    }

    public void OnExternalOrderCancelReject(OrderCancelReject ocr, Instant processingDt)
    {
        if (LoggingEnabled && LogLevel <= LogLevel.Information)
            Logger.LogInformation("OnExternalOrderCancelReject, ocr={externalOcr}, processingDt={processingDt}", ocr, processingDt);
        
        var evt = new OrderCancelRejectEvt(AccountId, EventIdProvider.GetNextEventId(), ocr, processingDt, AccountState.GetNextVersion());
        AccountState.Apply(evt, true);
        
        var order = _accountState.Orders.SingleOrDefault(o => o.OrderId == ocr.OrderId);
        if (order != null)
        {
            if (order.OrdStatus == OrdStatus.PendingCancel && ocr.RejectReason == CxlRejReason.UnknownOrder)
            {
                var er = order.ConfirmCancellation(ExecIdProvider, processingDt);
                if (er != null)
                {
                    if (er.AccountId == AccountId)
                    {
                        ProcessExecutionReport(er, processingDt);
                    }
                    else
                    {
                        var ssa = Query<GetAccount, IAccount?>(new(er.AccountId));
                        ssa.ProcessExecutionReport(er, processingDt);
                        // Need to update account state without recording the event:
                        var extEvt = new ExecutionReportEvt(EventIdProvider.GetNextEventId(), AccountId, _accountState.GetNextVersion(),
                            AccountType.StrategySubAccount, er, processingDt);
                        _accountState.Apply(extEvt, true);
                    }
                }
            }
            else if (order.OrdStatus == OrdStatus.PendingReplace && ocr.RejectReason == CxlRejReason.OrderUnchanged)
            {
                var er = order.ConfirmReplace(ExecIdProvider.GetNextExecId(), processingDt);
                if (er != null)
                {
                    if (er.AccountId == AccountId)
                    {
                        ProcessExecutionReport(er, processingDt);
                    }
                    else
                    {
                        var ssa = Query<GetAccount, IAccount?>(new(er.AccountId));
                        ssa.ProcessExecutionReport(er, processingDt);
                        // Need to update account state without recording the event:
                        var extEvt = new ExecutionReportEvt(EventIdProvider.GetNextEventId(), AccountId, _accountState.GetNextVersion(),
                            AccountType.StrategySubAccount, er, processingDt);
                        _accountState.Apply(extEvt, true);
                    }
                }
            }
            else
            {
                if (LoggingEnabled && Logger.IsEnabled(LogLevel.Warning))
                    Logger.LogWarning("OCR external: reconciling account state");

                OnExecutionServiceMissedVersionEvt(processingDt); // Set account needs orders reconciliation with the broker
            }
        }
    }
    
    public void OnExternalTrade(ExternalTradeRecord externalTradeRecord, Instant processingDt) => 
        OnExternalTrade(externalTradeRecord, processingDt, false);
    
    private void OnExternalTrade(ExternalTradeRecord externalTradeRecord, Instant processingDt, bool isFromSnapshot)
    {
        if (LoggingEnabled && LogLevel <= LogLevel.Information)
            Logger.LogInformation($"OnExternalTrade {externalTradeRecord}");

        // If the trade comes not from the snapshot, and the account is in the trades reconciliation state, ignore the trade
        if (!isFromSnapshot && _accountState.NeedsTradesReconciliation)
        {
            if (LoggingEnabled && LogLevel <= LogLevel.Information)
                Logger.LogInformation("Ignoring trade {tradeId}, because trades snapshot has been requested", externalTradeRecord.ExternalTradeId);
            return;
        }
        
        // EnsureContractIdAddedToUsed(contract.ContractId, externalTradeRecord.Dt, processingDt);

        // Deduplication
        if (externalTradeRecord.Dt < _accountState.LastReceivedTradeTs) return;
        if (externalTradeRecord.Dt == _accountState.LastReceivedTradeTs
            && _accountState.LastReceivedTradeIds.Contains(externalTradeRecord.ExternalTradeId)) return;
        
        var contract = Query<GetContractByExternalId, Contract?>(
            new(_brokerId, externalTradeRecord.ExternalContractId)
        );
        
        if (contract == null)
        {
            if (LoggingEnabled && LogLevel <= LogLevel.Error)
                Logger.LogError("Trade with an unknown contract, placing in the dead letter queue");
            
            var externalContractId = externalTradeRecord.ExternalContractId;
            
            if (!string.IsNullOrEmpty(externalContractId) && !_accountState.UnmappedExternalContractIds.Contains(externalContractId))
            {
                var evt = new NewUnmappedContractRegisteredEvt(EventIdProvider.GetNextEventId(), AccountId, externalContractId, _accountState.GetNextVersion(), processingDt);
                _accountState.Apply(evt, true);
            }
            
            var extTradeEvt = new NewTradeInDeadLetterQueueEvt(EventIdProvider.GetNextEventId(), AccountId, externalTradeRecord, _accountState.GetNextVersion(), processingDt);
            _accountState.Apply(extTradeEvt, true);
            return;
        }

        var fill = externalTradeRecord.ExternalOrderId is not null 
            ? _accountState.PendingFills.Values.FirstOrDefault(f => 
                f.ExternalId == externalTradeRecord.ExternalOrderId 
                && f.Side == externalTradeRecord.Side && f.LastQty == externalTradeRecord.Volume)
            : _accountState.PendingFills.Values.FirstOrDefault(f =>
                f.ContractId == contract.ContractId
                && f.Side == externalTradeRecord.Side
                && f.LastQty == externalTradeRecord.Volume);

        decimal fxRate = 1;
        if (contract.Template.SettlementCurrency.CurrencyId != AccountCurrency.CurrencyId)
        {
            fxRate = Query<GetConversionRate, decimal?>(new(contract.Template.SettlementCurrency.CurrencyId, AccountCurrency.CurrencyId))
                ?? 1m;
            
            // TODO: place the trade into a dead letter queue if the rate cannot be retrieved
        }
        
        decimal? calculatedLastCcyOverride = null;

        if (BrokerType == BrokerType.Ibkr)
        {
            calculatedLastCcyOverride = contract.PLCalculator
                .GetValueInSettlementCcy(externalTradeRecord.Price, externalTradeRecord.Volume);
        }

        if (externalTradeRecord.CommissionCurrency != contract.Template.SettlementCurrency.Asset.Name)
        {
            throw new NotSupportedException($"Settlement currency {contract.Template.SettlementCurrency.Asset.Name} doesn't match commission currency {externalTradeRecord.CommissionCurrency}");
        }
        
        var trade = externalTradeRecord.ToTrade(
            ServiceName,
            TradeIdProvider.GetNextTradeId(),
            contract.ContractId,
            fill?.ClOrdId,
            fill?.OrderId, 
            fill?.ExecId,
            fill?.StrategyPositionId,
            fill?.ExecutionRequestId,
            contract.Template.SettlementCurrency.CurrencyId,
            fxRate,
            calculatedLastCcyOverride,
            fill?.PositionEffect
        );
        ProcessTrade(trade, processingDt);

        if (fill is not null && fill.AccountId != AccountId)
        {
            AllocateTrade(trade, fill.AccountId, processingDt);
        }
        else
        {
            var ssas = Query<GetSsaIdsForBrokerAccount, IReadOnlyCollection<int>>(new(AccountId));
            if (ssas.Count == 1)
            {
                AllocateTrade(trade, ssas.Single(), processingDt);
            }
        }
        
        if (LoggingEnabled && LogLevel <= LogLevel.Debug)
            Logger.LogDebug("Exit OnExternalTrade");
        // NotificationsLogger.LogInformation($"Trade {contract.ContractId} ({contract.Ticker}) {trade.SignedVolume} @ {trade.Price}");
    }

    public void OnExternalPositionReport(ExternalPositionReport positionReport, Instant processingDt)
    {
        if (LoggingEnabled && LogLevel <= LogLevel.Information)
            Logger.LogInformation($"OnExternalPositionReport, positionReport={positionReport}, processingDt={processingDt}");
        

        var contract = Query<GetContractByExternalId, Contract?>(new(AccountRecord.BrokerId!.Value, positionReport.ExternalContractId));
        if (contract == null)
        {
            // TODO
            throw new NotImplementedException("Position for an unknown contract");
        }

        // EnsureContractIdAddedToUsed(contract.ContractId, referenceDt, processingDt);
        
        var expectedPosition = AccountStateReadonly.Positions.SingleOrDefault(p => p.ContractId == contract.ContractId) 
            ?? new Position { AccountId = AccountId, ContractId = contract.ContractId, SignedVolume = 0, };
        ValidatePositions(positionReport, expectedPosition, contract, processingDt, processingDt);
        
        // NotificationsLogger.LogInformation($"Position {contract.ContractId} ({contract.Ticker}) {positionReport.SignedVolume} @ {positionReport.OpenPrice}");

        // if (emitExternalAccountsEvents && offsetTrades.Count > 0)
        // {
        //     foreach (var trade in externalOffsetTrades)
        //     {
        //         var evt = new TradeAllocatedEvt(trade.AccountId, trade, trade.Dt, processingDt);
        //         Emit(evt);
        //     }
        // }
        
        if (LoggingEnabled && LogLevel <= LogLevel.Debug)
            Logger.LogDebug("Exit OnExternalAccountPositionsSnapshot");
    }

    public void OnExternalBalanceOperation(ExternalBalanceOperation balanceOperation, Instant processingDt)
    {
        var asset = Query<GetAssetByExternalId, Asset?>(new(_brokerId, balanceOperation.ExternalAssetId));

        if (asset == null)
        {
            if (LoggingEnabled && LogLevel <= LogLevel.Error)
                Logger.LogError($"No asset found by external id {balanceOperation.ExternalAssetId} and broker {_brokerId}, placing into dead letter queue");
            // TODO: dead letter queue
            return;
        }
        
        // Deduplication
        if (balanceOperation.Dt < _accountState.LastReceivedBalanceOperationTs
            || (balanceOperation.Dt == _accountState.LastReceivedBalanceOperationTs
                && _accountState.LastReceivedBalanceOperationIds.Contains(balanceOperation.ExternalId))
        )
        {
            if (LoggingEnabled && Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning("External balance operation {externalId} is detected as duplicated and ignored", balanceOperation.ExternalId);
            }
            return;
        }
        
        if (!balanceOperation.IsSwap)
        {
            // do not track investment on broker accounts
            var bo = balanceOperation.ToNewBalanceOperation(asset.AssetId, true, false, false);
            try
            {
                ProcessBalanceOperation(bo, processingDt);
            }
            catch (MissingFxRateException e)
            {
                SetAccountNeedsReconciliation($"Could not process balance operation: {e.Message}", processingDt);
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(balanceOperation.SwapPositionExternalContractId))
            {
                throw new NotImplementedException();
            }
            else
            {
                var bo = balanceOperation.ToNewBalanceOperation(asset.AssetId, true, true, false);
                try
                {
                    ProcessBalanceOperation(bo, processingDt);
                }
                catch (MissingFxRateException e)
                {
                    SetAccountNeedsReconciliation($"Could not process balance operation: {e.Message}", processingDt);
                }
            }
        }
    }
    
    
    #endregion

    #region Snapshots
    
    public void OnExternalAccountOrdersSnapshot(ExternalAccountOrdersSnapshot snapshot, Instant processingDt) =>
        OnExternalAccountOrdersSnapshot(snapshot.Orders, snapshot.UpdateTs, processingDt);

    private void OnExternalAccountOrdersSnapshot(IReadOnlyCollection<ExternalExecutionReport> orders, Instant referenceDt, Instant processingDt)
    {
        if (LoggingEnabled && LogLevel <= LogLevel.Information)
            Logger.LogInformation($"OnExternalAccountOrdersSnapshot");
        
        var expectedOrders = AccountStateReadonly
            .Orders
            .ToDictionary(o => o.OrderId, o => o);
        
        foreach (var er in orders)
        {
            OrderStatus? order = null;
            
            if (er.OrderId.HasValue)
            {
                order = expectedOrders.GetValueOrDefault(er.OrderId.Value);
            }

            order ??= expectedOrders.Values.SingleOrDefault(o => o.ExternalId == er.ExternalId);

            if (order != null)
            {
                expectedOrders.Remove(order.OrderId);
            }
            else
            {
                if (LoggingEnabled && LogLevel <= LogLevel.Warning)
                    Logger.LogWarning($"Unexpected order on the broker's account: {er}");
            }
            OnExternalExecutionReport(order, er, processingDt, true);
        }
        
        foreach (var o in expectedOrders.Values)
        {
            if (LoggingEnabled && Logger.IsEnabled(LogLevel.Information))
                Logger.LogInformation("Order {orderId} doesn't exist on the broker, cancelling", o.OrderId);
            
            OnExternalExecutionReport(o, new ExternalExecutionReport(o.ClOrdId, o.OrderId, null, AccountId, null,
                OrdStatus.Canceled, null, null, null, null, null, o.CumQty, 0, o.Price, o.StopPx, o.TimeInForce, o.ExpireDt,
                ExecType.Canceled, null, RejectReason.UnknownOrder, "Order doesn't exist on the broker", processingDt, null),
                processingDt, true);
        }
        
        var reconciledEvt = new BrokerAccountOrdersReconciledEvt(EventIdProvider.GetNextEventId(), AccountId, _accountState.GetNextVersion(), processingDt);
        _accountState.Apply(reconciledEvt, true);
        
        if (LoggingEnabled && LogLevel <= LogLevel.Debug)
            Logger.LogDebug("Exit OnOrderSnapshotReceivedFromTradingVenue");
    }
    
    public void OnExternalAccountTradesReport(ExternalAccountTradesReport report, Instant processingDt) =>
        OnExternalAccountTradesReport(report.Trades, processingDt);

    private void OnExternalAccountTradesReport(IReadOnlyCollection<ExternalTradeRecord> trades, Instant processingDt)
    {
        if (LoggingEnabled && LogLevel <= LogLevel.Information)
            Logger.LogInformation($"OnExternalAccountTradesReport");
        
        foreach (var t in trades.OrderBy(t => t.Dt))
        {
            OnExternalTrade(t, processingDt, true);
        }
        
        if (LoggingEnabled && LogLevel <= LogLevel.Debug)
            Logger.LogDebug("Exit OnExternalAccountTradesReport");
    }
    
    public void OnExternalAccountPositionsSnapshot(AccountPositionsSnapshot snapshot, Instant processingDt) =>
        OnExternalAccountPositionsSnapshot(snapshot.Positions, snapshot.UpdateTs, processingDt);

    private void OnExternalAccountPositionsSnapshot(IReadOnlyCollection<ExternalPositionReport> positions, Instant referenceDt, Instant processingDt)
    {
        if (LoggingEnabled && LogLevel <= LogLevel.Information)
            Logger.LogInformation($"OnExternalAccountPositionsSnapshot");
        
        var expectedPositions = AccountStateReadonly.Positions.ToDictionary(p => p.ContractId);
        
        foreach (var p in positions)
        {
            var contract = Query<GetContractByExternalId, Contract?>(new(_brokerId, p.ExternalContractId));

            if (contract == null)
            {
                // TODO
                throw new NotImplementedException("Position for an unknown contract");
            }

            if (!expectedPositions.Remove(contract.ContractId, out var ep))
                ep = new Position { SignedVolume = 0, };
            
            ValidatePositions(p, ep, contract, referenceDt, processingDt);
        }

        foreach (var ep in expectedPositions.Values)
        {
            var contract = Query<GetContract, Contract>(new(ep.ContractId));
            ValidatePositions(new ExternalPositionReport { SignedVolume = 0, OpenPrice = 0, AccountId = AccountId },
                ep, contract, referenceDt, processingDt);
        }
        
        if (LoggingEnabled && LogLevel <= LogLevel.Debug)
            Logger.LogDebug("Exit OnExternalAccountPositionsSnapshot");
    }

    /// <summary>
    /// Checks positions for equality. If positions differ, books an offset trade(s) at the broker account and returns the list of these trades
    /// </summary>
    private void ValidatePositions(ExternalPositionReport p, Position ep, Contract contract, Instant referenceDt, Instant processingDt)
    {
        List<Trade> offsetTrades = new();
        
        var openPayments = contract.PLCalculator.GetValueInSettlementCcy(p.OpenPrice, Math.Abs(p.SignedVolume));
        if (p.SignedVolume != ep.SignedVolume || (p.SignedVolume != 0 && openPayments != ep.TotalOpenPayments))
        {
            if (LoggingEnabled && LogLevel <= LogLevel.Warning)
                Logger.LogWarning($"Positions for contract {contract.ContractId} do not match: expected={ep.SignedVolume} @ {ep.OpenPrice}, actual={p.SignedVolume} @ {p.OpenPrice}");

            var fxRate = 1m;
            if (contract.Template.SettlementCurrency.CurrencyId != AccountCurrency.CurrencyId)
            {
                fxRate = Query<GetConversionRate, decimal?>(new(contract.Template.SettlementCurrency.CurrencyId, AccountCurrency.CurrencyId))
                    ?? 1m; // TODO
            }
            
            offsetTrades = ep.GetOffsetTrades(ServiceName, TradeIdProvider, AccountId, contract.ContractId, 
                p.SignedVolume, openPayments, referenceDt, contract.PLCalculator, 
                contract.Template.SettlementCurrency.CurrencyId, fxRate);
            foreach (var t in offsetTrades)
            {
                ProcessTrade(t, processingDt);
            }
            SetAccountNeedsReconciliation($"Positions mismatch for contract {contract.ContractId} ({contract.Ticker})", processingDt);
        }
        
        if (offsetTrades.Count > 0)
        {
            // If there is only one strategy executed on this broker account, the position discrepancy can be attributed to this strategy
            var ssas = Query<GetSsaIdsForBrokerAccount, IReadOnlyCollection<int>>(new(AccountId));
            if (ssas.Count == 1)
            {
                var ssaId = ssas.Single();
                foreach (var trade in offsetTrades)
                {
                    AllocateTrade(trade, ssaId, processingDt);
                }
            }
        }
        
    }

    public void OnExternalAccountBalancesSnapshot(AccountBalancesSnapshot snapshot, Instant processingDt) =>
        OnExternalAccountBalancesSnapshot(snapshot.Balances, snapshot.UpdateTs, processingDt, true);
    
    private void OnExternalAccountBalancesSnapshot(IReadOnlyDictionary<string, decimal> balances, Instant referenceDt, Instant processingDt, bool notifyFund)
    {
        if (LoggingEnabled && LogLevel <= LogLevel.Information)
            Logger.LogInformation($"OnExternalAccountBalancesSnapshot");
        
        var assetsMap = balances.Keys.ToDictionary(
            key => key, 
            key => Query<GetAssetByExternalId, Asset?>(new(_brokerId, key))
        );
        var missingAssets = assetsMap.Where(kv => kv.Value == null).Select(kv => kv.Key).ToList();
        if (missingAssets.Count > 0)
        {
            if (LoggingEnabled && LogLevel <= LogLevel.Warning)
            {
                Logger.LogWarning($"Assets not found by externalId: {string.Join(',', missingAssets)}");
            }
            
            // TODO: add missing assets to the dead letter queue
        }
        
        var actualBalances = assetsMap
            .Where(kv => kv.Value != null)
            .ToDictionary(kv => kv.Value!, kv => balances[kv.Key]);
        
        foreach (var b in AccountStateReadonly.Balances.FullOuterJoin(
            actualBalances,
            kv => kv.Key,
            kv => kv.Key.AssetId,
            (l, r, k) => new { CcyId = k, Expected = l.Value, Actual = r.Value }
        ))
        {
            var delta = b.Actual - b.Expected;
            if (delta != 0)
            {
                if (LoggingEnabled && LogLevel <= LogLevel.Warning)
                {
                    Logger.LogWarning($"Balance mismatch for asset {b.CcyId}: expected={b.Expected}, actual={b.Actual}");
                }
                var bo = new NewBalanceOperation
                {
                    AccountId = AccountId,
                    AffectsBalance = true,
                    AffectsInvestment = false,
                    AffectsPnL = false,
                    Amount = delta,
                    AssetId = b.CcyId,
                    Description = $"Reconciliation {referenceDt}",
                    Dt = referenceDt,
                    ExternalId = $"Reconciliation {referenceDt} [{b.CcyId}]"
                };

                SetAccountNeedsReconciliation($"Balance mismatch for asset {b.CcyId}: expected={b.Expected}, actual={b.Actual}", processingDt);

                try
                {
                    ProcessBalanceOperation(bo, processingDt);
                }
                catch (MissingFxRateException e)
                {
                    SetAccountNeedsReconciliation($"Could not reconcile balance: {e.Message}", processingDt);
                }
            }
        }
    }
    
    public void OnFullSnapshot(ExternalAccountFullSnapshot snapshot, Instant processingDt)
    {
        if (LoggingEnabled && LogLevel <= LogLevel.Information)
            Logger.LogInformation($"OnFullSnapshot, processingDt={processingDt}");
        
        OnExternalAccountOrdersSnapshot(snapshot.Orders, snapshot.UpdateTs, processingDt);
        OnExternalAccountTradesReport(snapshot.Trades, processingDt);
        OnExternalAccountPositionsSnapshot(snapshot.Positions, snapshot.UpdateTs, processingDt);
        OnExternalAccountBalancesSnapshot(snapshot.Balances, snapshot.UpdateTs, processingDt, true);
        
        var evt = new BrokerAccountTradesReconciledEvt(EventIdProvider.GetNextEventId(), AccountId, _accountState.GetNextVersion(), processingDt);
        _accountState.Apply(evt, true);

        if (LoggingEnabled && LogLevel <= LogLevel.Debug)
            Logger.LogDebug("Exit OnFullSnapshot");
    }

    #endregion

    private void SetAccountNeedsReconciliation(string message, Instant timestamp)
    {
        var evt = new AccountReconciliationStatusChangedEvt(EventIdProvider.GetNextEventId(), AccountId, _accountState.GetNextVersion(),
            true, message, timestamp);
        _accountState.Apply(evt, true);
    }
    
    // private void EnsureContractIdAddedToUsed(int contractId, Instant processingDt)
    // {
    //     if (BrokerType == BrokerType.Binance)
    //     {
    //         if (!_accountState.UsedContractIds.Contains(contractId))
    //         {
    //             var evt = new ContractIdAddedToUsedContractsEvt(AccountId, contractId, referenceDt, processingDt);
    //             Apply(evt);
    //             Emit(evt);
    //         }
    //     }
    // }
}