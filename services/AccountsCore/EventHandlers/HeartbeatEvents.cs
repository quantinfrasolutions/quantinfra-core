using System;
using AccountsCore;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Events.Accounts.AccountsService;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Services.AccountsCore.State;

namespace QuantInfra.Services.AccountsCore.EventHandlers;

internal class HeartbeatEvents(Config config, AccountServiceState state, IEventBus eventBus, IQueryBus queryBus, IClock clock) :
    IEventHandler,
    ICommandHandler<ProcessHeartbeatCmd>
{
    private readonly Duration _heartbeatInterval = config.HeartbeatInterval;
    
    public void Handle(IEvent e) => Handle(e.Timestamp);

    public void Handle(ProcessHeartbeatCmd cmd) => Handle(clock.GetCurrentInstant());

    private void Handle(Instant ts)
    {
        foreach (var accountId in state.AccountStates.Keys)
        {
            var account = queryBus.Query<GetAccount, IAccount?>(new(accountId));
            account!.OnHeartbeat(ts);
        }

        if (ts - state.LastProcessedHeartbeatTs >= _heartbeatInterval) eventBus.Emit(new AccountsServiceHeartbeatEvt(0, ts));
        state.LastProcessedHeartbeatTs = ts;
    }
}

public record ProcessHeartbeatCmd(Guid RequestId) : ICommand;