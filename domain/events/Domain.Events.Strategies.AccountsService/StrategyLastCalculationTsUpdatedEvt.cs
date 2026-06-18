using NodaTime;

namespace QuantInfra.Domain.Events.Strategies.AccountsService;

public record StrategyLastCalculationTsUpdatedEvt(
    long EventId,
    int StrategyId,
    Instant Ts,
    long Version,
    Instant Timestamp
) : IStrategyEventBase;