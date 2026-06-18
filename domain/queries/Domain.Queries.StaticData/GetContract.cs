using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Domain.Queries.StaticData;

public record GetContract(int ContractId) : IQuery<Contract?>;