using Common.StaticData.Abstractions;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Domain.StaticData.QueryHandlers;

public class GetContractQueryHandler : IQueryHandler<GetContract, Contract?>
{
    private readonly IStaticDataProvider _sdProvider;

    public GetContractQueryHandler(IStaticDataProvider sdProvider)
    {
        _sdProvider = sdProvider;
    }

    public Contract? Handle(GetContract query) =>
        _sdProvider.GetContract(query.ContractId);

    public Task<Contract?> HandleAsync(GetContract query) =>
        Task.Run(() => Handle(query));
}