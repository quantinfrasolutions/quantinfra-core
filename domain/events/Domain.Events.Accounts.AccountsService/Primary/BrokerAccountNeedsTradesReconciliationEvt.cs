using System.Collections.Generic;
using NodaTime;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Primary;

public record BrokerAccountNeedsTradesReconciliationEvt(
    long EventId,
    int AccountId,
    IReadOnlyDictionary<string, Instant> LastReceivedTradesDts,
    Instant LastReceivedBalanceOperationsDt,
    long Version,
    Instant Timestamp
) : IAccountEventBase;