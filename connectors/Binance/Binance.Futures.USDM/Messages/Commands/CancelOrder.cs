using System.Globalization;
using Binance.Futures.USDM;
using NodaTime;
using QuantInfra.Sdk.Trading.ExternalAccounts;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Messages.Commands;

public sealed class CancelOrder : OrderCommand
{
    public CancelOrder(OrderCancelRequestExternal request, string apiKey, string apiSecret)
        : base("order.cancel", BuildParameters(request, apiKey), apiSecret)
    {
    }

    private static SortedDictionary<string, object> BuildParameters(OrderCancelRequestExternal request, string apiKey)
    {
        var parameters = new SortedDictionary<string, object>
        {
            ["apiKey"] = apiKey,
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
