using QuantInfra.Sdk.MarketData;

namespace Binance.Common;

public class BinanceUsdmMarketDataSubscriptionRequest
{
    public string Symbol { get; set; }
    public int? StreamId { get; set; }
    public SubscriptionType SubscriptionType { get; set; }
}