using QuantInfra.Sdk.Accounting;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Projections;

public record SharePriceHistoryProjectionEvt(long EventId, int AccountId, SharePriceHistory SharePrice) : IAccountProjectionUpdatedEvt;