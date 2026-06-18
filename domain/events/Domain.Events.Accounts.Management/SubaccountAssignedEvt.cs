using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Domain.Events.Accounts.Management;

public record SubaccountAssignedEvt(
    long EventId,
    string AccountServiceName,
    int AccountId,
    Subaccount Subaccount,
    Instant Timestamp
) : IEvent
{ }