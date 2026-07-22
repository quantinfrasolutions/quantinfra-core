using System.Globalization;
using NodaTime;
using QuantInfra.Connectors.Binance.Common;
using QuantInfra.Sdk.Trading.ExternalAccounts;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Messages.Commands;

public sealed class NewOrder : OrderCommand
{
    public NewOrder(NewOrderSingleExternal nos, string apiKey, string apiSecret)
        : base("order.place", BuildParameters(nos, apiKey), apiSecret)
    {
    }

    private static SortedDictionary<string, object> BuildParameters(NewOrderSingleExternal nos, string apiKey)
    {
        if (nos.OrdType != OrdType.Limit && nos.OrdType != OrdType.Market)
            throw new NotSupportedException("Only market and limit orders are supported");

        var parameters = new SortedDictionary<string, object>
        {
            ["apiKey"] = apiKey,
            ["quantity"] = nos.OrderQty.ToString(CultureInfo.InvariantCulture),
            ["side"] = nos.Side.ToBinanceString(),
            ["symbol"] = nos.ExternalContractId,
            ["timestamp"] = SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds(),
            ["type"] = nos.OrdType.ToBinanceString(),
            ["newClientOrderId"] = string.IsNullOrEmpty(nos.ClOrdId)
                ? nos.OrderId.ToString(CultureInfo.InvariantCulture)
                : nos.ClOrdId,
        };

        if (nos.OrdType == OrdType.Limit)
        {
            if (!nos.Price.HasValue) throw new InvalidOperationException("Price must be provided for a limit order");
            parameters.Add("price", nos.Price.Value.ToString(CultureInfo.InvariantCulture));
            parameters.Add("timeInForce", nos.TimeInForce.ToBinanceString());
        }

        if (nos.StopPx.HasValue)
            parameters.Add("stopPrice", nos.StopPx.Value.ToString(CultureInfo.InvariantCulture));

        if (nos.ExpireDt.HasValue)
            parameters.Add("goodTillDate", nos.ExpireDt.Value.ToUnixTimeMilliseconds());

        return parameters;
    }
}
