using System.Collections.Generic;
using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Domain.Queries.MarketData;

public record GetLastKnownContractPrices() : IQuery<IReadOnlyDictionary<int, decimal>>;