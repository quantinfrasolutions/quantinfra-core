using System;
using System.Text.Json.Serialization;
using NodaTime;

namespace QuantInfra.Domain.Commands.Accounts.AccountsService;

public record RunEndOfDayCmd(
    string AccountServiceName,
    Instant ReferenceDt,
    Guid RequestId
) : IAccountsServiceCmd
{
    [JsonConstructor]
    public RunEndOfDayCmd(string accountServiceName, Instant referenceDt) : 
        this(accountServiceName, referenceDt, Guid.NewGuid()) { }
}