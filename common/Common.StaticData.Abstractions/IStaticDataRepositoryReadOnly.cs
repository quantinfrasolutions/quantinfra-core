using QuantInfra.Common.Interfaces.Api.StaticData;
using QuantInfra.Sdk.StaticData;

namespace Common.StaticData.Abstractions;

public interface IStaticDataRepositoryReadOnly
{
    Task<IReadOnlyCollection<ContractListView>> GetContractsAsync(ContractsFilter filter);
    Task<IReadOnlyCollection<Currency>> GetCurrenciesAsync(IEnumerable<int> currencyIds);
}