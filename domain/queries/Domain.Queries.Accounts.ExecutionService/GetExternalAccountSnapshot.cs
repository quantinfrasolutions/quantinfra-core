using System.Text.Json.Serialization;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.Accounts.ExternalAccounts;

namespace QuantInfra.Domain.Queries.Accounts.ExecutionService;

public record GetExternalAccountSnapshot(Guid RequestId, int AccountId, IReadOnlyDictionary<string, Instant>? LastReceivedTradeDts, Instant? LastReceivedBalanceOperationDt)
    : IAsyncQuery<ExternalAccountFullSnapshot?>
{
    [JsonConstructor]
    public GetExternalAccountSnapshot(int accountId, IReadOnlyDictionary<string, Instant>? lastReceivedTradeDts, Instant? lastReceivedBalanceOperationDt) 
        : this(Guid.NewGuid(), accountId, lastReceivedTradeDts, lastReceivedBalanceOperationDt) { }
}