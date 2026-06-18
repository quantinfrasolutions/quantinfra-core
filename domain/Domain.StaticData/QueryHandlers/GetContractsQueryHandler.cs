using Common.StaticData.Abstractions;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Domain.StaticData.QueryHandlers;

public class GetContractsQueryHandler : IQueryHandler<GetContracts, IReadOnlyCollection<Contract>>
{
    private readonly IStaticDataProvider _sdProvider;

    public GetContractsQueryHandler(IStaticDataProvider sdProvider)
    {
        _sdProvider = sdProvider;
    }

    public IReadOnlyCollection<Contract> Handle(GetContracts query) => 
        _sdProvider.GetContracts(query.ContractIds);

    public Task<IReadOnlyCollection<Contract>> HandleAsync(GetContracts query) =>
        Task.Run(() => Handle(query));
}