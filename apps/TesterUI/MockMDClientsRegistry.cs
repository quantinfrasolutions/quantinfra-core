using QuantInfra.Common.MarketData.Infrastructure;

namespace QuantInfra.Core.Apps.TesterUI;

public class MockMDClientsRegistry<TRequest, TSubscription> : IMarketDataClientsRegistry<TRequest, TSubscription>
{
    public IReadOnlyCollection<string> GetAvailableClients()
    {
        throw new NotImplementedException();
    }

    public IMarketDataClient<TRequest, TSubscription>? GetMarketDataClient(string clientName)
    {
        throw new NotImplementedException();
    }
}