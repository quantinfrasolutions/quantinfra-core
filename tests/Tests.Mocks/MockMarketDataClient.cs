using QuantInfra.Common.MarketData.Abstractions;

namespace QuantInfra.Tests.Mocks;

public class MockMarketDataClient : IMarketDataClient
{
    public Task RequestHistoricalBars(string exchangeSymbol, int count, int timeframe = 1)
    {
        throw new NotImplementedException();
    }

    public Task SubscribeToTicks(string exchangeSymbol)
    {
        throw new NotImplementedException();
    }

    public Task SubscribeToTicks(IEnumerable<string> exchangeSymbols)
    {
        throw new NotImplementedException();
    }

    public Task SubsribeToCandles1M(long streamId)
    {
        throw new NotImplementedException();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task SubscribeToLastContractPricesAsync()
    {
        throw new NotImplementedException();
    }

    public Task SubscribeToBestBidOffers(int contractId)
    {
        throw new NotImplementedException();
    }

    public Task SubscribeToOrderBook(int contractId, string mdsName)
    {
        throw new NotImplementedException();
    }
}