using System.Text.Json.Serialization;
using Binance.Futures.USDM;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Messages.Commands;

public abstract class OrderCommand
{
    [JsonPropertyName("id")]
    public string RequestId { get; }

    [JsonPropertyName("method")]
    public string Method { get; }

    [JsonPropertyName("params")]
    public SortedDictionary<string, object> Params { get; }

    protected OrderCommand(string method, SortedDictionary<string, object> parameters, string apiSecret)
    {
        RequestId = Guid.NewGuid().ToString();
        Method = method;
        Params = parameters;
        Params.Add("signature", Params.BuildSignaturePayload().GetHmacSha256Signature(apiSecret));
    }
}
