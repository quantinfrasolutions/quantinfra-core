using System.Text.Json;
using NodaTime;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Messages.MarketData;

public readonly record struct Kline1m(
    int SubscriptionId,
    long Timestamp,
    long OpenTimeMs,
    long CloseTimeMs,
    double Open,
    double High,
    double Low,
    double Close,
    double Volume,
    bool IsClosed
)
{
    public ExchangeBar ToExchangeBar(int streamId, int? tradingSessionId) => new(
        streamId,
        null,
        Instant.FromUnixTimeMilliseconds(OpenTimeMs),
        Instant.FromUnixTimeMilliseconds(OpenTimeMs).Plus(Duration.FromMinutes(1)),
        Open,
        High,
        Low,
        Close,
        Volume,
        0,
        tradingSessionId
    );
}

public static class Kline1mParser
{
    public static bool TryParseKline1m(ReadOnlySpan<byte> utf8Json, IReadOnlyDictionary<string, int> symbolToSubscription, out Kline1m kline)
    {
        kline = default;

        var reader = new Utf8JsonReader(utf8Json, isFinalBlock: true, state: default);

        // Fields we care about
        int subscriptionId = 0;
        long t = 0, T = 0, ts = 0;
        double o = 0, h = 0, l = 0, c = 0, v = 0;
        bool x = false;

        // We’ll walk down to data -> k
        int depth = 0;
        bool inData = false, inK = false;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    depth++;
                    break;

                case JsonTokenType.EndObject:
                    // leaving nested objects
                    if (inK && depth == 3) inK = false; // heuristic: ... data:{ ... k:{...} }
                    if (inData && depth == 2) inData = false;
                    depth--;
                    break;

                case JsonTokenType.PropertyName:
                {
                    // Compare property names without allocating strings
                    if (!inData)
                    {
                        if (reader.ValueTextEquals("data"))
                        {
                            reader.Read(); // should be StartObject
                            inData = true;
                            depth++; // StartObject consumed
                        }
                        else
                        {
                            // Skip values we don't need at top-level
                            reader.Read();
                            SkipValue(ref reader);
                        }
                    }
                    else if (!inK)
                    {
                        if (reader.ValueTextEquals("e"))
                        {
                            reader.Read();
                            if (!reader.ValueTextEquals("kline")) return false;
                            reader.Read();
                        }
                        if (reader.ValueTextEquals("s"))
                        {
                            reader.Read();
                            // This will allocate if you call GetString(); instead map from UTF8
                            // If your map supports UTF8 key lookup, do that. Otherwise, one string allocation here.
                            if (!symbolToSubscription.TryGetValue(reader.GetString()!, out subscriptionId)) return false;
                        }
                        else if (reader.ValueTextEquals("k"))
                        {
                            reader.Read(); // StartObject
                            inK = true;
                            depth++; // StartObject consumed
                        }
                        else if (reader.ValueTextEquals("E"))
                        {
                            reader.Read();
                            ts = reader.GetInt64();
                        }
                        else
                        {
                            reader.Read();
                            SkipValue(ref reader);
                        }
                    }
                    else // inK
                    {
                        if (reader.ValueTextEquals("t"))
                        {
                            reader.Read();
                            t = reader.GetInt64();
                        }
                        else if (reader.ValueTextEquals("T"))
                        {
                            reader.Read();
                            T = reader.GetInt64();
                        }
                        else if (reader.ValueTextEquals("o"))
                        {
                            reader.Read();
                            o = ReadDoubleFlexible(ref reader);
                        }
                        else if (reader.ValueTextEquals("h"))
                        {
                            reader.Read();
                            h = ReadDoubleFlexible(ref reader);
                        }
                        else if (reader.ValueTextEquals("l"))
                        {
                            reader.Read();
                            l = ReadDoubleFlexible(ref reader);
                        }
                        else if (reader.ValueTextEquals("c"))
                        {
                            reader.Read();
                            c = ReadDoubleFlexible(ref reader);
                        }
                        else if (reader.ValueTextEquals("v"))
                        {
                            reader.Read();
                            v = ReadDoubleFlexible(ref reader);
                        }
                        else if (reader.ValueTextEquals("x"))
                        {
                            reader.Read();
                            x = reader.GetBoolean();
                        }
                        else
                        {
                            reader.Read();
                            SkipValue(ref reader);
                        }
                    }

                    break;
                }
            }
        }

        if (subscriptionId == 0) return false;

        kline = new Kline1m(subscriptionId, ts, t, T, o, h, l, c, v, x);
        return true;
    }

    static void SkipValue(ref Utf8JsonReader r)
    {
        // Assumes we've already advanced to the value token.
        if (r.TokenType != JsonTokenType.StartObject && r.TokenType != JsonTokenType.StartArray)
            return;

        int depth = 0;
        do
        {
            if (r.TokenType == JsonTokenType.StartObject || r.TokenType == JsonTokenType.StartArray) depth++;
            else if (r.TokenType == JsonTokenType.EndObject || r.TokenType == JsonTokenType.EndArray) depth--;
            r.Read();
        } while (depth > 0);
    }

    static double ReadDoubleFlexible(ref Utf8JsonReader r)
    {
        // Binance numbers are often strings ("123.45"). This handles both string and number.
        return r.TokenType switch
        {
            JsonTokenType.Number => r.GetDouble(),
            JsonTokenType.String => double.Parse(r.GetString()!, System.Globalization.CultureInfo.InvariantCulture),
            _ => 0d
        };
    }
}