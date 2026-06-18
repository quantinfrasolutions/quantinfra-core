using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Domain.Queries.MarketData;

public record GetLastKnownContractPrice(int ContractId) : IQuery<decimal?>;