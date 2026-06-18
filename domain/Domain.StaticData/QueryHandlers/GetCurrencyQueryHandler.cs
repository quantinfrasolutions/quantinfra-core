using Common.StaticData.Abstractions;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Domain.StaticData.QueryHandlers;

public class GetCurrencyQueryHandler(IStaticDataProvider sdProvider) : IQueryHandler<GetCurrency, Currency?>
{
    public Currency? Handle(GetCurrency query) => sdProvider.GetCurrency(query.CurrencyId);
}