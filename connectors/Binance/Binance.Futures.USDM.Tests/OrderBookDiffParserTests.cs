using System.Text;
using QuantInfra.Connectors.Binance.Futures.Usdm.Messages.MarketData;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Tests;

public class OrderBookDiffParserTests
{
    private static readonly IReadOnlyDictionary<string, int> Subscriptions =
        new Dictionary<string, int> { ["BTCUSDT"] = 12 };

    [Test]
    public void TryParseDepthDiff_ValidCombinedStream_MapsUpdates()
    {
        var json = Encoding.UTF8.GetBytes("""
            {
              "stream":"btcusdt@depth",
              "data":{
                "e":"depthUpdate",
                "E":123456789,
                "s":"BTCUSDT",
                "U":157,
                "u":160,
                "b":[["0.0024","10"],["0.0025","0"]],
                "a":[["0.0026","100"]]
              }
            }
            """);

        var parsed = OrderBookDiffParser.TryParseDepthDiff(json, Subscriptions, out var diff);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.True);
            Assert.That(diff.SubscriptionId, Is.EqualTo(12));
            Assert.That(diff.EventTimeMs, Is.EqualTo(123456789));
            Assert.That(diff.FirstUpdateId, Is.EqualTo(157));
            Assert.That(diff.FinalUpdateId, Is.EqualTo(160));
            Assert.That(diff.BidCount, Is.EqualTo(2));
            Assert.That(diff.AskCount, Is.EqualTo(1));
            Assert.That(diff.Bids[0], Is.EqualTo(new OrderBookLevelUpdate(0.0024m, 10m)));
            Assert.That(diff.Bids[1], Is.EqualTo(new OrderBookLevelUpdate(0.0025m, 0m)));
            Assert.That(diff.Asks[0], Is.EqualTo(new OrderBookLevelUpdate(0.0026m, 100m)));
        });
    }

    [Test]
    public void TryParseDepthDiff_NumberValues_AcceptsUnquotedNumbers()
    {
        var json = Encoding.UTF8.GetBytes("""
            {"data":{"e":"depthUpdate","s":"BTCUSDT","U":1,"u":2,
              "b":[[1.25,3]],"a":[[1.5,4]]}}
            """);

        var parsed = OrderBookDiffParser.TryParseDepthDiff(json, Subscriptions, out var diff);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.True);
            Assert.That(diff.Bids[0], Is.EqualTo(new OrderBookLevelUpdate(1.25m, 3m)));
            Assert.That(diff.Asks[0], Is.EqualTo(new OrderBookLevelUpdate(1.5m, 4m)));
        });
    }

    [Test]
    public void TryParseDepthDiff_UnknownSymbol_ReturnsFalse()
    {
        var json = Encoding.UTF8.GetBytes("""
            {"data":{"e":"depthUpdate","s":"ETHUSDT","U":1,"u":2,"b":[],"a":[]}}
            """);

        var parsed = OrderBookDiffParser.TryParseDepthDiff(json, Subscriptions, out _);

        Assert.That(parsed, Is.False);
    }

    [Test]
    public void TryParseDepthDiff_MissingFinalUpdateId_ReturnsFalse()
    {
        var json = Encoding.UTF8.GetBytes("""
            {"data":{"e":"depthUpdate","s":"BTCUSDT","U":1,"b":[],"a":[]}}
            """);

        var parsed = OrderBookDiffParser.TryParseDepthDiff(json, Subscriptions, out _);

        Assert.That(parsed, Is.False);
    }
}
