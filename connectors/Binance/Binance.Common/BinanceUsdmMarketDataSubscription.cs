using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Connectors.Binance.Common;

public class BinanceUsdmMarketDataSubscription : IMarketDataSubscription
{
    public BinanceUsdmMarketDataSubscription() { }

    public BinanceUsdmMarketDataSubscription(int subscriptionId, int? streamId, SubscriptionType subscriptionType, string symbol)
    {
        SubscriptionId = subscriptionId;
        StreamId = streamId;
        SubscriptionType = subscriptionType;
        Symbol = symbol;
    }
    
    public BinanceUsdmMarketDataSubscription(BinanceUsdmMarketDataSubscription s) : this(s.SubscriptionId, s.StreamId, s.SubscriptionType, s.Symbol) { }

    public int SubscriptionId { get; init; }
    public int? StreamId { get; set; }
    public SubscriptionType SubscriptionType { get; init; }
    public string Symbol { get; init; }
    public ExchangeBar LastBar { get; set; }
    public string ClientName { get; init; }
    

    public override string ToString()
    {
        return $"{{ BinanceUsdmMarketDataSubscription | {nameof(SubscriptionId)}: {SubscriptionId}, {nameof(StreamId)}: {StreamId}, {nameof(SubscriptionType)}: {SubscriptionType}, {nameof(Symbol)}: {Symbol} }}";
    }
}