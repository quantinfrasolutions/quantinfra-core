using NodaTime;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Projections;

public record RealizedPnLAccruedEvt(
    long EventId,
    int AccountId,
    decimal PnL,
    long? TradeId,
    long? BalanceOperationId,
    Instant Timestamp
) : IAccountProjectionUpdatedEvt;