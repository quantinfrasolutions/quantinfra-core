using System.Text.Json;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Messages.MarketData;

public readonly record struct OrderBookLevelUpdate(decimal Price, decimal Quantity);

public readonly record struct OrderBookDiff(
    int SubscriptionId,
    long EventTimeMs,
    long FirstUpdateId,
    long FinalUpdateId,
    OrderBookLevelUpdate[] Bids,
    int BidCount,
    OrderBookLevelUpdate[] Asks,
    int AskCount
);

public static class OrderBookDiffParser
{
    public static bool TryParseDepthDiff(ReadOnlySpan<byte> json, IReadOnlyDictionary<string, int> symbolToSubscription, out OrderBookDiff msg)
    {
        msg = default;
        var reader = new Utf8JsonReader(json, isFinalBlock: true, state: default);
        int subscriptionId = -1;
        long eventTime = 0;
        long firstUpdateId = 0;
        long finalUpdateId = 0;
        var bids = new OrderBookLevelUpdate[128];
        var asks = new OrderBookLevelUpdate[128];
        int bidCount = 0;
        int askCount = 0;

        bool inData = false;

        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals("data"))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    inData = true;
                    continue;
                }
            }

            if (reader.ValueTextEquals("e"))
            {
                reader.Read();
                // Optional: verify event type.
                if (reader.TokenType == JsonTokenType.String &&
                    !reader.ValueTextEquals("depthUpdate"))
                {
                    SkipValue(ref reader);
                    continue;
                }
            }

            else if (reader.ValueTextEquals("E"))
            {
                reader.Read();
                eventTime = reader.GetInt64();
            }

            else if (reader.ValueTextEquals("s"))
            {
                reader.Read();
                if (!symbolToSubscription.TryGetValue(reader.GetString(), out subscriptionId)) return false;
            }

            else if (reader.ValueTextEquals("U"))
            {
                reader.Read();
                firstUpdateId = reader.GetInt64();
            }

            else if (reader.ValueTextEquals("u"))
            {
                reader.Read();
                finalUpdateId = reader.GetInt64();
            }

            else if (reader.ValueTextEquals("b"))
            {
                reader.Read();
                bidCount = ReadLevels(ref reader, ref bids);
            }

            else if (reader.ValueTextEquals("a"))
            {
                reader.Read();
                askCount = ReadLevels(ref reader, ref asks);
            }
            else
            {
                reader.Read();
                SkipValue(ref reader);
            }
        }

        if (subscriptionId < 0 || finalUpdateId == 0)
            return false;

        msg = new OrderBookDiff(
            SubscriptionId: subscriptionId,
            EventTimeMs: eventTime,
            FirstUpdateId: firstUpdateId,
            FinalUpdateId: finalUpdateId,
            Bids: bids,
            BidCount: bidCount,
            Asks: asks,
            AskCount: askCount);

        return true;
    }

    private static int ReadLevels(
        ref Utf8JsonReader reader,
        ref OrderBookLevelUpdate[] levels)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            SkipValue(ref reader);
            return 0;
        }

        int count = 0;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return count;

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                SkipValue(ref reader);
                continue;
            }

            // level array: ["price", "qty"]
            reader.Read();

            var price = ReadDecimalFlexible(ref reader);
            reader.Read();
            var qty = ReadDecimalFlexible(ref reader);
            // consume until end of this level array
            while (reader.TokenType != JsonTokenType.EndArray && reader.Read()) { }
            if (count == levels.Length)
                Array.Resize(ref levels, levels.Length * 2);

            levels[count++] = new OrderBookLevelUpdate(price, qty);
        }

        return count;
    }

    private static decimal ReadDecimalFlexible(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetDecimal(),
            JsonTokenType.String => decimal.Parse(reader.GetString()!, System.Globalization.CultureInfo.InvariantCulture),
            _ => 0m
        };

    }

    private static void SkipValue(ref Utf8JsonReader r)
    {
        if (r.TokenType != JsonTokenType.StartObject &&
            r.TokenType != JsonTokenType.StartArray)
            return;

        int depth = 0;

        do
        {
            if (r.TokenType == JsonTokenType.StartObject ||
                r.TokenType == JsonTokenType.StartArray)
                depth++;
            
            else if (r.TokenType == JsonTokenType.EndObject ||
                     r.TokenType == JsonTokenType.EndArray)
                depth--;

        }
        while (depth > 0 && r.Read());
    }

}