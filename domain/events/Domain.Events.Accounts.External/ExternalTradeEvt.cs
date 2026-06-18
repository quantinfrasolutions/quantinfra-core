using NodaTime;
using QuantInfra.Sdk.Trading.ExternalAccounts;

namespace QuantInfra.Domain.Events.Accounts.External;

public record ExternalTradeEvt(int AccountId, ExternalTradeRecord Trade, Instant Timestamp) : IExternalAccountEvent
{
    public long EventId { get; } = 0;
}