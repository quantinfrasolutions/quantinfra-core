using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Commands.Accounts.AccountsService;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Domain.Accounts.Execution.CommandHandlers;

public class ProcessExternalExecutionReportCmdHandler : ICommandHandler<ProcessExternalExecutionReportCmd>
{
    private readonly IQueryBus _queryBus;
    private readonly IClock _clock;

    public ProcessExternalExecutionReportCmdHandler(IQueryBus queryBus, IClock clock)
    {
        _queryBus = queryBus;
        _clock = clock;
    }

    public void Handle(ProcessExternalExecutionReportCmd cmd)
    {
        var ba = _queryBus.Query<GetAccount, IBrokerAccount?>(new(cmd.ExecutionReport.AccountId));
        if (ba is null) return;
        ba.OnExternalExecutionReport(cmd.ExecutionReport, _clock.GetCurrentInstant());
    }
}