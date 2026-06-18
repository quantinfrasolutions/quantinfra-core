using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Domain.Events.Accounts.Management;

public record AccountCreatedEvt(
    long EventId,
    int AccountId,
    AccountRecordV6 Account,
    Instant Timestamp
) : IEvent;