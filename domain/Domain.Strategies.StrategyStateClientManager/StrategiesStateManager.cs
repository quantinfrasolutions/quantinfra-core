using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Domain.Events.Strategies.AccountsService;
using QuantInfra.Domain.Queries.Strategies;
using QuantInfra.Sdk.Strategies;
using GetStrategyState = QuantInfra.Domain.Queries.Strategies.AccountsService.GetStrategyState;

namespace QuantInfra.Domain.Strategies.StrategyStateClientManager;

public class StrategiesStateManager : 
    IExternalEventHandler<StrategyInternalStateUpdatedEvt>,
    IExternalEventHandler<StrategyLastCalculationTsUpdatedEvt>,
    IAsyncQueryResponseHandler<GetStrategyState, StrategyStateReadonly?>,
    IQueryHandler<global::QuantInfra.Domain.Queries.Strategies.GetStrategyState, IStrategyStateReadonly?>,
    IQueryHandler<GetStrategy, IStrategy?>
{
    private readonly IQueryBus _queryBus;
    private readonly ILogger _logger;
    private readonly IAccountsServiceApi _asApi;
    private readonly IEventBus _eventBus;
    private readonly ILoggerFactory _loggerFactory;

    private readonly AsyncRequestsManager<int> _requests = new();
    private readonly Dictionary<int, StrategyClient> _strategies = new();

    public StrategiesStateManager(IQueryBus queryBus, ILogger<StrategiesStateManager> logger,
        IAccountsServiceApi asApi, IEventBus eventBus, ILoggerFactory loggerFactory)
    {
        _queryBus = queryBus;
        _logger = logger;
        _asApi = asApi;
        _eventBus = eventBus;
        _loggerFactory = loggerFactory;
    }
    
    public IReadOnlyDictionary<int, IStrategyStateReadonly> StrategyStates => 
        _strategies.ToDictionary(kv => kv.Key, kv => (IStrategyStateReadonly)kv.Value);
    
    public void Apply(StrategyInternalStateUpdatedEvt evt) => ProcessEvent(evt, s => s.Apply(evt));
    public void Apply(StrategyLastCalculationTsUpdatedEvt evt) => ProcessEvent(evt, s => s.Apply(evt));

    
    private void ProcessEvent(IStrategyEventBase evt, Action<StrategyState> applyMethod)
    {
        var strategyId = evt.StrategyId;
        var strategy = _strategies.GetValueOrDefault(strategyId);
        if (strategy == null) return;
        
        try
        {
            applyMethod(strategy);
        }
        catch (MissingVersionException)
        {
            _logger.LogInformation($"Strategy {strategyId} is outdated (expected={strategy.Version}, received {evt.Version})");
            _strategies.Remove(strategyId); // State is obsolete
            
            // If there are no existing requests, send a new one
            if (_requests.TryCreateRequest(strategyId, out var requestId))
            {
                _logger.LogInformation($"Requesting state for strategy {strategyId}, requestId={requestId}");
                _queryBus.SendAsyncQuery<GetStrategyState, StrategyStateReadonly?>(new(requestId, strategyId, true));
            }
        }
    }

    public void Handle(AsyncQueryResponse<GetStrategyState, StrategyStateReadonly?> response)
    {
        _logger.LogDebug($"Strategy snapshot received: {response.Result?.StrategyId}");
        
        var newState = response.Result;
        if (newState == null)
        {
            CompleteStrategyStateRequest(response.RequestId, newState);
            return;
        }
        
        var existingState = _strategies.GetValueOrDefault(newState.StrategyId);
        if (existingState?.Version == newState.Version)
        {
            _logger.LogDebug($"Strategy {newState.StrategyId} is up to date, version={newState.Version}");
            CompleteStrategyStateRequest(response.RequestId, newState);
            return;
        }
        
        _strategies[newState.StrategyId] = new(newState.StrategyId, newState.Version,
            newState.ActiveSignalGroup, newState.LastCalculationTs,
            newState.InternalStateJson, _asApi, _eventBus, _loggerFactory);
        
        _logger.LogDebug($"Added strategy {newState.StrategyId}, version={newState.Version}");
        
        CompleteStrategyStateRequest(response.RequestId, newState);
        
        // TODO: Reconcile(newState, existingState); // Call a strategy with reconciliation results
    }
    
    private void CompleteStrategyStateRequest(Guid requestId, StrategyStateReadonly? result)
    {
        _asApi.OnStrategySnapshot(result, requestId);
        // Remove request, if it exists
        _requests.RemoveRequest(requestId);
    }

    public IStrategyStateReadonly? Handle(Domain.Queries.Strategies.GetStrategyState query) =>
        _strategies.GetValueOrDefault(query.StrategyId);

    public IStrategy? Handle(GetStrategy query) => _strategies.GetValueOrDefault(query.StrategyId);
}