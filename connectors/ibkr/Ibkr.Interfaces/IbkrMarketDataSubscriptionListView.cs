namespace QuantInfra.Connectors.Ibkr.Interfaces;

public class IbkrMarketDataSubscriptionListView : IbkrMarketDataSubscription
{
    public IbkrMarketDataSubscriptionListView() { }

    public IbkrMarketDataSubscriptionListView(IbkrMarketDataSubscription s, QuantInfra.Sdk.StaticData.Stream? stream) : base(s)
    {
        StreamName = stream?.Ticker;
    }
    
    public string? StreamName { get; set; }
}