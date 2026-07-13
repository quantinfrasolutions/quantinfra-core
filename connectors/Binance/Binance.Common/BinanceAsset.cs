using QuantInfra.Connectors.Binance.Common;

namespace QuantInfra.Connectors.Binance.StaticDataClient.Models;

/// <summary>An asset referenced by an exchange-info response.</summary>
public sealed record BinanceAsset(
    string Symbol,
    BinanceMarket Market,
    bool IsBaseAsset,
    bool IsQuoteAsset,
    bool IsMarginAsset,
    bool? IsMarginAvailable,
    decimal? AutoAssetExchange);
