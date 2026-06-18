using Disruptor.Dsl;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Domain.Accounts.AccountStateClientManager.Events;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.Events.Accounts.External;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Sdk.Accounts.AccountStates;
using QuantInfra.Sdk.Trading.ExternalAccounts;
using QuantInfra.Sdk.Trading.Infrastructure;
using QuantInfra.Sdk.Trading.Orders;
using QuantInfra.Services.ExecutionCore.Queries;
using ExternalExecutionReportEvt = QuantInfra.Domain.Events.Accounts.External.ExternalExecutionReportEvt;

namespace QuantInfra.Services.ExecutionCore.EventHandlers;

public class ExternalTradingEventsHandler(
    IQueryBus queryBus,
    ILogger<ExternalTradingEventsHandler> logger,
    Disruptor<OutgoingDisruptorMessage> outputDisruptor,
    IClock clock
) :
    IEventHandler<NewOrderSingleExternalCreatedEvt>,
    IEventHandler<OrderCancelRequestExternalCreatedEvt>,
    IEventHandler<OrderReplaceRequestExternalCreatedEvt>,
    IEventHandler<BrokerAccountNeedsOrdersReconciliationEvt>,
    IEventHandler<BrokerAccountNeedsTradesReconciliationEvt>,
    IEventHandler<AccountStateReconciledEvt>,
    IEventHandler<AccountMissingVersionEvt>,
    
    IEventHandler<ExternalAccountConnectionRestoredEvt>,
    IEventHandler<ExternalExecutionReportEvt>,
    IEventHandler<ExternalOrderCancelRejectEvt>,
    IEventHandler<ExternalTradeEvt>,
    IEventHandler<ExternalBalanceOperationEvt>,
    IEventHandler<ExternalAccountFullSnapshotEvt>,
    IEventHandler<ExternalAccountOrdersSnapshotEvt>
{
    private readonly Dictionary<int, Dictionary<long, NewOrderSingleExternal>> _ordersWithoutExternalIds = new();
    private readonly HashSet<int> _fullsnapshotRequests = new();
    private readonly HashSet<int> _ordersRequests = new();
    
    public void Handle(NewOrderSingleExternalCreatedEvt evt)
    {
        var account = queryBus.Query<GetAccount, IBrokerAccountStateReadonly?>(new(evt.AccountId));
        if (account is null) return; // Account state has been removed from the cache because of missing version
        
        var client = queryBus.Query<GetTradingClient, IHostedTradingClient?>(new(evt.AccountId));
        if (client == null)
        {
            var now = clock.GetCurrentInstant();
            outputDisruptor.PublishMessage(new QuantInfra.Domain.Events.Accounts.External.ExternalExecutionReportEvt(
                evt.AccountId,
                evt.Order.OutrightReject(now, rejectReason: RejectReason.ApplicationNotAvailable, rejectText: "Trading client not found"), 
                now
            ));
            return;
        }
        
        _ordersWithoutExternalIds.TryAdd(evt.AccountId, new());
        _ordersWithoutExternalIds[evt.AccountId].Add(evt.Order.OrderId, evt.Order);
        
        client.PlaceOrder(evt.Order);
    }

    public void Handle(OrderCancelRequestExternalCreatedEvt evt)
    {
        var account = queryBus.Query<GetAccount, IBrokerAccountStateReadonly?>(new(evt.AccountId));
        if (account is null) return; // Account state has been removed from the cache because of missing version
        
        var client = queryBus.Query<GetTradingClient, IHostedTradingClient?>(new(evt.AccountId));
        if (client is null)
        {
            // TODO
            return;
        }
        client.CancelOrder(evt.Ocr);
    }
    
    public void Handle(OrderReplaceRequestExternalCreatedEvt evt)
    {
        var account = queryBus.Query<GetAccount, IBrokerAccountStateReadonly?>(new(evt.AccountId));
        if (account is null) return; // Account state has been removed from the cache because of missing version
        
        var client = queryBus.Query<GetTradingClient, IHostedTradingClient?>(new(evt.AccountId));
        if (client is null)
        {
            // TODO
            return;
        }
        client.ReplaceOrder(evt.Ocr);
    }

    public void Handle(BrokerAccountNeedsOrdersReconciliationEvt evt)
    {
        var client = queryBus.Query<GetTradingClient, IHostedTradingClient?>(new(evt.AccountId));
        if (client is null)
        {
            // TODO
            return;
        }
        RequestOrdersSnapshot(client, evt.AccountId);
    }

    private void RequestOrdersSnapshot(IHostedTradingClient client, int accountId)
    {
        // Allow only one snapshot request at a time. If there is an active full snapshot request, it will return the orders
        if (!_fullsnapshotRequests.Contains(accountId) && _ordersRequests.Add(accountId))
        {
            logger.LogInformation("Requesting orders snapshot for account {accountId}", accountId);
            client.RequestAccountOrdersSnapshot();
        }
    }

    public void Handle(BrokerAccountNeedsTradesReconciliationEvt evt)
    {
        var client = queryBus.Query<GetTradingClient, IHostedTradingClient?>(new(evt.AccountId));
        if (client is null)
        {
            // TODO
            return;
        }
        RequestFullReconciliation(client, evt.AccountId, evt.LastReceivedTradesDts, evt.LastReceivedBalanceOperationsDt);
    }

    private void RequestFullReconciliation(IHostedTradingClient client, int accountId, IReadOnlyDictionary<string, Instant> lastReceivedTradesDts, Instant lastReceivedBalanceOperationsDt)
    {
        // Allow only one state request at a time
        if (_fullsnapshotRequests.Add(accountId))
        {
            logger.LogInformation("Requesting full snapshot for account {accountId}", accountId);
            client.RequestAccountFullSnapshot(lastReceivedTradesDts, lastReceivedBalanceOperationsDt);
        }
    }

    public void Handle(AccountMissingVersionEvt evt)
    {
        outputDisruptor.PublishMessage(new ExecutionServiceMissedVersionEvt(evt.AccountId, clock.GetCurrentInstant()));
    }
    
    public void Handle(AccountStateReconciledEvt evt)
    {
        var account = queryBus.Query<GetAccount, IBrokerAccountStateReadonly?>(new(evt.AccountId));
        if (account is null) throw new InvalidOperationException("Account state doesn't exist when AccountStateReconciledEvt is emitted"); // Something went wrong
        
        var client = queryBus.Query<GetTradingClient, IHostedTradingClient?>(new(evt.AccountId));
        if (client is null)
        {
            // TODO
            return;
        }
        
        if (account.NeedsTradesReconciliation)
            RequestFullReconciliation(client, evt.AccountId, account.UsedContractIds, account.LastReceivedBalanceOperationTs);

        if (account.NeedsOrdersReconciliation)
            RequestOrdersSnapshot(client, evt.AccountId);
    }

    public void Handle(ExternalAccountConnectionRestoredEvt evt) => outputDisruptor.PublishMessage(evt);
    
    public void Handle(ExternalExecutionReportEvt evt)
    {
        var er = evt.ExecutionReport;
        
        if (long.TryParse(er.ClOrdId, out var orderId)
            && _ordersWithoutExternalIds.TryGetValue(er.AccountId, out var orders)
            && orders.Remove(orderId)
           )
        {
            er = new(er) { OrderId = orderId };
            evt = new ExternalExecutionReportEvt(er.AccountId, er, clock.GetCurrentInstant());
        }
        
        outputDisruptor.PublishMessage(evt);
    }

    public void Handle(ExternalOrderCancelRejectEvt evt) => outputDisruptor.PublishMessage(evt);

    public void Handle(ExternalTradeEvt evt) => outputDisruptor.PublishMessage(evt);

    public void Handle(ExternalBalanceOperationEvt evt) => outputDisruptor.PublishMessage(evt);

    public void Handle(ExternalAccountFullSnapshotEvt evt)
    {
        _fullsnapshotRequests.Remove(evt.AccountId);
        outputDisruptor.PublishMessage(evt);
    }

    public void Handle(ExternalAccountOrdersSnapshotEvt evt)
    {
        _ordersRequests.Remove(evt.AccountId);
        outputDisruptor.PublishMessage(evt);
    }
}