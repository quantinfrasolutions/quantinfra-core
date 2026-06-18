using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Events.Strategies.AccountsService;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Domain.Strategies;

public class Strategy : Processor, IStrategy
{
    private readonly StrategyState _state;
    private readonly IEventIdProvider _eventIdProvider;
    private readonly IClock _clock;

    public Strategy(StrategyState state, IEventIdProvider eventIdProvider, IEventBus eventBus, IQueryBus queryBus, IClock clock) 
        : base(eventIdProvider, eventBus, queryBus)
    {
        _state = state;
        _eventIdProvider = eventIdProvider;
        _clock = clock;
    }

    public void Stop(string reason)
    {
        throw new System.NotImplementedException();
    }

    public void UpdateInternalState(object? state)
    {
        UpdateInternalStateFromString(state?.SerializeInternalState() ?? "null");
    }

    public void UpdateInternalStateFromString(string state)
    {
        var evt = new StrategyInternalStateUpdatedEvt(
            _eventIdProvider.GetNextEventId(),
            _state.StrategyId,
            state,
            _state.GetNextVersion(),
            _clock.GetCurrentInstant()
        );
        _state.Apply(evt);
        Emit(evt);
    }

    public void UpdateLastCalculationTs(Instant ts)
    {
        var evt = new StrategyLastCalculationTsUpdatedEvt(
            _eventIdProvider.GetNextEventId(),
            _state.StrategyId,
            ts,
            _state.GetNextVersion(),
            _clock.GetCurrentInstant()
        );
        _state.Apply(evt);
        Emit(evt);
    }
}