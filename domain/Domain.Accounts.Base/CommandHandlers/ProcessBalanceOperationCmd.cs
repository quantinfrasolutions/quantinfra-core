using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Commands.Accounts.AccountsService;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Domain.Accounts.Base.CommandHandlers;

public sealed class ProcessBalanceOperationCmdHandler : ICommandHandler<ProcessBalanceOperationCmd>
{
    private readonly IQueryBus _queryBus;
    private readonly IClock _clock;
    private readonly ILogger _logger;

    public ProcessBalanceOperationCmdHandler(IClock clock, IQueryBus queryBus, ILogger<ProcessBalanceOperationCmd> logger)
    {
        _clock = clock;
        _queryBus = queryBus;
        _logger = logger;
    }

    public void Handle(ProcessBalanceOperationCmd command)
    {
        _logger.LogDebug($"Handle {command}");
        var account = _queryBus.Query<GetAccount, IAccount?>(new (command.BalanceOperation.AccountId));
        account!.ProcessBalanceOperation(command.BalanceOperation, _clock.GetCurrentInstant(), command.RequestId);
    }
}