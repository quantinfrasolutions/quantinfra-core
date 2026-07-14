using System.Text;
using QuantInfra.Connectors.Binance.Futures.Usdm.Messages.MarketData;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Tests;

public class Kline1mParserTests
{
    private static readonly IReadOnlyDictionary<string, int> Subscriptions =
        new Dictionary<string, int> { ["BTCUSDT"] = 42 };

    [Test]
    public void TryParseKline1m_ValidCombinedStream_MapsFields()
    {
        var json = Encoding.UTF8.GetBytes("""
            {
              "stream":"btcusdt@kline_1m",
              "data":{
                "e":"kline",
                "E":1499404907056,
                "s":"BTCUSDT",
                "k":{
                  "t":1499404860000,
                  "T":1499404919999,
                  "o":"0.10278577",
                  "h":"0.10278712",
                  "l":"0.10278518",
                  "c":"0.10278645",
                  "v":"17.47929838",
                  "x":true
                }
              }
            }
            """);

        var parsed = Kline1mParser.TryParseKline1m(json, Subscriptions, out var kline);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.True);
            Assert.That(kline.SubscriptionId, Is.EqualTo(42));
            Assert.That(kline.Timestamp, Is.EqualTo(1499404907056));
            Assert.That(kline.OpenTimeMs, Is.EqualTo(1499404860000));
            Assert.That(kline.CloseTimeMs, Is.EqualTo(1499404919999));
            Assert.That(kline.Open, Is.EqualTo(0.10278577d));
            Assert.That(kline.High, Is.EqualTo(0.10278712d));
            Assert.That(kline.Low, Is.EqualTo(0.10278518d));
            Assert.That(kline.Close, Is.EqualTo(0.10278645d));
            Assert.That(kline.Volume, Is.EqualTo(17.47929838d));
            Assert.That(kline.IsClosed, Is.True);
        });
    }

    [Test]
    public void TryParseKline1m_NumberValues_AcceptsUnquotedNumbers()
    {
        var json = Encoding.UTF8.GetBytes("""
            {"data":{"e":"kline","s":"BTCUSDT","k":{
              "t":1,"T":2,"o":1.1,"h":2.2,"l":0.5,"c":2.0,"v":3.5,"x":false
            }}}
            """);

        var parsed = Kline1mParser.TryParseKline1m(json, Subscriptions, out var kline);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.True);
            Assert.That(kline.Open, Is.EqualTo(1.1d));
            Assert.That(kline.Volume, Is.EqualTo(3.5d));
            Assert.That(kline.IsClosed, Is.False);
        });
    }

    [Test]
    public void TryParseKline1m_UnknownSymbol_ReturnsFalse()
    {
        var json = Encoding.UTF8.GetBytes("""
            {"data":{"e":"kline","s":"ETHUSDT","k":{}}}
            """);

        var parsed = Kline1mParser.TryParseKline1m(json, Subscriptions, out _);

        Assert.That(parsed, Is.False);
    }

    [Test]
    public void TryParseKline1m_DifferentEventType_ReturnsFalse()
    {
        var json = Encoding.UTF8.GetBytes("""
            {"data":{"e":"depthUpdate","s":"BTCUSDT"}}
            """);

        var parsed = Kline1mParser.TryParseKline1m(json, Subscriptions, out _);

        Assert.That(parsed, Is.False);
    }
}
