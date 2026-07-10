using System;
using System.Text.Json.Serialization;
using NodaTime;

namespace QuantInfra.Domain.Commands.Accounts.AccountsService;

public record ClearBrokerAccountReconciliationStatus(
    string AccountServiceName,
    int AccountId,
    Instant ReferenceDt,
    Guid RequestId
) : IAccountsServiceCmd
{
    [JsonConstructor]
    public ClearBrokerAccountReconciliationStatus(string accountServiceName, int accountId, Instant referenceDt) : 
        this(accountServiceName, accountId, referenceDt, Guid.NewGuid()) { }
}