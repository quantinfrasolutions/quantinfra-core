using System.Globalization;
using Binance.Futures.USDM;
using NodaTime;
using QuantInfra.Connectors.Binance.Common;
using QuantInfra.Sdk.Trading.ExternalAccounts;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Messages.Commands;

public sealed class ModifyOrder : OrderCommand
{
    public ModifyOrder(OrderReplaceRequestExternal request, string apiKey, string apiSecret)
        : base("order.modify", BuildParameters(request, apiKey), apiSecret)
    {
    }

    private static SortedDictionary<string, object> BuildParameters(OrderReplaceRequestExternal request, string apiKey)
    {
        if (!request.Price.HasValue || !request.OrderQty.HasValue || !request.Side.HasValue)
            throw new InvalidModifyRequestException("Price, OrderQty, and Side must be provided");

        var parameters = new SortedDictionary<string, object>
        {
            ["apiKey"] = apiKey,
            ["price"] = request.Price.Value.ToString(CultureInfo.InvariantCulture),
            ["quantity"] = request.OrderQty.Value.ToString(CultureInfo.InvariantCulture),
            ["side"] = request.Side.Value.ToBinanceString(),
            ["symbol"] = request.ExternalContractId,
            ["timestamp"] = SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds(),
        };

        if (!string.IsNullOrEmpty(request.ExternalOrderId))
        {
            if (!long.TryParse(request.ExternalOrderId, NumberStyles.None, CultureInfo.InvariantCulture, out var externalOrderId))
                throw new OrderIdNotProvidedException("ExternalOrderId must be a Binance numeric order id");
            parameters.Add("orderId", externalOrderId);
        }
        else if (request.OrderId.HasValue)
        {
            parameters.Add("origClientOrderId", request.OrderId.Value.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            throw new OrderIdNotProvidedException("Either ExternalOrderId or OrderId must be provided");
        }

        return parameters;
    }
}
