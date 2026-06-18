using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Events.Strategies.AccountsService;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Domain.Strategies;

public class StrategyState : Aggregate, IStrategyStateReadonly
{
    [JsonConstructor]
    public StrategyState(
        int strategyId,
        long version, 
        SignalGroup? activeSignalGroup, 
        Instant lastCalculationTs, 
        string internalStateJson,
        IEventBus eventBus,
        ILoggerFactory loggerFactory
    ) : base(version)
    {
        StrategyId = strategyId;
        ActiveSignalGroup = activeSignalGroup;
        InternalStateJson = internalStateJson;
        Initialize(eventBus, loggerFactory.CreateLogger($"Strategy.State.{StrategyId}"));
    }

    public int StrategyId { get; }
    public SignalGroup? ActiveSignalGroup { get; protected set; }
    public Instant LastCalculationTs { get; protected set; }
    
    public string InternalStateJson { get; protected set; }

    public TState? GetInternalState<TState>() => 
        JsonSerializer.Deserialize<TState>(InternalStateJson, JsonSerializerOptions);
    
    protected static JsonSerializerOptions JsonSerializerOptions => new ()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true,
        WriteIndented = false,
    };

    public void Apply(StrategyInternalStateUpdatedEvt evt)
    {
        if (!base.Apply(evt)) return;
        InternalStateJson = evt.InternalStateJson;
    }
    
    protected void SetInternalState(object? state) => 
        InternalStateJson = JsonSerializer.Serialize(state, JsonSerializerOptions);

    public void Apply(StrategyLastCalculationTsUpdatedEvt evt)
    {
        if (!base.Apply(evt)) return;
        LastCalculationTs = evt.Ts;
    }

    public static StrategyState CreateNewState(QuantInfra.Sdk.Strategies.Strategy strategy, Instant ts, IEventBus eventBus, ILoggerFactory loggerFactory) => 
        new(strategy.StrategyId, 0, null, ts, "null", eventBus, loggerFactory);

    public static StrategyState FromStrategyStateReadonly(IStrategyStateReadonly state, IEventBus eventBus, ILoggerFactory loggerFactory) =>
        new(state.StrategyId, state.Version, state.ActiveSignalGroup, state.LastCalculationTs,
            state.InternalStateJson, eventBus, loggerFactory);
    
    public StrategyStateReadonly ToStrategyStateReadonly() => 
        new(StrategyId, ActiveSignalGroup, LastCalculationTs, InternalStateJson, Version);
}