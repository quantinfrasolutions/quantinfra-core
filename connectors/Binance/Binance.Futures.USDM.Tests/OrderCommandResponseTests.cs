using System.Text;
using QuantInfra.Connectors.Binance.Common;
using QuantInfra.Connectors.Binance.Futures.Usdm.Messages.Commands;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Tests;

public class OrderCommandResponseTests
{
    [Test]
    public void TryParse_SuccessResponse_CorrelatesRequest()
    {
        var json = Encoding.UTF8.GetBytes("""
            {"id":"request-1","status":200,"result":{"orderId":123}}
            """);

        var parsed = OrderCommandResponse.TryParse(json, out var response);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.True);
            Assert.That(response.RequestId, Is.EqualTo("request-1"));
            Assert.That(response.IsSuccess, Is.True);
            Assert.That(response.ErrorCode, Is.Null);
        });
    }

    [Test]
    public void TryParse_ErrorResponse_MapsBinanceError()
    {
        var json = Encoding.UTF8.GetBytes("""
            {"id":"request-2","status":400,"error":{"code":-2011,"msg":"Unknown order sent."}}
            """);

        var parsed = OrderCommandResponse.TryParse(json, out var response);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.True);
            Assert.That(response.IsSuccess, Is.False);
            Assert.That(response.Status, Is.EqualTo(400));
            Assert.That(response.ErrorCode, Is.EqualTo(-2011));
            Assert.That(response.ErrorMessage, Is.EqualTo("Unknown order sent."));
        });
    }

    [TestCase("https://fapi.binance.com/", "wss://ws-fapi.binance.com/ws-fapi/v1")]
    [TestCase("https://testnet.binancefuture.com/", "wss://testnet.binancefuture.com/ws-fapi/v1")]
    public void ResolveUri_KnownEnvironment_UsesMatchingWebSocketApiEndpoint(string restUri, string expected)
    {
        var config = new TradingClientConfig { RestUri = restUri };

        var uri = OrderWebSocketClient.ResolveUri(config);

        Assert.That(uri, Is.EqualTo(new Uri(expected)));
    }

    [Test]
    public void ResolveUri_ExplicitEndpoint_TakesPrecedence()
    {
        var config = new TradingClientConfig
        {
            RestUri = "https://gateway.example.com/",
            WebSocketApiUri = "wss://gateway.example.com/orders",
        };

        var uri = OrderWebSocketClient.ResolveUri(config);

        Assert.That(uri, Is.EqualTo(new Uri("wss://gateway.example.com/orders")));
    }
}
