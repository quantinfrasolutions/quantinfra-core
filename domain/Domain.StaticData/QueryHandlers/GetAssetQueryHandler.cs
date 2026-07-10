using Common.StaticData.Abstractions;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Domain.StaticData.QueryHandlers;

public class GetAssetQueryHandler(IStaticDataProvider sdProvider) : IQueryHandler<GetAsset, Asset?>
{
    public Asset? Handle(GetAsset query) => sdProvider.GetAsset(query.AssetId);
}