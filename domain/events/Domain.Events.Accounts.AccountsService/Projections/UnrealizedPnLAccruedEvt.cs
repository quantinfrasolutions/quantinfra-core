using NodaTime;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Projections;

public record UnrealizedPnLAccruedEvt(
    long EventId,
    int AccountId,
    decimal PnL,
    Instant Timestamp
) : IAccountProjectionUpdatedEvt;