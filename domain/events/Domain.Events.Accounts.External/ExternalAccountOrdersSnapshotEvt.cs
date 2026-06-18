using NodaTime;
using QuantInfra.Sdk.Accounts.ExternalAccounts;

namespace QuantInfra.Domain.Events.Accounts.External;

public record ExternalAccountOrdersSnapshotEvt(
    int AccountId,
    ExternalAccountOrdersSnapshot? Snapshot,
    bool SuccessfulRetrieval,
    Instant Timestamp
) : IExternalAccountEvent
{
    public long EventId { get; } = 0;
}