using NodaTime;

namespace QuantInfra.Domain.Events.Accounts.External;

/// <summary>
/// Used by trading connectors to indicate that they have restored the connection to the trading venue and that
/// missing trades must be retrieved
/// </summary>
public record ExternalAccountConnectionRestoredEvt(int AccountId, Instant Timestamp) : IExternalAccountEvent
{
    public long EventId { get; } = 0;
}