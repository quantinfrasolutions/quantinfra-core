using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuantInfra.Connectors.Binance.StaticDataClient.Internal;

internal sealed class ExchangeInfoResponse
{
    [JsonPropertyName("serverTime")]
    public long? ServerTime { get; init; }

    [JsonPropertyName("assets")]
    public List<ExchangeAsset> Assets { get; init; } = [];

    [JsonPropertyName("symbols")]
    public List<ExchangeSymbol> Symbols { get; init; } = [];
}

internal sealed class ExchangeAsset
{
    [JsonPropertyName("asset")]
    public string? Asset { get; init; }

    [JsonPropertyName("marginAvailable")]
    public bool? MarginAvailable { get; init; }

    [JsonPropertyName("autoAssetExchange")]
    public string? AutoAssetExchange { get; init; }
}

internal sealed class ExchangeSymbol
{
    [JsonPropertyName("symbol")] public string? Symbol { get; init; }
    [JsonPropertyName("pair")] public string? Pair { get; init; }
    [JsonPropertyName("status")] public string? Status { get; init; }
    [JsonPropertyName("contractStatus")] public string? ContractStatus { get; init; }
    [JsonPropertyName("contractType")] public string? ContractType { get; init; }
    [JsonPropertyName("baseAsset")] public string? BaseAsset { get; init; }
    [JsonPropertyName("quoteAsset")] public string? QuoteAsset { get; init; }
    [JsonPropertyName("marginAsset")] public string? MarginAsset { get; init; }
    [JsonPropertyName("settleAsset")] public string? SettleAsset { get; init; }
    [JsonPropertyName("deliveryDate")] public long? DeliveryDate { get; init; }
    [JsonPropertyName("onboardDate")] public long? OnboardDate { get; init; }
    [JsonPropertyName("contractSize")] public decimal? ContractSize { get; init; }
    [JsonPropertyName("pricePrecision")] public int? PricePrecision { get; init; }
    [JsonPropertyName("quantityPrecision")] public int? QuantityPrecision { get; init; }
    [JsonPropertyName("baseAssetPrecision")] public int? BaseAssetPrecision { get; init; }
    [JsonPropertyName("quoteAssetPrecision")] public int? QuoteAssetPrecision { get; init; }
    [JsonPropertyName("quotePrecision")] public int? QuotePrecision { get; init; }
    [JsonPropertyName("underlyingType")] public string? UnderlyingType { get; init; }
    [JsonPropertyName("underlyingSubType")] public List<string> UnderlyingSubTypes { get; init; } = [];
    [JsonPropertyName("orderTypes")] public List<string> OrderTypes { get; init; } = [];
    [JsonPropertyName("timeInForce")] public List<string> TimeInForce { get; init; } = [];
    [JsonPropertyName("filters")] public List<ExchangeFilter> Filters { get; init; } = [];
}

internal sealed class ExchangeFilter
{
    [JsonPropertyName("filterType")]
    public string? FilterType { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Values { get; init; } = [];
}
