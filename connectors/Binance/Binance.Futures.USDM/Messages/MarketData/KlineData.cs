using System.Text.Json.Serialization;
using NodaTime;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Messages.MarketData;

// https://developers.binance.com/docs/derivatives/usds-margined-futures/websocket-market-streams/Kline-Candlestick-Streams
// {
//     "e": "kline",     // Event type
//     "E": 1638747660000,   // Event time
//     "s": "BTCUSDT",    // Symbol
//     "k": {
//         "t": 1638747660000, // Kline start time
//         "T": 1638747719999, // Kline close time
//         "s": "BTCUSDT",  // Symbol
//         "i": "1m",      // Interval
//         "f": 100,       // First trade ID
//         "L": 200,       // Last trade ID
//         "o": "0.0010",  // Open price
//         "c": "0.0020",  // Close price
//         "h": "0.0025",  // High price
//         "l": "0.0015",  // Low price
//         "v": "1000",    // Base asset volume
//         "n": 100,       // Number of trades
//         "x": false,     // Is this kline closed?
//         "q": "1.0000",  // Quote asset volume
//         "V": "500",     // Taker buy base asset volume
//         "Q": "0.500",   // Taker buy quote asset volume
//         "B": "123456"   // Ignore
//     }
// }

public class KlineData : StreamDataBase
{
    [JsonPropertyName("k")] public Kline Kline { get; set; }

    public ExchangeBar ToExchangeBar(int streamId, int datasourceId, int? tradingSessionId) => 
        new(streamId, null,
            Instant.FromUnixTimeMilliseconds(Kline.OpenTs), Instant.FromUnixTimeMilliseconds(Kline.CloseTs + 1),
            Kline.Open, Kline.High, Kline.Low, Kline.Close, Kline.Volume, Kline.QuoteAssetVolume, datasourceId,
            tradingSessionId
        );
}

public class Kline
{
    [JsonPropertyName("t")] public long OpenTs { get; set; }
    [JsonPropertyName("T")] public long CloseTs { get; set; }
    [JsonPropertyName("s")] public string Symbol { get; set;}
    [JsonPropertyName("i")] public string Interval { get; set; }
    [JsonPropertyName("f")] public long FirstTradeId { get; set; }
    [JsonPropertyName("L")] public long LastTradeId { get; set; }
    [JsonPropertyName("o")] public double Open { get; set; }
    [JsonPropertyName("c")] public double Close { get; set; }
    [JsonPropertyName("h")] public double High { get; set; }
    [JsonPropertyName("l")] public double Low { get; set; }
    [JsonPropertyName("v")] public double Volume { get; set; }
    [JsonPropertyName("n")] public int TradesNum { get; set; }
    [JsonPropertyName("x")] public bool IsClosed { get; set; }
    [JsonPropertyName("q")] public double QuoteAssetVolume { get; set; }
    [JsonPropertyName("V")] public double TakerBuyBaseAssetVolume { get; set; }
    [JsonPropertyName("Q")] public double TakerBuyQuoteAssetVolume { get; set; }
    [JsonPropertyName("B")] public string Ignore { get; set; }
}