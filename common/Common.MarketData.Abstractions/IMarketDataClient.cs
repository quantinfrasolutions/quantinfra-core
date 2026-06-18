namespace QuantInfra.Common.MarketData.Abstractions
{
    public interface IMarketDataClient
    {
        Task RequestHistoricalBars(string exchangeSymbol, int count, int timeframe = 1);
        Task SubscribeToTicks(string exchangeSymbol);
        Task SubscribeToTicks(IEnumerable<string> exchangeSymbols);
        Task SubsribeToCandles1M(long streamId);
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
        Task SubscribeToLastContractPricesAsync();
        Task SubscribeToBestBidOffers(int contractId);
        Task SubscribeToOrderBook(int contractId, string mdsName);
    }
}
