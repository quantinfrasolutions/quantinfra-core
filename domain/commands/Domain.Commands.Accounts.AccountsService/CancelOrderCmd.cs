using System;
using System.Text.Json.Serialization;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Domain.Commands.Accounts.AccountsService;

public record CancelOrderCmd(
    string AccountServiceName,
    OrderCancelRequest Ocr,
    Guid RequestId
) : IAccountsServiceCmd
{
    [JsonConstructor]
    public CancelOrderCmd(string accountServiceName, OrderCancelRequest ocr) : 
        this(accountServiceName, ocr, Guid.NewGuid()) { }
}