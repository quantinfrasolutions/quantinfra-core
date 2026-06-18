using Common.StaticData.Abstractions;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Domain.StaticData.QueryHandlers;

public class GetFxConversionPathQueryHandler(IStaticDataProvider sdProvider) : IQueryHandler<GetConversionPath, IReadOnlyCollection<FxConversionStep>>
{
    public IReadOnlyCollection<FxConversionStep> Handle(GetConversionPath query)
    {
        var (contractId, isDirect) = sdProvider.GetFxConversionContract(query.FromCurrencyId, query.ToCurrencyId);
        return new List<FxConversionStep> { new() { ContractId = contractId, IsDirect = isDirect } };
    }
}