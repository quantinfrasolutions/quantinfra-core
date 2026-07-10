using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Domain.Queries.Accounts.AccountsService;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.Trading.Positions;

namespace QuantInfra.Domain.Queries.Accounts.AccountsService;

public record GetPositions(
    Guid RequestId,
    int AccountId,
    string AccountServiceName,
    bool MarkToMarket
) : IAsyncQuery<IReadOnlyCollection<Position>>, IAccountServiceAsyncQuery
{
    [JsonConstructor] public GetPositions(int accountId, string accountServiceName, bool markToMarket) 
        : this(Guid.NewGuid(), accountId, accountServiceName, markToMarket) { }
}