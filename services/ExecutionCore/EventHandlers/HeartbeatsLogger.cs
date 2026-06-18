using Microsoft.Extensions.Logging;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Events.Accounts.AccountsService;

namespace QuantInfra.Services.ExecutionCore.EventHandlers;

public class HeartbeatsLogger(Config config, ILogger<HeartbeatsLogger> logger)
    : IExternalEventHandler<AccountsServiceHeartbeatEvt>
{
    private readonly bool _loggingEnabled = config.EnableHeartbeatsLogging;

    public void Apply(AccountsServiceHeartbeatEvt e)
    {
        if (_loggingEnabled) logger.LogDebug("{seqNo} {ts}", e.EventId, e.Timestamp);
    }
}