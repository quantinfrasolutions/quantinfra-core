using System.Net;
using QuantInfra.Connectors.Binance.Common;

namespace QuantInfra.Connectors.Binance.StaticDataClient;

public sealed class BinanceStaticDataException : HttpRequestException
{
    public BinanceStaticDataException(BinanceMarket market, HttpStatusCode statusCode, string responseBody)
        : base($"Binance {market} exchange-info request failed with HTTP {(int)statusCode} ({statusCode}).",
            null, statusCode)
    {
        Market = market;
        ResponseBody = responseBody;
    }

    public BinanceMarket Market { get; }
    public string ResponseBody { get; }
}
