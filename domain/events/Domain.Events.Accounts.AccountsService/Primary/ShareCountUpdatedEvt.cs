using NodaTime;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Primary;

public record ShareCountUpdatedEvt(
    long EventId,
    int AccountId,
    decimal Change,
    int BalanceOperationId,
    long Version,
    Instant Timestamp
) : IAccountEventBase;