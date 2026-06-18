using QuantInfra.Common.Interfaces.Api.StaticData;

namespace UI.Interfaces.StaticData;

public interface IUiContractsRepository
{
    Task<IEnumerable<ContractListView>> GetContracts(ContractsFilter? filter = null);
    // Task CreateContract(CreateContractDefinitionRequest contract, CreateContractTemplateRequest? template);
    Task DeleteContract(int id);
    
    Task<IEnumerable<ContractTemplateListView>> GetContractTemplates(ContractTemplatesFilter? filter = null);
}