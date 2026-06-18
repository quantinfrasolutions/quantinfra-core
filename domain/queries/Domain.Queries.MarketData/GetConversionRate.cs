using QuantInfra.Common.EventSourcing;

namespace QuantInfra.Domain.Queries.MarketData;

public record GetConversionRate(int FromCcy, int ToCcy) : IQuery<decimal?>;