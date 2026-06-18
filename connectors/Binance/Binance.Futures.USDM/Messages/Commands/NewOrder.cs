using System.Globalization;
using System.Text.Json.Serialization;
using Binance.Futures.USDM;
using NodaTime;
using QuantInfra.Connectors.Binance.Common;
using QuantInfra.Sdk.Trading.ExternalAccounts;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Messages.Commands;

public class NewOrder
{
    [JsonPropertyName("id")] public string RequestId { get; init; }
    [JsonPropertyName("method")] public string Method => "order.place";
    [JsonPropertyName("params")] public SortedDictionary<string, object> Params { get; init; }

    public NewOrder(NewOrderSingleExternal nos, string apiKey, string apiSecret)
    {
        if (nos.OrdType != OrdType.Limit && nos.OrdType != OrdType.Market) throw new NotSupportedException("Only market and limit orders are supported");
        
        RequestId = nos.ClOrdId ?? Guid.NewGuid().ToString();
        Params = new();
        Params.Add("symbol", nos.ExternalContractId);	
        Params.Add("side", nos.Side.ToBinanceString());
        Params.Add("type", nos.OrdType.ToBinanceString());
        Params.Add("quantity", nos.OrderQty.ToString(CultureInfo.InvariantCulture));
        if (!string.IsNullOrEmpty(nos.ClOrdId)) Params.Add("newClientOrderId", nos.ClOrdId);
        Params.Add("apiKey", apiKey);
        Params.Add("timestamp", SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds());

        if (nos.OrdType == OrdType.Limit)
        {
            Params.Add("timeInForce", nos.TimeInForce.ToBinanceString());
            Params.Add("price", nos.Price!.ToString()!);
        }
        
        var signature = Params.BuildSignaturePayload().GetHmacSha256Signature(apiSecret);
        Params.Add("signature", signature);
    }
}