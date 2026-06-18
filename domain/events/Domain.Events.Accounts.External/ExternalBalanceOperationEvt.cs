using NodaTime;
using QuantInfra.Sdk.Trading.ExternalAccounts;

namespace QuantInfra.Domain.Events.Accounts.External;

public record ExternalBalanceOperationEvt(int AccountId, ExternalBalanceOperation BalanceOperation, Instant Timestamp) : IExternalAccountEvent
{
    public long EventId { get; } = 0;
}