using System.Globalization;
using System.Text.Json;
using Binance.Futures.USDM;
using QuantInfra.Connectors.Binance.Common;
using QuantInfra.Connectors.Binance.Futures.Usdm.Messages.Commands;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.ExternalAccounts;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Tests;

public class OrderCommandTests
{
    private const string ApiKey = "test-api-key";
    private const string ApiSecret = "test-api-secret";

    [Test]
    public void NewOrder_LimitOrder_SerializesSignedWebSocketRequest()
    {
        var order = new NewOrderSingleExternal
        {
            ExternalContractId = "BTCUSDT",
            OrderId = 42,
            ClOrdId = "42",
            OrdType = OrdType.Limit,
            Side = Side.Buy,
            OrderQty = 0.125m,
            Price = 61234.50m,
            StopPx = 60000.25m,
            ExpireDt = NodaTime.Instant.FromUnixTimeMilliseconds(1_800_000_000_000),
            TimeInForce = TimeInForce.GoodTillCancelled,
        };

        var command = new NewOrder(order, ApiKey, ApiSecret);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(command));

        Assert.Multiple(() =>
        {
            Assert.That(command.Method, Is.EqualTo("order.place"));
            Assert.That(command.Params["symbol"], Is.EqualTo("BTCUSDT"));
            Assert.That(command.Params["newClientOrderId"], Is.EqualTo("42"));
            Assert.That(command.Params["quantity"], Is.EqualTo("0.125"));
            Assert.That(command.Params["price"], Is.EqualTo("61234.50"));
            Assert.That(command.Params["stopPrice"], Is.EqualTo("60000.25"));
            Assert.That(command.Params["goodTillDate"], Is.EqualTo(1_800_000_000_000L));
            Assert.That(command.Params["timeInForce"], Is.EqualTo("GTC"));
            Assert.That(json.RootElement.GetProperty("method").GetString(), Is.EqualTo("order.place"));
            Assert.That(json.RootElement.GetProperty("params").GetProperty("timestamp").ValueKind, Is.EqualTo(JsonValueKind.Number));
            Assert.That(command.Params["signature"], Is.EqualTo(ExpectedSignature(command)));
        });
    }

    [Test]
    public void NewOrder_UsesInvariantDecimalFormatting()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var order = new NewOrderSingleExternal
            {
                ExternalContractId = "ETHUSDT",
                OrderId = 7,
                OrdType = OrdType.Market,
                Side = Side.Sell,
                OrderQty = 1.25m,
            };

            var command = new NewOrder(order, ApiKey, ApiSecret);

            Assert.That(command.Params["quantity"], Is.EqualTo("1.25"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Test]
    public void CancelOrder_WithExchangeOrderId_UsesNumericOrderId()
    {
        var request = new OrderCancelRequestExternal(42, 1, "BTCUSDT", "987654321");

        var command = new CancelOrder(request, ApiKey, ApiSecret);

        Assert.Multiple(() =>
        {
            Assert.That(command.Method, Is.EqualTo("order.cancel"));
            Assert.That(command.Params["orderId"], Is.EqualTo(987654321L));
            Assert.That(command.Params.ContainsKey("origClientOrderId"), Is.False);
            Assert.That(command.Params["signature"], Is.EqualTo(ExpectedSignature(command)));
        });
    }

    [Test]
    public void CancelOrder_WithoutExchangeOrderId_UsesOriginalClientOrderId()
    {
        var request = new OrderCancelRequestExternal(42, 1, "BTCUSDT", null);

        var command = new CancelOrder(request, ApiKey, ApiSecret);

        Assert.That(command.Params["origClientOrderId"], Is.EqualTo("42"));
    }

    [Test]
    public void ModifyOrder_MapsRequiredParameters()
    {
        var request = new OrderReplaceRequestExternal
        {
            AccountId = 1,
            RequestId = "replace-1",
            OrderId = 42,
            ExternalOrderId = "987654321",
            ExternalContractId = "BTCUSDT",
            Side = Side.Sell,
            OrderQty = 0.25m,
            Price = 62000.10m,
            OrdType = OrdType.Limit,
        };

        var command = new ModifyOrder(request, ApiKey, ApiSecret);

        Assert.Multiple(() =>
        {
            Assert.That(command.Method, Is.EqualTo("order.modify"));
            Assert.That(command.Params["side"], Is.EqualTo("SELL"));
            Assert.That(command.Params["quantity"], Is.EqualTo("0.25"));
            Assert.That(command.Params["price"], Is.EqualTo("62000.10"));
            Assert.That(command.Params["orderId"], Is.EqualTo(987654321L));
            Assert.That(command.Params["signature"], Is.EqualTo(ExpectedSignature(command)));
        });
    }

    private static string ExpectedSignature(OrderCommand command)
    {
        var unsignedParameters = new SortedDictionary<string, object>(
            command.Params.Where(parameter => parameter.Key != "signature")
                .ToDictionary(parameter => parameter.Key, parameter => parameter.Value));
        return unsignedParameters.BuildSignaturePayload().GetHmacSha256Signature(ApiSecret);
    }
}
