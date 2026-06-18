using QuantInfra.Common.Interfaces.Api.StaticData;
using QuantInfra.Sdk.StaticData;

namespace UI.Interfaces.StaticData;

public interface IUiCurrenciesRepository
{
    public Task<IEnumerable<Currency>> GetCurrencies(CurrencyFilter? filter = null);
    public Task CreateCurrency(Currency currency);
    public Task UpdateCurrency(Currency currency);
    public Task DeleteCurrency(long id);
}