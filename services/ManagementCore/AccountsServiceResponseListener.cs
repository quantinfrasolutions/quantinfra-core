using ManagementCore;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Interfaces.Api.Accounts;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.Patterns.TopicMulticast;
using QuantInfra.Connectors.Common;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Sdk.Trading.Orders;
using QuantInfra.Sdk.Trading.Positions;

namespace QuantInfra.Services.ManagementCore;

public class AccountsServiceResponseListener : IMulticastListenerEventHandler
{
    private readonly RequestsManager<NewOrderIdentifier> _newOrdersRequestsManager;
    private readonly RequestsManager<Guid> _requestsManager;
    private readonly IMulticastMessageFactory _messageFactory;

    public AccountsServiceResponseListener(
        RequestsManager<NewOrderIdentifier> newOrdersRequestsManager,
        RequestsManager<Guid> requestsManager,
        IMulticastMessageFactory messageFactory
    )
    {
        _newOrdersRequestsManager = newOrdersRequestsManager;
        _requestsManager = requestsManager;
        _messageFactory = messageFactory;
    }

    public void HandleIncomingMessage(DownstreamMessage message)
    {
        if (message.MessageType != MessageType.DataMessage) return;
        var msg = _messageFactory.Parse(message);
        
        switch (msg)
        {
            case ExecutionReportEvt er:
                var id = new NewOrderIdentifier(er.AccountId, er.ExecutionReport.ClOrdId ?? "");
                _newOrdersRequestsManager.CompleteRequest(id, er.ExecutionReport);
                if (!string.IsNullOrEmpty(er.ExecutionReport.RequestId) && Guid.TryParse(er.ExecutionReport.RequestId, out var clOrdId))
                {
                    _requestsManager.CompleteRequest(clOrdId, er.ExecutionReport);
                }
                break;
            case OrderCancelRejectEvt ocr:
                if (!Guid.TryParse(ocr.Ocr.RequestId, out var requestId)) return;
                _requestsManager.FailRequest(requestId, "Cannot cancel/replace order");
                break;
            case AsyncQueryResponse<GetActiveOrders, IReadOnlyCollection<OrderStatus>> activeOrdersResponse:
                _requestsManager.CompleteRequest(activeOrdersResponse.RequestId, activeOrdersResponse.Result);
                break;
            case AsyncQueryResponse<GetPositions, IReadOnlyCollection<Position>> activePositionsResponse:
                _requestsManager.CompleteRequest(activePositionsResponse.RequestId, activePositionsResponse.Result);
                break;
            case AsyncQueryResponse<GetBalances, IReadOnlyDictionary<int, decimal> > balancesResponse:
                _requestsManager.CompleteRequest(balancesResponse.RequestId, balancesResponse.Result);
                break;
            case BalanceOperationProcessedEvt bo:
                if (bo.RequestId.HasValue) _requestsManager.CompleteRequest(bo.RequestId.Value, bo.BalanceOperation.BalanceOperationId);
                break;
            case AsyncQueryResponse<GetBrokerAccountReconciliationStatus, BrokerAccountReconciliationStatus?> br:
                _requestsManager.CompleteRequest(br.RequestId, br.Result);
                break;
            case AccountReconciliationStatusChangedEvt recon:
                if (recon.RequestId.HasValue) _requestsManager.CompleteRequest(recon.RequestId.Value, recon);
                break;
        }
    }
}

public class Config
{
    public List<string> AccountServicesEndpoints { get; set; }
}