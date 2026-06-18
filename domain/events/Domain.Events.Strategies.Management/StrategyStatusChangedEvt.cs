using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Domain.Events.Strategies.Management;

public record struct StrategyStatusChangedEvt(
    long EventId,
    int StrategyId,
    StrategyStatus Status,
    Instant Timestamp
) : IEvent;