using System.Collections.Generic;
using System.Linq;
using Common.Metrics;
using Microsoft.Extensions.Logging;
using NodaTime;
using Prometheus;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.Orders;
using QuantInfra.Sdk.Trading.Positions;

namespace QuantInfra.Domain.Accounts.AccountStateClientManager;

public class TradingAccount : AccountBaseState, ITradingAccount
{
    private readonly string _accountServiceName;
    private readonly IAccountsServiceApi _serviceApi;
    private readonly IClock _clock;
    private readonly Histogram? _brokerOrderRoundtrip;
    private readonly Histogram? _accountsServiceRoundtrip;
    private long _nextOrderId = -1;
    private readonly Dictionary<long, long> _orderSubmissionTimestamps = new();
    private long _sessionId;

    public TradingAccount(int accountId,
        PositionAccounting positionAccounting,
        IReadOnlyDictionary<int, decimal> balances,
        IEnumerable<OrderStatus> orders,
        IEnumerable<Position> positions,
        decimal sharePrice,
        decimal shareCount,
        decimal hwm,
        decimal investment,
        decimal returnSinceLastMtm,
        long version,
        string accountServiceName,
        IAccountsServiceApi serviceApi,
        IClock clock,
        IEventBus eventBus,
        ILoggerFactory loggerFactory, 
        Histogram? brokerOrderRoundtrip,
        Histogram? accountsServiceRoundtrip
    ) : base(accountServiceName, accountId, positionAccounting, balances, orders, positions, sharePrice,
        shareCount, hwm, investment, returnSinceLastMtm, version, eventBus, loggerFactory)
    {
        _accountServiceName = accountServiceName;
        _serviceApi = serviceApi;
        _clock = clock;
        _sessionId = clock.GetCurrentInstant().ToUnixTimeSeconds();
        _brokerOrderRoundtrip = brokerOrderRoundtrip;
        _accountsServiceRoundtrip = accountsServiceRoundtrip;
    }

    public void PlaceOrder(NewOrderSingle order, Instant processingDt)
    {
        var now = _clock.GetCurrentInstant();
        var tmpOrderId = _nextOrderId--;
        if (string.IsNullOrEmpty(order.ClOrdId)) order = new(order) { ClOrdId = $"{_sessionId}{tmpOrderId}" };
        var er = Order.CreateOrder(order, _accountServiceName, tmpOrderId, 0, _clock.GetCurrentInstant());
        ApplyExecutionReport(er);
        if (_brokerOrderRoundtrip is not null)
        {
            _orderSubmissionTimestamps[er.OrderId] = MetricsUtils.GetUnixMicro();
        }
        _serviceApi.PlaceOrder(_accountServiceName, order);
    }

    public void CancelOrder(OrderCancelRequest request, Instant processingDt)
    {
        var order = Orders.GetOrder(request.OrderId, request.OrigClOrdId);
        if (order == null) return;
        ApplyExecutionReport(order.CancelOrder(0,  _clock.GetCurrentInstant()));
        _serviceApi.CancelOrder(_accountServiceName, request);
    }

    public void ReplaceOrder(OrderReplaceRequest request, Instant processingDt)
    {
        var order = Orders.GetOrder(request.OrderId, request.OrigClOrdId);
        if (order == null) return;
        ApplyExecutionReport(order.RequestReplace(0, request, _clock.GetCurrentInstant()));
        _serviceApi.ReplaceOrder(_accountServiceName, request);
    }

    public override void Apply(ExecutionReportEvt evt, bool emit)
    {
        base.Apply(evt, emit);
        
        var er = evt.ExecutionReport;
        if (er.OrderId > 0
            && (
                er is { ExecType: ExecType.PendingNew, ExecTypeReason: ExecTypeReason.OrderChangeInitiated }
                || er.ExecType == ExecType.Rejected
                || er.ExecType == ExecType.Canceled
            )
        )
        {
            var clOrdId = er.ClOrdId;
            OrderStatus? newOrder = null;
            
            if (!string.IsNullOrEmpty(clOrdId))
            {
                newOrder = Orders.SingleOrDefault(o => o.ClOrdId == clOrdId && o.OrderId < 0);
            }

            if (newOrder != null)
            {
                RemoveNewOrder(newOrder.OrderId);

                if (_brokerOrderRoundtrip is not null 
                    && _orderSubmissionTimestamps.Remove(newOrder.OrderId, out var ts)
                    && er is { ExecType: ExecType.PendingNew, IsSuspended: false }
                )
                {
                    var now = MetricsUtils.GetUnixMicro();
                    _accountsServiceRoundtrip?.Observe(now - ts);
                    _orderSubmissionTimestamps[er.OrderId] = ts;
                    Logger.LogDebug("AccountsServiceRoundtrip observed, orderId: {orderId}, sentAt={ts}, now={now}", er.OrderId, ts, now);
                }
            }
        }

        // Received the first response from the broker
        if (_brokerOrderRoundtrip is not null && !string.IsNullOrEmpty(er.ExternalId) && 
            (er.ExecType == ExecType.New || er.ExecType == ExecType.Rejected) 
            && _orderSubmissionTimestamps.Remove(er.OrderId, out var origTs)
        )
        {
            var now = MetricsUtils.GetUnixMicro();
            _brokerOrderRoundtrip.Observe(now - origTs);
            Logger.LogDebug("BrokerOrderRoundtrip observed, orderId: {orderId}, origTs={origTs}, now={now}", er.OrderId,  origTs, now);
        }
    }
}