using QuantInfra.Common.Interfaces.Api;
using QuantInfra.Sdk.StaticData;

namespace UI.Interfaces.StaticData;

public interface IUiExchangesRepository
{
    public Task<IEnumerable<Exchange>> GetExchanges(PagingFilter filter);
    public Task CreateExchange(Exchange exchange);
    public Task DeleteExchange(long id);
    // Task<Dictionary<long, TradingSessionModel>> GetTradingSessions(long exchangeId, bool refresh = false);
    // Task CreateTradingSession(TradingSessionModel value);
    // Task UpdateContractTradingSessions(long contractId, IEnumerable<long> add, IEnumerable<long> remove);
}