using Microsoft.Extensions.Logging;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Events.Strategies.Management;

namespace QuantInfra.Domain.StrategyRecordsStateManager.ExternalEventHandlers;

public class StrategyUpdatesHandler : 
    IExternalEventHandler<StrategyCreatedEvt>,
    IExternalEventHandler<StrategyStatusChangedEvt>
{
    private readonly StrategiesServiceStateManager _strategiesServiceStateManager;
    private readonly ILogger _logger;
    private readonly ICommandBus _commandBus;

    public StrategyUpdatesHandler(StrategiesServiceStateManager strategiesServiceStateManager, ILogger<StrategyUpdatesHandler> logger, ICommandBus commandBus)
    {
        _strategiesServiceStateManager = strategiesServiceStateManager;
        _logger = logger;
        _commandBus = commandBus;
    }
    
    public void Apply(StrategyCreatedEvt e)
    {
        if (e.Strategy.StrategyServiceName == _strategiesServiceStateManager.Config.StrategiesServiceName)
        {
            _logger.LogInformation($"Adding strategy {e.Strategy.StrategyId}");
            _strategiesServiceStateManager.SetStrategyRecord(e.Strategy, e.Account);
        }
    }

    public void Apply(StrategyStatusChangedEvt e)
    {
        if (_strategiesServiceStateManager.StrategyRecords.TryGetValue(e.StrategyId, out var strategy))
        {
            _logger.LogInformation($"Update strategy {strategy.StrategyId} status to {e.Status}");
            strategy.Status = e.Status;
        }
    }
}