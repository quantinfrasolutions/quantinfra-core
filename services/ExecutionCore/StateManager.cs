using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Domain.Accounts.AccountStateClientManager;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Sdk.Trading.Infrastructure;
using QuantInfra.Services.ExecutionCore.Queries;

namespace QuantInfra.Services.ExecutionCore;

internal sealed class StateManager : AccountsStateManager
{
    private readonly IQueryBus _queryBus;

    public StateManager(IEventBus eventBus, IQueryBus queryBus, IAccountsServiceApiReadonly serviceApi, ILoggerFactory loggerFactory, IClock clock) 
        : base(eventBus, queryBus, serviceApi, loggerFactory, clock)
    {
        _queryBus = queryBus;
    }

    protected override void OnMissingVersion(AccountBaseState state, long receivedVersion)
    {
        base.OnMissingVersion(state, receivedVersion);
        var client = _queryBus.Query<GetTradingClient, IHostedTradingClient?>(new(state.AccountId));
        client?.RequestAccountOrdersSnapshot();
    }
}