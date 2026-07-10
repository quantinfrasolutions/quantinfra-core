using NodaTime;

namespace QuantInfra.Domain.Events.Accounts.AccountsService.Primary;

public record NewUnmappedContractRegisteredEvt(
    long EventId,
    int AccountId,
    string? ExternalContractId,
    string? ExternalAssetId,
    long Version,
    Instant Timestamp
) : IAccountEventBase;