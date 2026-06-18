using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Domain.Queries.Accounts.AccountsService;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Domain.Queries.Accounts.AccountsService;

public record GetActiveOrders(
    Guid RequestId,
    int AccountId,
    string AccountServiceName
) : IAsyncQuery<IReadOnlyCollection<OrderStatus>>, IAccountServiceAsyncQuery
{
    [JsonConstructor] public GetActiveOrders(int accountId, string accountServiceName) 
        : this(Guid.NewGuid(), accountId, accountServiceName) { }
}