using System.Text;
using GenericWebSocketClient;
using QuantInfra.Connectors.Binance.Futures.Usdm.Messages.MarketData;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Tests;

public class BinanceMessageRouterTests
{
    [Test]
    public void Classify_SubscriptionAcknowledgement_ReturnsServiceAck()
    {
        var json = Encoding.UTF8.GetBytes("""{"result":null,"id":1}""");

        var kind = BinanceMessageRouter.Classify(json, out var service);

        Assert.Multiple(() =>
        {
            Assert.That(kind, Is.EqualTo(BinanceMsgKind.ServiceAck));
            Assert.That(service.Kind, Is.EqualTo(BinanceMsgKind.ServiceAck));
            Assert.That(service.Id, Is.EqualTo(1));
            Assert.That(service.ResultBool, Is.Null);
        });
    }

    [Test]
    public void Classify_BoundedIngressSpan_IgnoresUnusedPooledBufferCapacity()
    {
        var payload = Encoding.UTF8.GetBytes("""{"result":null,"id":1}""");
        var pooledBuffer = new byte[64];
        payload.CopyTo(pooledBuffer, 0);
        var message = new IngressMessage(pooledBuffer, payload.Length, 0, 0);

        var kind = BinanceMessageRouter.Classify(message.AsSpan(), out var service);

        Assert.Multiple(() =>
        {
            Assert.That(kind, Is.EqualTo(BinanceMsgKind.ServiceAck));
            Assert.That(service.Id, Is.EqualTo(1));
        });
    }

    [Test]
    public void Classify_CombinedStreamMessage_ReturnsMarketData()
    {
        var json = Encoding.UTF8.GetBytes("""
            {"stream":"btcusdt@depth","data":{"e":"depthUpdate"}}
            """);

        var kind = BinanceMessageRouter.Classify(json, out _);

        Assert.That(kind, Is.EqualTo(BinanceMsgKind.MarketData));
    }

    [Test]
    public void Classify_ErrorObject_ReturnsServiceError()
    {
        var json = Encoding.UTF8.GetBytes("""
            {"error":{"code":-1121,"msg":"Invalid symbol."},"id":7}
            """);

        var kind = BinanceMessageRouter.Classify(json, out var service);

        Assert.Multiple(() =>
        {
            Assert.That(kind, Is.EqualTo(BinanceMsgKind.ServiceError));
            Assert.That(service.Id, Is.EqualTo(7));
            Assert.That(service.Code, Is.EqualTo(-1121));
            Assert.That(service.Msg, Is.EqualTo("Invalid symbol."));
        });
    }

    [Test]
    public void Classify_UnrecognizedMessage_ReturnsUnknown()
    {
        var json = Encoding.UTF8.GetBytes("""{"notice":"maintenance"}""");

        var kind = BinanceMessageRouter.Classify(json, out _);

        Assert.That(kind, Is.EqualTo(BinanceMsgKind.Unknown));
    }
}
