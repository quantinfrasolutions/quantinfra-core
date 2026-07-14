using QuantInfra.Connectors.Binance.Common;

namespace UI.Interfaces.Binance;

public interface IUiBinanceMarketDataClient
{
    Task<IEnumerable<string>> GetMarketDataClientsAsync();
    Task<IEnumerable<string>> GetOrderBookClientsAsync();
    
    Task<IEnumerable<BinanceUsdmMarketDataSubscriptionView>> GetActiveSubscriptionsAsync(EmptyFilter filter);
    Task CreateSubscriptionAsync(BinanceMarket market, string clientName, BinanceUsdmMarketDataSubscriptionRequest request);
    Task DeleteSubscriptionAsync(BinanceMarket market, string clientName, int subscriptionId);
    
    Task<IEnumerable<BinanceUsdmOrderBookSubscriptionListView>> GetActiveOrderBookSubscriptionsAsync(EmptyFilter filter);
    Task CreateOrderBookSubscriptionAsync(BinanceMarket market, string clientName, BinanceUsdmOrderBookSubscriptionRequest request);
    Task DeleteOrderBookSubscriptionAsync(BinanceMarket market, string clientName, int subscriptionId);
}