using System.Text.Json.Serialization;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Messages.UserStreams;

public class BaseStreamMessage
{
    [JsonPropertyName("e")] public string EventType { get; set; }
    [JsonPropertyName("E")] public long EventTs { get; set; }
}