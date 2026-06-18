using System.Collections.Generic;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Queries.Strategies;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Services.AccountsCore.State;
using Strategy = QuantInfra.Domain.Strategies.Strategy;

namespace QuantInfra.Services.AccountsCore;

public class StrategiesFactory(
    AccountServiceState state,
    IEventBus eventBus,
    IQueryBus queryBus,
    IClock clock
) :
    IQueryHandler<GetStrategy, IStrategy?>,
    IQueryHandler<GetStrategyConcreteImplementation, Strategy?>
{
    private readonly Dictionary<int, Strategy> _strategies = new();

    IStrategy? IQueryHandler<GetStrategy, IStrategy?>.Handle(GetStrategy query) => 
        ((IQueryHandler<GetStrategyConcreteImplementation, Strategy>)this).Handle(new(query.StrategyId));


    Strategy IQueryHandler<GetStrategyConcreteImplementation, Strategy?>.Handle(GetStrategyConcreteImplementation query)
    {
        if (!_strategies.TryGetValue(query.StrategyId, out var strategy))
        {
            var strategyRecord = queryBus.Query<GetStrategyState, IStrategyStateReadonly?>(new(query.StrategyId));
            if (strategyRecord is null) return null;
            
            var strategyState = state.StrategyStates[query.StrategyId];
            strategy = new(strategyState, state, eventBus, queryBus, clock);
            _strategies[query.StrategyId] = strategy;
        }

        return strategy;
    }
}

internal record GetStrategyConcreteImplementation(int StrategyId) : IQuery<Strategy?>;