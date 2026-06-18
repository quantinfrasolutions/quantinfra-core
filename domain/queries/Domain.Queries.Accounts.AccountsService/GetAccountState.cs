using System;
using System.Text.Json.Serialization;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.Accounts.AccountStates;

namespace QuantInfra.Domain.Queries.Accounts.AccountsService;

public record GetAccountState(
    Guid RequestId,
    string AccountServiceName,
    int AccountId,
    bool UseMulticast = false
) : IAsyncQueryWithMulticast<AccountStateReadonly?>,
    IAsyncQueryWithMulticast<BrokerAccountStateReadonly?>
{
    [JsonConstructor] public GetAccountState(int accountId, string accountServiceName, bool useMulticast = false) : 
        this(Guid.NewGuid(), accountServiceName, accountId, useMulticast) { }
}

public record GetBrokerAccountState(
    Guid RequestId,
    string AccountServiceName,
    int AccountId,
    bool UseMulticast = false
) : IAsyncQueryWithMulticast<BrokerAccountStateReadonly?>
{
    [JsonConstructor] public GetBrokerAccountState(int accountId, string accountServiceName, bool useMulticast = false) : 
        this(Guid.NewGuid(), accountServiceName, accountId, useMulticast) { }
}