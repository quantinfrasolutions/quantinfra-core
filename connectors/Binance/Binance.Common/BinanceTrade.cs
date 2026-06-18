using System.Text.Json.Serialization;

namespace QuantInfra.Connectors.Binance.Common;

public class BinanceTrade
{
    [JsonPropertyName("buyer")] public bool Buyer { get; set; }
    [JsonPropertyName("commission")] public decimal Commission { get; set; }
    [JsonPropertyName("commissionAsset")] public string CommissionAsset { get; set; }
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("maker")] public bool Maker { get; set; }
    [JsonPropertyName("orderId")] public long OrderId { get; set; }
    [JsonPropertyName("price")] public decimal Price { get; set; }
    [JsonPropertyName("qty")] public decimal Qty { get; set; }
    [JsonPropertyName("quoteQty")] public decimal QuoteQty { get; set; }
    [JsonPropertyName("realizedPnl")] public decimal RealizedPnl { get; set; }
    [JsonPropertyName("side")] public string Side { get; set; }
    [JsonPropertyName("positionSide")] public string PositionSide { get; set; }
    [JsonPropertyName("symbol")] public string Symbol { get; set; }
    [JsonPropertyName("time")] public long Time { get; set; }
}