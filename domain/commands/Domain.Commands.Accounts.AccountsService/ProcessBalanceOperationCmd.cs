using System;
using System.Text.Json.Serialization;
using QuantInfra.Sdk.Accounting;

namespace QuantInfra.Domain.Commands.Accounts.AccountsService;

public record ProcessBalanceOperationCmd(
    string AccountServiceName,
    NewBalanceOperation BalanceOperation,
    Guid RequestId
) : IAccountsServiceCmd
{
    [JsonConstructor]
    public ProcessBalanceOperationCmd(string accountServiceName, NewBalanceOperation BalanceOperation) : 
        this(accountServiceName, BalanceOperation, Guid.NewGuid()) { }
}