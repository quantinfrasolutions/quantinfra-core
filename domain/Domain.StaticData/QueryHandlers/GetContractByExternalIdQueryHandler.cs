using Common.StaticData.Abstractions;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Domain.StaticData.QueryHandlers;

public class GetContractByExternalIdQueryHandler : IQueryHandler<GetContractByExternalId, Contract?>
{
    private IStaticDataProvider _sdProvider;

    public GetContractByExternalIdQueryHandler(IStaticDataProvider sdProvider)
    {
        _sdProvider = sdProvider;
    }

    public Contract? Handle(GetContractByExternalId query) =>
        _sdProvider.GetContractByExternalId(query.BrokerId, query.ExternalId);

    public Task<Contract?> HandleAsync(GetContractByExternalId query) =>
        Task.Run(() => Handle(query));
}