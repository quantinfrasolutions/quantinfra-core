using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Domain.Queries.StaticData;

public record GetContractByExternalId(int BrokerId, string ExternalId) : IQuery<Contract?>;