using System;
using System.Text.Json.Serialization;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Domain.Commands.Accounts.AccountsService;

public record ReplaceOrderCmd(
    string AccountServiceName,
    OrderReplaceRequest Ocr,
    Guid RequestId
) : IAccountsServiceCmd
{
    [JsonConstructor]
    public ReplaceOrderCmd(string accountServiceName, OrderReplaceRequest ocr) : 
        this(accountServiceName, ocr, Guid.NewGuid()) { }
}