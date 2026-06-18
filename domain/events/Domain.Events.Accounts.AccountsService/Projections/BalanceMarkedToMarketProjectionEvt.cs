using NodaTime;
using QuantInfra.Sdk.Accounting;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Projections;

public record BalanceMarkedToMarketProjectionEvt(
    long EventId,
    int AccountId,
    BalanceValue Value,
    Instant ReferenceDt,
    Instant Timestamp
) : IAccountProjectionUpdatedEvt;