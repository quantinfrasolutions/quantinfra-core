using System.Collections.Generic;
using NodaTime;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Trading.Positions;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Primary;

public record AccountEndOfDayEvt(
    long EventId,
    int AccountId,
    long Version,
    IReadOnlyDictionary<long, PositionValue> PositionValues,
    IReadOnlyDictionary<int, BalanceValue> BalanceValues,
    bool SuccessfulConversion,
    Instant ReferenceDt,
    Instant Timestamp) : IAccountEventBase;