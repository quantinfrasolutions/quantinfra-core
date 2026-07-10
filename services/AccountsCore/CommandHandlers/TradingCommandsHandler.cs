using AccountsCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Commands.Accounts.AccountsService;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Services.AccountsCore.CommandHandlers;

public class TradingCommandsHandler(
    Config config,
    ILogger<TradingCommandsHandler> logger,
    IQueryBus queryBus,
    IClock clock
) :
    ICommandHandler<NewOrderCmd>,
    ICommandHandler<ReplaceOrderCmd>,
    ICommandHandler<CancelOrderCmd>,
    ICommandHandler<ClearBrokerAccountReconciliationStatus>,
    IConfigurableLoggingModule
{
    private readonly ILogger _logger = logger;
    private readonly LogLevel _logLevel = config.LogLevel;

    private bool _loggingEnabled = true;

    public void DisableLogging() => _loggingEnabled = false;
    public void EnableLogging() => _loggingEnabled = true;

    public void Handle(NewOrderCmd command)
    {
        if (_loggingEnabled) _logger.LogDebug("Handle {command}", command);
        
        var account = queryBus.Query<GetAccount, IAccount?>(new (command.Order.AccountId));
        var now = clock.GetCurrentInstant();
        // TODO: account is null
        account.PlaceOrder(command.Order, now);
    }

    public void Handle(ReplaceOrderCmd cmd)
    {
        if (_loggingEnabled) _logger.LogDebug("Handle {cmd}", cmd);
        
        var account = queryBus.Query<GetAccount, IAccount?>(new (cmd.Ocr.AccountId));
        if (account is null) return;
        
        var now = clock.GetCurrentInstant();
        account.ReplaceOrder(cmd.Ocr, now);
    }

    public void Handle(CancelOrderCmd cmd)
    {
        if (_loggingEnabled) _logger.LogDebug("Handle {cmd}", cmd);
        
        var account = queryBus.Query<GetAccount, IAccount?>(new (cmd.Ocr.AccountId));
        if (account is null) return;
        var now = clock.GetCurrentInstant();
        // TODO: account is null
        account.CancelOrder(cmd.Ocr, now);
    }

    public void Handle(ClearBrokerAccountReconciliationStatus cmd)
    {
        if (_loggingEnabled) _logger.LogDebug("Handle {cmd}", cmd);
        
        var account = queryBus.Query<GetAccount, IBrokerAccount?>(new (cmd.AccountId));
        if (account is null) return;
        account.Reconcile(clock.GetCurrentInstant(), cmd.RequestId);
    }
}