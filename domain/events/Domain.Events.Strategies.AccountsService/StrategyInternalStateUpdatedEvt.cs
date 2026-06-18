using NodaTime;

namespace QuantInfra.Domain.Events.Strategies.AccountsService;

public record StrategyInternalStateUpdatedEvt(
    long EventId,
    int StrategyId,
    string InternalStateJson,
    long Version,
    Instant Timestamp
) : IStrategyEventBase;