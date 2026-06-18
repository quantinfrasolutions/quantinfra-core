using NodaTime;

namespace QuantInfra.Domain.Events.Accounts.External;

/// <summary>
/// Used by trading connectors to indicate that they missed some messages from the Accounts Service and that
/// orders need to be reconciled
/// </summary>
public record ExecutionServiceMissedVersionEvt(int AccountId, Instant Timestamp) : IExternalAccountEvent
{
    public long EventId { get; } = 0;
}