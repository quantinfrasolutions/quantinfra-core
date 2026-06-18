using System.Text.Json.Serialization;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Messages.MarketData;

public class UserSocketRequestMsg
{
    [JsonPropertyName("method")] public string Method { get; set; }
    [JsonPropertyName("params")] public object Params { get; set; }
    [JsonPropertyName("id")] public string RequestId { get; set; }
}