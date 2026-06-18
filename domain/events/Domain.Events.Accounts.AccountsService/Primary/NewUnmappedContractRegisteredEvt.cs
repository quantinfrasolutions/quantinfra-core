using NodaTime;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Primary;

public record NewUnmappedContractRegisteredEvt(
    long EventId,
    int AccountId,
    string ExternalContractId,
    long Version,
    Instant Timestamp
) : IAccountEventBase;