using System.Text.Json.Serialization;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Messages.MarketData;

public class StreamDataBase
{
    [JsonPropertyName("e")] public string EventType { get; set; }
    [JsonPropertyName("E")] public long Timestamp { get; set; }
    [JsonPropertyName("s")] public string Symbol { get; set; }
}