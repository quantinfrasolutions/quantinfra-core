namespace QuantInfra.Common.MarketData.Infrastructure;

public interface IMarketDataClient<TSubscriptionRequest, TSubscription>
{
    IReadOnlyCollection<TSubscription> GetActiveSubscriptions();
    Task Subscribe(TSubscriptionRequest request);
    Task Unsubscribe(int requestId);
}