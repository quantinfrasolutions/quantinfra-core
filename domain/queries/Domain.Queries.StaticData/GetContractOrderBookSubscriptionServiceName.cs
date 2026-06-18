using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Domain.Queries.StaticData;

public record GetContractOrderBookSubscriptionServiceName(int ContractId) : IQuery<string?>;