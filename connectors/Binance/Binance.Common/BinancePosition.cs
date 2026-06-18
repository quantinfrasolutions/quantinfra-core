using System.Text.Json.Serialization;

namespace QuantInfra.Connectors.Binance.Common;

public class BinancePosition
{
    [JsonPropertyName("symbol")] public string Symbol { get; set; }
    [JsonPropertyName("positionSide")] public string PositionSide { get; set; }
    [JsonPropertyName("positionAmt")] public decimal PositionAmt { get; set; }
    [JsonPropertyName("entryPrice")] public decimal EntryPrice { get; set; }
    [JsonPropertyName("breakEvenPrice")] public decimal BreakEvenPrice { get; set; }
    [JsonPropertyName("markPrice")] public decimal MarkPrice { get; set; }
    [JsonPropertyName("unrealizedProfit")] public decimal UnrealizedProfit { get; set; }
    [JsonPropertyName("liquidationPrice")] public decimal LiquidationPrice { get; set; }
    [JsonPropertyName("isolatedMargin")] public decimal IsolatedMargin { get; set; }
    [JsonPropertyName("notional")] public decimal Notional { get; set; }
    [JsonPropertyName("marginAsset")] public string MarginAsset { get; set; }
    [JsonPropertyName("isolatedWallet")] public decimal IsolatedWallet { get; set; }
    [JsonPropertyName("initialMargin")] public decimal InitialMargin { get; set; }
    [JsonPropertyName("maintMargin")] public decimal MaintMargin { get; set; }
    [JsonPropertyName("positionInitialMargin")] public decimal PositionInitialMargin { get; set; }
    [JsonPropertyName("openOrderInitialMargin")] public decimal OpenOrderInitialMargin { get; set; }
    [JsonPropertyName("adl")] public decimal Adl { get; set; }
    [JsonPropertyName("bidNotional")] public decimal BidNotional { get; set; }
    [JsonPropertyName("askNotional")] public decimal AskNotional { get; set; }
    [JsonPropertyName("updateTime")] public long UpdateTime { get; set; }
}