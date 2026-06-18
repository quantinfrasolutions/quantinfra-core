using QuantInfra.Common.Interfaces.Api.StaticData;
using UI.Interfaces.StaticData;

namespace UI.ApiWrapper;

public partial class ApiRepository : IUiContractsRepository
{
    public Task<IEnumerable<ContractListView>> GetContracts(ContractsFilter? filter = null) =>
        RetrieveCollection("contracts",
            () => _wrapper.Client.GetContractsAsync(filter?.Ticker, filter?.ExchangeId, filter?.ContractIds,
                filter?.CommissionId, filter?.Limit, filter?.Offset));

    // public Task CreateContract(CreateContractDefinitionRequest contract, CreateContractTemplateRequest? template)
    // {
    //     throw new NotImplementedException();
    // }

    public Task DeleteContract(int id)
    {
        throw new NotImplementedException();
    }

    public Task<ContractListView> GetContract(long id)
    {
        throw new NotImplementedException();
    }
    
    public Task<IEnumerable<ContractTemplateListView>> GetContractTemplates(ContractTemplatesFilter? filter = null) =>
        RetrieveCollection("contract templates",
            () => _wrapper.Client.GetContractTemplatesAsync(filter?.TemplateId, filter?.Name, filter?.Limit, filter?.Offset)
        );
}