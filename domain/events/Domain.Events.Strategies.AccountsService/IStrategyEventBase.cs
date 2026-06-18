using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Domain.Events.Strategies.AccountsService;

public interface IStrategyEventBase : IAggregateEvent
{        
    int StrategyId { get; }
}

public interface IStrategyProjectionUpdatedEvt : IProjectionUpdatedEvent
{
    int StrategyId { get; }
}