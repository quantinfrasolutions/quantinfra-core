using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Domain.VirtualExecution.EventHandlers;

public class ForwardOrdersToVirtualExecutor : 
    IEventHandler<ExecutionReportEvt>,
    IConfigurableLoggingModule
{
    private readonly VirtualExecutor _virtualExecutor;
    private readonly IClock _clock;
    private readonly ILogger _logger;
    private bool _enableLogging = true;
    private readonly IQueryBus _queryBus;

    public ForwardOrdersToVirtualExecutor(VirtualExecutor virtualExecutor, IClock clock,
        ILogger<ForwardOrdersToVirtualExecutor> logger, IQueryBus queryBus)
    {
        _virtualExecutor = virtualExecutor;
        _clock = clock;
        _logger = logger;
        _queryBus = queryBus;
    }

    public void Handle(ExecutionReportEvt evt)
    {
        var er = evt.ExecutionReport;
        if (er is { IsVirtual: true, IsSuspended: false })
        {
            if (!er.OrdStatus.IsTerminal() && 
                (er.ExecTypeReason == ExecTypeReason.OrderChangeInitiated || er.ExecTypeReason == ExecTypeReason.SuspendedOrderActivated ))
            {
                if (_enableLogging && _logger.IsEnabled(LogLevel.Information)) _logger.LogInformation("Sending order {orderId} ({ordStatus}) to VE", er.OrderId, er.OrdStatus);
                
                var now = _clock.GetCurrentInstant();
                switch (er.OrdStatus)
                {
                    case OrdStatus.PendingNew:
                        _virtualExecutor.PlaceOrder(er, er.TransactTime, now, _queryBus, evt.AccountType == AccountType.VirtualAccount);
                        break;
                    case OrdStatus.PendingCancel:
                        _virtualExecutor.CancelOrder(er.ContractId, er.AccountId, er.OrderId, er.TransactTime, now,
                            _queryBus, evt.AccountType == AccountType.VirtualAccount);
                        break;
                    case OrdStatus.PendingReplace:
                        _virtualExecutor.ReplaceOrder(er.ContractId, er.AccountId, er.OrderId,
                            new OrderReplaceRequest { OrderQty = er.OrderQty, Price = er.Price, StopPx = er.StopPx },
                            er.TransactTime, now, _queryBus, evt.AccountType == AccountType.VirtualAccount);
                        break;
                }
            }
            // else if (er.OrdStatus.IsTerminal())
            // {
            //     if (_enableLogging && _logger.IsEnabled(LogLevel.Information))
            //         _logger.LogInformation("Ensuring order {orderId} ({ordStatus}) is removed from VE", er.OrderId, er.OrdStatus);
            //     _virtualExecutor.EnsureOrderIsRemoved(er.ContractId, er.AccountId, er.OrderId);
            // }
        }
    }

    public void EnableLogging()
    {
        _enableLogging = true;
    }

    public void DisableLogging()
    {
        _enableLogging = false;
    }
}