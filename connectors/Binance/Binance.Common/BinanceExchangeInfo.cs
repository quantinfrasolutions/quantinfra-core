using QuantInfra.Connectors.Binance.Common;

namespace QuantInfra.Connectors.Binance.StaticDataClient.Models;

public sealed record BinanceExchangeInfo(
    BinanceMarket Market,
    DateTimeOffset? ServerTime,
    IReadOnlyList<BinanceAsset> Assets,
    IReadOnlyList<BinanceContract> Contracts);
