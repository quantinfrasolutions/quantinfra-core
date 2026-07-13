using QuantInfra.Connectors.Binance.Common;

namespace QuantInfra.Connectors.Binance.StaticDataClient;

public sealed class BinanceStaticDataClientOptions
{
    /// <summary>Used by overloads that do not explicitly specify a market.</summary>
    public BinanceMarket DefaultMarket { get; init; } = BinanceMarket.UsdmFutures;

    public Uri SpotBaseAddress { get; init; } = new("https://api.binance.com/");
    public Uri UsdmFuturesBaseAddress { get; init; } = new("https://fapi.binance.com/");
    public Uri CoinmFuturesBaseAddress { get; init; } = new("https://dapi.binance.com/");

    internal Uri GetExchangeInfoUri(BinanceMarket market)
    {
        var (baseAddress, path) = market switch
        {
            BinanceMarket.Spot => (SpotBaseAddress, "api/v3/exchangeInfo"),
            BinanceMarket.UsdmFutures => (UsdmFuturesBaseAddress, "fapi/v1/exchangeInfo"),
            BinanceMarket.CoinmFutures => (CoinmFuturesBaseAddress, "dapi/v1/exchangeInfo"),
            _ => throw new ArgumentOutOfRangeException(nameof(market), market, "Unsupported Binance market")
        };

        if (!baseAddress.IsAbsoluteUri)
            throw new InvalidOperationException($"The base address for {market} must be absolute.");

        return new Uri(baseAddress, path);
    }
}
