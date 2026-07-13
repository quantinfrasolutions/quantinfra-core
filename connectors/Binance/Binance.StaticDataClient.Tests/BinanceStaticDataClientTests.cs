using System.Net;
using System.Text;
using QuantInfra.Connectors.Binance.Common;

namespace QuantInfra.Connectors.Binance.StaticDataClient.Tests;

public class BinanceStaticDataClientTests
{
    [Test]
    public async Task DefaultMarket_MapsUsdmAssetsContractsAndFilters()
    {
        const string json = """
        {
          "serverTime": 1565613908500,
          "assets": [
            { "asset": "USDT", "marginAvailable": true, "autoAssetExchange": "-10000" }
          ],
          "symbols": [{
            "symbol": "BTCUSDT", "pair": "BTCUSDT", "contractType": "PERPETUAL",
            "deliveryDate": 4133404800000, "onboardDate": 1569398400000,
            "status": "TRADING", "baseAsset": "BTC", "quoteAsset": "USDT",
            "marginAsset": "USDT", "pricePrecision": 2, "quantityPrecision": 3,
            "baseAssetPrecision": 8, "quotePrecision": 8,
            "underlyingType": "COIN", "underlyingSubType": ["PoW"],
            "orderTypes": ["LIMIT", "MARKET"], "timeInForce": ["GTC"],
            "filters": [
              { "filterType": "PRICE_FILTER", "minPrice": "0.10", "maxPrice": "1000000", "tickSize": "0.10" },
              { "filterType": "LOT_SIZE", "minQty": "0.001", "maxQty": "1000", "stepSize": "0.001" }
            ]
          }]
        }
        """;
        var handler = new StubHandler(json);
        var client = new BinanceStaticDataClient(new HttpClient(handler));

        var result = await client.GetExchangeInfoAsync();

        Assert.Multiple(() =>
        {
            Assert.That(handler.LastRequestUri, Is.EqualTo(new Uri("https://fapi.binance.com/fapi/v1/exchangeInfo")));
            Assert.That(result.Market, Is.EqualTo(BinanceMarket.UsdmFutures));
            Assert.That(result.ServerTime, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1565613908500)));
            Assert.That(result.Assets, Has.Count.EqualTo(2));
        });

        var usdt = result.Assets.Single(x => x.Symbol == "USDT");
        Assert.Multiple(() =>
        {
            Assert.That(usdt.IsQuoteAsset, Is.True);
            Assert.That(usdt.IsMarginAsset, Is.True);
            Assert.That(usdt.IsMarginAvailable, Is.True);
            Assert.That(usdt.AutoAssetExchange, Is.EqualTo(-10000m));
        });

        var contract = result.Contracts.Single();
        Assert.Multiple(() =>
        {
            Assert.That(contract.Symbol, Is.EqualTo("BTCUSDT"));
            Assert.That(contract.ContractType, Is.EqualTo("PERPETUAL"));
            Assert.That(contract.IsTrading, Is.True);
            Assert.That(contract.SettlementAsset, Is.EqualTo("USDT"));
            Assert.That(contract.GetFilter("PRICE_FILTER")!.GetDecimal("tickSize"), Is.EqualTo(0.10m));
            Assert.That(contract.GetFilter("LOT_SIZE")!.GetDecimal("stepSize"), Is.EqualTo(0.001m));
        });
    }

    [Test]
    public async Task Spot_DerivesAssetsFromSymbolsAndUsesSpotEndpoint()
    {
        const string json = """
        { "symbols": [{
          "symbol": "ETHBTC", "status": "TRADING", "baseAsset": "ETH",
          "baseAssetPrecision": 8, "quoteAsset": "BTC", "quoteAssetPrecision": 8,
          "orderTypes": ["LIMIT"], "filters": []
        }] }
        """;
        var handler = new StubHandler(json);
        var client = new BinanceStaticDataClient(new HttpClient(handler));

        var result = await client.GetExchangeInfoAsync(BinanceMarket.Spot);

        Assert.Multiple(() =>
        {
            Assert.That(handler.LastRequestUri, Is.EqualTo(new Uri("https://api.binance.com/api/v3/exchangeInfo")));
            Assert.That(result.Assets.Select(x => x.Symbol), Is.EqualTo(new[] { "BTC", "ETH" }));
            Assert.That(result.Contracts.Single().SettlementAsset, Is.EqualTo("BTC"));
            Assert.That(result.Contracts.Single().QuoteAssetPrecision, Is.EqualTo(8));
        });
    }

    [Test]
    public async Task Coinm_MapsInverseContractFieldsAndUsesCoinmEndpoint()
    {
        const string json = """
        { "symbols": [{
          "symbol": "BTCUSD_PERP", "pair": "BTCUSD", "contractType": "PERPETUAL",
          "contractStatus": "TRADING", "contractSize": 100,
          "baseAsset": "BTC", "quoteAsset": "USD", "marginAsset": "BTC",
          "pricePrecision": 1, "quantityPrecision": 0, "filters": []
        }] }
        """;
        var handler = new StubHandler(json);
        var client = new BinanceStaticDataClient(new HttpClient(handler));

        var contracts = await client.GetContractsAsync(BinanceMarket.CoinmFutures);

        Assert.Multiple(() =>
        {
            Assert.That(handler.LastRequestUri, Is.EqualTo(new Uri("https://dapi.binance.com/dapi/v1/exchangeInfo")));
            Assert.That(contracts.Single().Status, Is.EqualTo("TRADING"));
            Assert.That(contracts.Single().ContractSize, Is.EqualTo(100m));
            Assert.That(contracts.Single().SettlementAsset, Is.EqualTo("BTC"));
        });
    }

    [Test]
    public void FailedResponse_ThrowsTypedExceptionWithResponseBody()
    {
        var handler = new StubHandler("{\"code\":-1003,\"msg\":\"Too many requests\"}", HttpStatusCode.TooManyRequests);
        var client = new BinanceStaticDataClient(new HttpClient(handler));

        var exception = Assert.ThrowsAsync<BinanceStaticDataException>(
            async () => await client.GetExchangeInfoAsync(BinanceMarket.UsdmFutures));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
            Assert.That(exception.Market, Is.EqualTo(BinanceMarket.UsdmFutures));
            Assert.That(exception.ResponseBody, Does.Contain("Too many requests"));
        });
    }

    private sealed class StubHandler(string response, HttpStatusCode statusCode = HttpStatusCode.OK)
        : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            });
        }
    }
}
