using QuantInfra.Common.Interfaces.Api;
using QuantInfra.Sdk.StaticData;
using UI.Interfaces.StaticData;

namespace UI.ApiWrapper;

public partial class ApiRepository : IUiExchangesRepository
{
    public Task<IEnumerable<Exchange>> GetExchanges(PagingFilter filter) =>
        RetrieveCollection("exchanges", () => _wrapper.Client.GetExchangesAsync());

    public Task CreateExchange(Exchange exchange)
    {
        throw new NotImplementedException();
    }

    public Task DeleteExchange(long id)
    {
        throw new NotImplementedException();
    }
}