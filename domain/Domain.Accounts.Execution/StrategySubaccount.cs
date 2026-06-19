using System;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Accounts.Base;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Domain.Accounts.Execution;

// TODO: rename to StrategyExecutionAccount
public class StrategySubaccount : AccountBase
{
    public StrategySubaccount(
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
    ) : base(accountRecord, accountStateReadonly, eventIdProvider, balanceOperationsIdProvider, orderIdProvider, execIdProvider,
        tradeIdProvider, eventBus, queryBus, loggerFactory, logLevel)
    {
    }

    public override void PlaceOrder(NewOrderSingle order, Instant processingDt)
    {
        if (order.AccountId != AccountId)
            throw new InvalidOperationException("Order's AccountId doesn't match the account");
        
        var (er, contract) = ValidateOrder(order, processingDt);
        if (er.OrdStatus.IsTerminal())
        {
            ProcessExecutionReport(er, processingDt);
            return;
        }
        
        var brokerId = contract!.Template.Broker.BrokerId;
        var brokerAccountId = Query<GetBrokerAccountForSsa, int?>(new(AccountId, brokerId));
        if (!brokerAccountId.HasValue)
        {
            if (LoggingEnabled && Logger.IsEnabled(LogLevel.Warning))
                Logger.LogWarning($"Order rejected: broker account not configured for contract {order.ContractId}");
            ProcessExecutionReport(
                er.RejectOrder(ExecIdProvider.GetNextExecId(),
                    RejectReason.UnknownAccount, $"Broker account not configured for contract {order.ContractId}", 
                    processingDt
                ),
                processingDt
            );
            return;
        }
        
        var brokerAccount = Query<GetAccount, IBrokerAccount?>(new(brokerAccountId.Value));
        if (brokerAccount is null)
        {
            if (LoggingEnabled && Logger.IsEnabled(LogLevel.Warning))
                Logger.LogWarning($"Order rejected: broker account {brokerAccountId} cannot be retrieved");
            ProcessExecutionReport(
                er.RejectOrder(ExecIdProvider.GetNextExecId(),
                    RejectReason.UnknownAccount, $"Broker account {brokerAccountId} cannot be retrieved", 
                    processingDt
                ),
                processingDt
            );
            return;
        }

        er = new ExecutionReport(er) { BrokerAccountId = brokerAccountId.Value };
        
        ProcessExecutionReport(er, processingDt);
        
        if (er.IsVirtual || er.OrdStatus.IsTerminal() || er.IsSuspended) return;
        brokerAccount.PlaceExternalOrder(er, processingDt);
    }

    protected override void CancelOrder(OrderStatus order, Instant processingDt, string? requestId = null)
    {
        var brokerId = Query<GetContract, Contract?>(new(order.ContractId))!.Template.Broker.BrokerId;
        var brokerAccountId = Query<GetBrokerAccountForSsa, int?>(new(AccountId, brokerId));
        
        if (!brokerAccountId.HasValue)
        {
            if (LoggingEnabled && Logger.IsEnabled(LogLevel.Warning))
                Logger.LogWarning("Trying to cancel an order with no broker account configured");
            ProcessExecutionReport(order.CancelOrder(ExecIdProvider, processingDt, immediateCancellation: true), processingDt);
            return;
        }

        var er = base.CancelOrderInternal(order, processingDt, requestId);
        if (er?.OrdStatus == OrdStatus.PendingCancel)
        {
            var brokerAccount = Query<GetAccount, IBrokerAccount?>(new(brokerAccountId.Value));
            brokerAccount.CancelExternalOrder(er, processingDt);
        }
    }

    protected override void ReplaceOrder(OrderStatus order, OrderReplaceRequest request, Instant processingDt)
    {
        var er = base.ReplaceOrderInternal(order, request, processingDt);
        if (er is { BrokerAccountId: not null, IsVirtual: false, OrdStatus: OrdStatus.PendingReplace })
        {
            var brokerAccount = Query<GetAccount, IBrokerAccount?>(new(er.BrokerAccountId.Value));
            brokerAccount.ReplaceExternalOrder(request, er, processingDt); 
        }
    }
}