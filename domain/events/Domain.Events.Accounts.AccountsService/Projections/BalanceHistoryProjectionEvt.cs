using NodaTime;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Projections;

public record BalanceHistoryProjectionEvt(
    long EventId,
    int AccountId,
    int CurrencyId,
    decimal Change,
    decimal Balance,
    long? BalanceOperationId,
    long? TradeId,
    Instant Timestamp) : IAccountProjectionUpdatedEvt;