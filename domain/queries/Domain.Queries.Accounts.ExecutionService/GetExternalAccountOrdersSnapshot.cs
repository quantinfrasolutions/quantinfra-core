using System.Text.Json.Serialization;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.Accounts.ExternalAccounts;

namespace QuantInfra.Domain.Queries.Accounts.ExecutionService;

public record GetExternalAccountOrdersSnapshot(Guid RequestId, int AccountId, bool UseMulticast = false)
    : IAsyncQueryWithMulticast<ExternalAccountOrdersSnapshot?>
{
    [JsonConstructor]
    public GetExternalAccountOrdersSnapshot(int accountId, bool useMulticast = false) : this(Guid.NewGuid(), accountId, useMulticast) { }
}