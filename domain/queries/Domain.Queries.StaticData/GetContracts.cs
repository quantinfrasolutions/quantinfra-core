using System.Collections.Generic;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Domain.Queries.StaticData;

public record GetContracts(IReadOnlyCollection<int> ContractIds) : IQuery<IReadOnlyCollection<Contract>>;