using System.Linq;
using Common.Trading;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.Queries.MarketData;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Domain.Accounts.Base;

/// <summary>
/// Adds support for virtual execution:
/// * Booking of a trade upon receiving a Fill ER
/// * Order cancellation logic for fills (when a position requested to be open by an order is opened already, or when requested
///     to be closed and is closed already). TODO: check why this happens
/// * OCO/OTO linked orders
/// </summary>
public sealed class VirtualAccount : AccountBase
{
    // private readonly VirtualExecutor _virtualExecutor;
    private readonly ILogger _logger;

    public VirtualAccount(
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
    ) : base(accountRecord, accountStateReadonly, eventIdProvider, balanceOperationsIdProvider, orderIdProvider, execIdProvider, tradeIdProvider, eventBus, queryBus, loggerFactory, logLevel)
    {
        // if (accountState.AccountType != AccountType.VirtualAccount)
        //     throw new ArgumentException($"Account {accountState.AccountId} is {accountState.AccountType} instead of Virtual");
        
        _logger = loggerFactory.CreateLogger(GetLoggerCategory("Virtual"));
    }

    // 
    public override void ProcessExecutionReport(ExecutionReport? er, Instant processingDt)
    {
        #if PROFILE
        using (Profiler.Step("VirtualAccount.ProcessExecutionReport"))
        {
        #endif
        if (er == null) return;
        
        if (LoggingEnabled && _logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("ProcessExecutionReport, er={er}", er.GetLogString());
        
        // check if the position has already been opened or closed and cancel the order if yes
        if (AccountRecord.PositionAccounting == PositionAccounting.Hedged && er.ExecType == ExecType.Fill)
        {
            var positions = AccountStateReadonly.Positions.Where(p => p.ContractId == er.ContractId).ToList();
            if (
                // if a position with the given id has already been opened, cancel the order
                // at the moment, partial execution is not supported
                (er.PositionEffect == PositionEffect.Open &&
                 positions.Any(p => p.StrategyPositionId == er.StrategyPositionId))
                ||
                // executed order is trying to close an already closed position
                (er.PositionEffect == PositionEffect.Close && positions.All(p => p.StrategyPositionId != er.StrategyPositionId))
            )
            {
                if (LoggingEnabled && Logger.IsEnabled(LogLevel.Information))
                    _logger.LogInformation("Canceling order as the target position already changed");
                
                var order = AccountStateReadonly.Orders.SingleOrDefault(o => o.OrderId == er.OrderId);
                // Order may be null in case Virtual Executor triggers both SL and TP at the same bar.
                // In this case the second order will be cancelled while processing the Fill of the first order.
                if (order != null)
                {
                    ProcessExecutionReport(
                        order.CancelOrder(ExecIdProvider, processingDt, rejectReason: RejectReason.DuplicateVirtualAccountExecution, immediateCancellation: true),
                        processingDt
                    );
                }
        
                return;
            }
        }
        
        base.ProcessExecutionReport(er, processingDt);
        
        if (er.ExecType == ExecType.Fill)
        {
            // This is not required. Let the user utilize OCO orders
            // if (AccountRecord.PositionAccounting == PositionAccounting.Hedged)
            // {
            //     // Cancel the orders with the same position effect
            //     var orders = AccountState.Orders
            //         .Where(o => o.ContractId == er.ContractId && o.StrategyPositionId == er.StrategyPositionId && o.PositionEffect == er.PositionEffect)
            //         .ToList();
            //     if (orders.Count > 0)
            //     {
            //         if (LoggingEnabled && LogLevel <= LogLevel.Information)
            //             _logger.LogInformation($"Canceling {orders.Count} orders with the same PositionEffect");
            //         foreach (var o in orders)
            //         {
            //             CancelOrder(o, processingDt);
            //         }
            //     }
            // }
            
            var contract = Query<GetContract, Contract?>(new(er.ContractId));
            var calculator = contract.PLCalculator;
            // TODO: different cost for market and limit orders
        
            var tradeValue = calculator.GetValueInSettlementCcy(er.LastPx!.Value, er.LastQty!.Value);
        
            var commissions = contract.Template.Commissions
                .ToDictionary(
                    cs => cs.CommissionId,
                    cs => cs.GetCommission(er.LastQty!.Value, tradeValue, contract.Template.SettlementCurrency.Decimals)
                );
        
            var commission = commissions.Count > 0
                ? commissions.Sum(kv => kv.Value)
                : 0m;
            
            var fxRate = contract.Template.SettlementCurrency.CurrencyId == AccountCurrency.CurrencyId
                ? 1m
                : Query<GetConversionRate, decimal?>(new(contract.Template.SettlementCurrency.CurrencyId, AccountCurrency.CurrencyId));
        
            var trade = er.FillToTrade(
                TradeIdProvider.GetNextTradeId(),
                commission,
                commissions,
                contract.Template.SettlementCurrency.CurrencyId,
                fxRate ?? 0m,
                contract.IsSynthetic()
            );
        
            ProcessTrade(trade!, processingDt);
        
            // TODO: move to the strategy level
            // if (er.PositionEffect == PositionEffect.Close && StrategyConfig.LiquidationParameters?.LiquidateAt ==
            //     LiquidationTiming.OnPositionClose)
            // {
            //     CheckRiskLimits(referenceDt, processingDt);
            // }
            
            if (LoggingEnabled && Logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Exit ProcessExecutionReport");
        }
        #if PROFILE
        }
        #endif
    }

    // public override void PlaceOrder(Order order, Instant referenceDt, Instant processingDt)
    // {
    //     var er = PlaceOrderBase(order, referenceDt, processingDt);
    //     if (!er.OrdStatus.IsTerminal() && er.OrdStatus != OrdStatus.Suspended)
    //     {
    //         _virtualExecutor.PlaceOrder(er, referenceDt, processingDt, this,
    //             StaticDataProvider.GetContract(order.ContractId));
    //     }
    // }

    protected override void CancelOrder(OrderStatus order, Instant processingDt, string? requestId = null)
    {
        // Suspended orders are not placed to VE
        if (order.OrdStatus != OrdStatus.Suspended)
        {
            base.CancelOrder(order, processingDt, requestId);
            // _virtualExecutor.CancelOrder(order.ContractId, order.AccountId, order.OrderId, referenceDt, processingDt,
            //     this, StaticDataProvider.GetContract(order.ContractId));
        }
        else
        {
            base.ProcessExecutionReport(order.CancelOrder(ExecIdProvider, processingDt, immediateCancellation: true, requestId: requestId), processingDt);
        }
    }

    protected override void ReplaceOrder(OrderStatus order, OrderReplaceRequest request, Instant processingDt)
    {
        if (order.OrdStatus != OrdStatus.Suspended)
        {
            base.ReplaceOrder(order, request, processingDt);
            // _virtualExecutor.ReplaceOrder(order.ContractId, order.AccountId, order.OrderId, request, referenceDt, processingDt, this);
        }
        else
        {
            base.ProcessExecutionReport(order.ConfirmReplace(ExecIdProvider.GetNextExecId(), request, processingDt), processingDt);
        }
    }
}