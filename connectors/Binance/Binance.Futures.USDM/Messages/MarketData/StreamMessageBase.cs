using System.Text.Json.Serialization;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Messages.MarketData;

public class StreamMessageBase
{
    [JsonPropertyName("stream")] public string Stream { get; set; }
}