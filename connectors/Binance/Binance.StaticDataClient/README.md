# Binance.StaticDataClient

Retrieves assets and contracts from Binance's public exchange-information REST APIs. USDⓈ-M futures is the default market; Spot and COIN-M futures use the same client and normalized result models.

```csharp
using QuantInfra.Connectors.Binance.StaticDataClient;

var httpClient = new HttpClient();
var client = new BinanceStaticDataClient(httpClient);

// Calls GET https://fapi.binance.com/fapi/v1/exchangeInfo
var exchangeInfo = await client.GetExchangeInfoAsync(cancellationToken);
var assets = exchangeInfo.Assets;
var contracts = exchangeInfo.Contracts;

// Supported when these markets are needed:
var spot = await client.GetExchangeInfoAsync(BinanceMarket.Spot, cancellationToken);
var coinm = await client.GetExchangeInfoAsync(BinanceMarket.CoinmFutures, cancellationToken);
```

Prefer `GetExchangeInfoAsync` when both assets and contracts are required, because it returns both from one HTTP request. `GetAssetsAsync` and `GetContractsAsync` are convenience methods and each performs its own request.

The client uses public endpoints and does not require API credentials. Base addresses and the default market can be overridden with `BinanceStaticDataClientOptions`, including for testnet use.
