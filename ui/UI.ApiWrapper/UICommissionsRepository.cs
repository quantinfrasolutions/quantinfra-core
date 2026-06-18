using QuantInfra.Common.Interfaces.Api.StaticData;
using QuantInfra.Sdk.StaticData;
using UI.Interfaces.StaticData;

namespace UI.ApiWrapper;

public partial class ApiRepository : IUiCommissionsRepository
{
    public Task<IEnumerable<CommissionStructure>> GetCommissions(CommissionsFilter filter) =>
        RetrieveCollection("commissions", () => _wrapper.Client.GetCommissionStructuresAsync(filter.CommissionId,
            filter.Name, filter.CurrencyId, filter.Type.ToString(), filter.BrokerId, filter.ExchangeId, filter.Limit, filter.Offset));

    public Task CreateCommission(CommissionStructure commission)
    {
        throw new NotImplementedException();
    }

    public Task DeleteCommission(int id)
    {
        throw new NotImplementedException();
    }

    public Task UpdateContractCommissions(int contractId, IEnumerable<int> add, IEnumerable<int> remove)
    {
        throw new NotImplementedException();
    }

    public Task UpdateCommission(CommissionStructure cs)
    {
        throw new NotImplementedException();
    }
}