namespace QuantInfra.Common.MarketData.Infrastructure;

public interface IMarketDataClientsRegistry<TRequest, TSubscription>
{
    IReadOnlyCollection<string> GetAvailableClients();
    IMarketDataClient<TRequest, TSubscription>? GetMarketDataClient(string clientName);
}