using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Domain.Queries.Accounts.AccountsService;
using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Domain.Queries.Accounts.AccountsService;

public record GetBalances(
    Guid RequestId,
    int AccountId,
    string AccountServiceName
) : IAsyncQuery<IReadOnlyDictionary<int, decimal>>, IAccountServiceAsyncQuery
{
    [JsonConstructor] public GetBalances(int accountId, string accountServiceName) 
        : this(Guid.NewGuid(), accountId, accountServiceName) { }
}