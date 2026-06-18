using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Common.Trading.Positions;
using Domain.Queries.Accounts.AccountsService;
using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Domain.Queries.Accounts.AccountsService;

public record GetPositions(
    Guid RequestId,
    int AccountId,
    string AccountServiceName
) : IAsyncQuery<IReadOnlyCollection<Position>>, IAccountServiceAsyncQuery
{
    [JsonConstructor] public GetPositions(int accountId, string accountServiceName) 
        : this(Guid.NewGuid(), accountId, accountServiceName) { }
}