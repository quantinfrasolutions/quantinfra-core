using QuantInfra.Connectors.Binance.Common;
using UI.Interfaces;
using UI.Interfaces.Binance;

namespace QuantInfra.UI.ApiWrapper.Backtesting;

public partial class ApiRepository : IUiBinanceMarketDataClient
{
    public Task<IEnumerable<string>> GetMarketDataClientsAsync() =>
        RetrieveCollection("market data clients", () => _wrapper.Client.GetUsdmMarketDataClientsAsync());

    public Task<IEnumerable<string>> GetOrderBookClientsAsync() =>
        RetrieveCollection("market data clients", () => _wrapper.Client.GetUsdmOrderBookClientsAsync());

    public Task<IEnumerable<BinanceUsdmMarketDataSubscriptionView>> GetActiveSubscriptionsAsync(EmptyFilter filter) =>
        RetrieveCollection("market data subscriptions", () => _wrapper.Client.GetBinanceUsdmMarketDataSubscriptionsAsync());

    public Task CreateSubscriptionAsync(BinanceMarket market, string clientName, BinanceUsdmMarketDataSubscriptionRequest request) =>
        Call(
            "Subscription created", "Failed to create subscription",
            () => _wrapper.Client.CreateBinanceUsdmMarketDataSubscriptionAsync(clientName, request) // TODO: market
        );

    public Task DeleteSubscriptionAsync(BinanceMarket market, string clientName, int subscriptionId) =>
        Call(
            "Subscription deleted", "Failed to delete subscription",
            () => _wrapper.Client.DeleteBinanceUsdmMarketDataSubscriptionAsync(clientName, subscriptionId)
        );
}