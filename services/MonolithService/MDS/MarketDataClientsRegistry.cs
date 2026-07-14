using QuantInfra.Common.MarketData.Infrastructure;

namespace QuantInfra.Services.MonolithService.MDS;

public class MarketDataClientsRegistry<TRequest, TSubscription> : IMarketDataClientsRegistry<TRequest, TSubscription>
{
    private readonly Dictionary<string, Func<IMarketDataClient<TRequest, TSubscription>?>> _mdsRetrievalFuncs = new();
    
    internal void AddClient(string name, Func<IMarketDataClient<TRequest, TSubscription>?> client)
    {
        _mdsRetrievalFuncs.Add(name, client);
    }
    
    public IReadOnlyCollection<string> GetAvailableClients() => _mdsRetrievalFuncs.Keys.ToList();

    public IMarketDataClient<TRequest, TSubscription>? GetMarketDataClient(string clientName) => 
        _mdsRetrievalFuncs[clientName]();
}