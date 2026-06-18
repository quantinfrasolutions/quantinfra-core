using NodaTime;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Primary;

public record BrokerAccountNeedsOrdersReconciliationEvt(
    long EventId,
    int AccountId,
    long Version,
    Instant Timestamp
) : IAccountEventBase;