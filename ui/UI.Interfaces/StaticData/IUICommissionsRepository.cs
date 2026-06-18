using QuantInfra.Common.Interfaces.Api.StaticData;
using QuantInfra.Sdk.StaticData;

namespace UI.Interfaces.StaticData;

public interface IUiCommissionsRepository
{
    Task<IEnumerable<CommissionStructure>> GetCommissions(CommissionsFilter filter);
    Task CreateCommission(CommissionStructure commission);
    Task DeleteCommission(int id);
    Task UpdateContractCommissions(int contractId, IEnumerable<int> add, IEnumerable<int> remove);
    // Task<CommissionStructureView> GetCommission(long id);
    Task UpdateCommission(CommissionStructure cs);
}