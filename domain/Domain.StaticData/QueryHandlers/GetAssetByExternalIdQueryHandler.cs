using Common.StaticData.Abstractions;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Domain.StaticData.QueryHandlers;

public class GetAssetByExternalIdQueryHandler(IStaticDataProvider sdProvider)
    : IQueryHandler<GetAssetByExternalId, Asset?>
{
    public Asset? Handle(GetAssetByExternalId query) =>
        sdProvider.GetAssetByExternalId(query.BrokerId, query.ExternalId);
}