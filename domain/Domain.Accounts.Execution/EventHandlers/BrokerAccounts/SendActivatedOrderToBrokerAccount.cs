using Microsoft.Extensions.Logging;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Domain.Accounts.Execution.EventHandlers.BrokerAccounts;

/// <summary>
/// Handles events that occur when a new orders is placed on an SSA or ESA
/// </summary>
public sealed class SendActivatedOrderToBrokerAccount : IEventHandler<ExecutionReportEvt>
{
    private readonly IQueryBus _queryBus;
    private readonly ILogger<SendActivatedOrderToBrokerAccount> _logger;

    public SendActivatedOrderToBrokerAccount(IQueryBus queryBus, ILogger<SendActivatedOrderToBrokerAccount> logger)
    {
        _queryBus = queryBus;
        _logger = logger;
    }

    public void Handle(ExecutionReportEvt evt)
    {
        var er = evt.ExecutionReport;
        if (er is
            {
                ExecType: ExecType.TriggeredOrActivatedBySystem, 
                ExecTypeReason: ExecTypeReason.SuspendedOrderActivated,
                BrokerAccountId: not null
            })
        {
            var brokerAccount = _queryBus.Query<GetAccount, IBrokerAccount?>(new(er.BrokerAccountId.Value));
            brokerAccount.PlaceExternalOrder(er, evt.Timestamp);
            return;
        }

        // if (er is { ExecType: ExecType.PendingCancel, ExecTypeReason: ExecTypeReason.OrderChangeInitiated, IsSuspended: false })
        // {
        //     var brokerAccount = _queryBus.Query<GetAccount, IBrokerAccount?>(new(er.BrokerAccountId.Value));
        //     brokerAccount.CancelExternalOrder(er, evt.Timestamp);
        //     return;
        // }
        //
        // _logger.LogDebug($"Skipping execution report {er.ExecId}: order={er.OrderId}, execType={er.ExecType}, execTypeReason={er.ExecTypeReason}, isSuspended={er.IsSuspended}");
    }
}