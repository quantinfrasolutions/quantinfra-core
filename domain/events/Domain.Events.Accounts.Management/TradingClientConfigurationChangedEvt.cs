using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Domain.Events.Accounts.Management;

public record TradingClientConfigurationChangedEvt(
    long EventId,
    int AccountId,
    TradingClientConfig? Config,
    Instant Timestamp
) : IEvent;