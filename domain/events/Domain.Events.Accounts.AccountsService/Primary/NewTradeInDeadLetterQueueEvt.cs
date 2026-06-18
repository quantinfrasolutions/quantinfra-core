using NodaTime;
using QuantInfra.Sdk.Trading.ExternalAccounts;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Primary;

public record NewTradeInDeadLetterQueueEvt(
    long EventId,
    int AccountId,
    ExternalTradeRecord Trade,
    long Version,
    Instant Timestamp
) : IAccountEventBase;