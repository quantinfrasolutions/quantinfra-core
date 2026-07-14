using System.Text.Json.Serialization;

namespace QuantInfra.Connectors.Binance.Common;

public class BinanceUsdmMarketDataSubscriptionListView : BinanceUsdmMarketDataSubscription
{
    [JsonConstructor] public BinanceUsdmMarketDataSubscriptionListView() { }
    public BinanceUsdmMarketDataSubscriptionListView(BinanceUsdmMarketDataSubscription s, QuantInfra.Sdk.StaticData.Stream? stream) : base(s)
    {
        StreamName = stream?.Ticker;
    }
    
    public string StreamName { get; init; }
}