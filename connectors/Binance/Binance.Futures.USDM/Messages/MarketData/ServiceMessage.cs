using System.Text.Json.Serialization;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Messages.MarketData;

public class ServiceMessage
{
    // {
    //     "result": null,
    //     "id": 1
    // }
    [JsonPropertyName("result")] public string? Result { get; set; }
    [JsonPropertyName("id")] public int RequestId { get; set; }
}