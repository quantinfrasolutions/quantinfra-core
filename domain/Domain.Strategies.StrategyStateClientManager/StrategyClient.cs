using System;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Domain.Strategies.StrategyStateClientManager;

public class StrategyClient : StrategyState, IStrategy
{
    private readonly IAccountsServiceApi _accountsServiceApi;

    public StrategyClient(
        int strategyId, 
        long version, 
        SignalGroup? activeSignalGroup, 
        Instant lastCalculationTs, 
        string internalStateJson,
        IAccountsServiceApi accountsServiceApi,
        IEventBus eventBus,
        ILoggerFactory loggerFactory
    ) : base(strategyId, version, activeSignalGroup, lastCalculationTs, internalStateJson,  eventBus, loggerFactory)
    {
        _accountsServiceApi = accountsServiceApi;
    }

    public void Stop(string reason)
    {
        throw new NotSupportedException();
    }

    public void UpdateInternalState(object? state)
    {
        SetInternalState(state);
        _accountsServiceApi.UpdateStrategyInternalState(StrategyId, InternalStateJson);
    }

    public void UpdateLastCalculationTs(Instant ts)
    {
        LastCalculationTs = ts;
        _accountsServiceApi.UpdateStrategyLastCalculationTs(StrategyId, ts);
    }
}