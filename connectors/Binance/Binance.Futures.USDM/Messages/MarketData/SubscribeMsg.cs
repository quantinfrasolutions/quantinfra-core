using System.Text.Json.Serialization;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Messages.MarketData;

internal class SubscribeMsg
{
    [JsonPropertyName("method")] public string Method => "SUBSCRIBE";
    [JsonPropertyName("params")] public List<string> Params { get; private set; }
    [JsonPropertyName("id")] public int RequestId { get; private set; }

    public static SubscribeMsg CreateForSingleStream(string stream, int requestId) => new()
    {
        Params = new List<string> { stream },
        RequestId = requestId,
    };
}