using System;
using System.Text.Json.Serialization;
using Domain.Queries.Accounts.AccountsService;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Interfaces.Api.Accounts;

namespace QuantInfra.Domain.Queries.Accounts.AccountsService;

public record GetBrokerAccountReconciliationStatus(
    Guid RequestId,
    int AccountId,
    string AccountServiceName
) : IAsyncQuery<BrokerAccountReconciliationStatus?>, IAccountServiceAsyncQuery
{
    [JsonConstructor] public GetBrokerAccountReconciliationStatus(int accountId, string accountServiceName) 
        : this(Guid.NewGuid(), accountId, accountServiceName) { }
}