using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Events.Accounts.External;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Domain.Queries.Accounts.ExecutionService;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Accounts.ExternalAccounts;
using ExternalExecutionReportEvt = QuantInfra.Domain.Events.Accounts.External.ExternalExecutionReportEvt;

namespace QuantInfra.Services.AccountsCore.EventHandlers;

public class ExternalAccountsEventsHandler(
    IQueryBus queryBus,
    IClock clock
) : 
    IExternalEventHandler<ExternalExecutionReportEvt>,
    IExternalEventHandler<ExternalTradeEvt>,
    IExternalEventHandler<ExternalBalanceOperationEvt>,
    IExternalEventHandler<ExternalAccountConnectionRestoredEvt>,
    IExternalEventHandler<ExecutionServiceMissedVersionEvt>,
    IExternalEventHandler<ExternalOrderCancelRejectEvt>,
    IExternalEventHandler<ExternalAccountFullSnapshotEvt>,
    IExternalEventHandler<ExternalAccountOrdersSnapshotEvt>
{
    public void Apply(ExternalExecutionReportEvt e)
    {
        var ba = queryBus.Query<GetAccount, IBrokerAccount?>(new(e.AccountId));
        ba?.OnExternalExecutionReport(e.ExecutionReport, clock.GetCurrentInstant());
    }

    public void Apply(ExternalTradeEvt e)
    {
        var ba = queryBus.Query<GetAccount, IBrokerAccount?>(new(e.AccountId));
        ba?.OnExternalTrade(e.Trade, clock.GetCurrentInstant());
    }

    public void Apply(ExternalBalanceOperationEvt e)
    {
        var ba = queryBus.Query<GetAccount, IBrokerAccount?>(new(e.AccountId));
        ba?.OnExternalBalanceOperation(e.BalanceOperation, clock.GetCurrentInstant());
    }

    public void Apply(ExternalAccountConnectionRestoredEvt e)
    {
        var ba = queryBus.Query<GetAccount, IBrokerAccount?>(new(e.AccountId));
        ba?.OnExternalAccountConnectionRestoredEvt(clock.GetCurrentInstant());
    }

    public void Apply(ExecutionServiceMissedVersionEvt e)
    {
        var ba = queryBus.Query<GetAccount, IBrokerAccount?>(new(e.AccountId));
        ba?.OnExecutionServiceMissedVersionEvt(clock.GetCurrentInstant());
    }

    public void Apply(ExternalOrderCancelRejectEvt e)
    {
        var ba = queryBus.Query<GetAccount, IBrokerAccount?>(new(e.AccountId));
        ba?.OnExternalOrderCancelReject(e.Ocr, clock.GetCurrentInstant());
    }

    public void Apply(ExternalAccountFullSnapshotEvt e)
    {
        if (!e.SuccessfulRetrieval) return;
        
        var ba = queryBus.Query<GetAccount, IBrokerAccount?>(new(e.AccountId));
        ba?.OnFullSnapshot(e.Snapshot!, clock.GetCurrentInstant());
    }

    public void Apply(ExternalAccountOrdersSnapshotEvt e)
    {
        if (!e.SuccessfulRetrieval) return;
        
        var ba = queryBus.Query<GetAccount, IBrokerAccount?>(new(e.AccountId));
        ba?.OnExternalAccountOrdersSnapshot(e.Snapshot!, clock.GetCurrentInstant());
    }
}