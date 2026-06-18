using NodaTime;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Primary;

public record SharePriceUpdatedEvt(
    long EventId,
    int AccountId,
    decimal Equity,
    decimal SharePrice,
    decimal DailyReturn,
    long Version,
    Instant ReferenceDt,
    Instant Timestamp
) : IAccountEventBase;