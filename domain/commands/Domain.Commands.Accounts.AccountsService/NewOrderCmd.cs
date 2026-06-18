using System;
using System.Text.Json.Serialization;
using NodaTime;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Domain.Commands.Accounts.AccountsService;

public record NewOrderCmd(
    string AccountServiceName,
    NewOrderSingle Order,
    Instant? ReferenceDt,
    Guid RequestId
) : IAccountsServiceCmd
{
    [JsonConstructor]
    public NewOrderCmd(string accountServiceName, NewOrderSingle order, Instant? referenceDt = null) : 
        this(accountServiceName, order, referenceDt, Guid.NewGuid()) { }
}