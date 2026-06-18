using QuantInfra.Common.Interfaces.Api.StaticData;
using QuantInfra.Sdk.StaticData;
using UI.Interfaces.StaticData;

namespace UI.ApiWrapper;

public partial class ApiRepository : IUiCurrenciesRepository
{
    public Task<IEnumerable<Currency>> GetCurrencies(CurrencyFilter? filter = null) =>
        RetrieveCollection("currencies", () => _wrapper.Client.GetCurrenciesAsync(filter?.Id, filter?.Name, filter?.Limit, filter?.Offset));

    public Task CreateCurrency(Currency currency)
    {
        throw new NotImplementedException();
    }

    public Task UpdateCurrency(Currency currency)
    {
        throw new NotImplementedException();
    }

    public Task DeleteCurrency(long id)
    {
        throw new NotImplementedException();
    }
}