using System.Collections.Generic;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Domain.Queries.StaticData;

public record GetConversionPath(int FromCurrencyId, int ToCurrencyId) : IQuery<IReadOnlyCollection<FxConversionStep>>;