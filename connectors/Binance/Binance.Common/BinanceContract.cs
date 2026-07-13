using QuantInfra.Connectors.Binance.Common;

namespace QuantInfra.Connectors.Binance.StaticDataClient.Models;

/// <summary>A normalized Binance spot symbol or futures contract.</summary>
public sealed record BinanceContract
{
    public required BinanceMarket Market { get; init; }
    public required string Symbol { get; init; }
    public required string BaseAsset { get; init; }
    public required string QuoteAsset { get; init; }
    public string? Pair { get; init; }
    public string? Status { get; init; }
    public string? ContractType { get; init; }
    public string? MarginAsset { get; init; }
    public string? SettlementAsset { get; init; }
    public DateTimeOffset? OnboardDate { get; init; }
    public DateTimeOffset? DeliveryDate { get; init; }
    public decimal? ContractSize { get; init; }
    public int? PricePrecision { get; init; }
    public int? QuantityPrecision { get; init; }
    public int? BaseAssetPrecision { get; init; }
    public int? QuoteAssetPrecision { get; init; }
    public string? UnderlyingType { get; init; }
    public IReadOnlyList<string> UnderlyingSubTypes { get; init; } = [];
    public IReadOnlyList<string> OrderTypes { get; init; } = [];
    public IReadOnlyList<string> TimeInForce { get; init; } = [];
    public IReadOnlyList<BinanceSymbolFilter> Filters { get; init; } = [];

    public bool IsTrading => string.Equals(Status, "TRADING", StringComparison.OrdinalIgnoreCase);

    public BinanceSymbolFilter? GetFilter(string filterType) =>
        Filters.FirstOrDefault(x => string.Equals(x.FilterType, filterType, StringComparison.OrdinalIgnoreCase));
}
