using System.Text.Json;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Messages.MarketData;

public enum BinanceMsgKind
{
    Unknown,
    ServiceAck,
    ServiceError,
    Ping,
    Pong,
    MarketData
}

public readonly record struct BinanceServiceMessage(
    BinanceMsgKind Kind,
    int? Id,
    int? Code,
    string? Msg,
    bool? ResultBool // sometimes result is true/false, often null
);

public static class BinanceMessageRouter
{
    public static BinanceMsgKind Classify(
        ReadOnlySpan<byte> utf8Json,
        out BinanceServiceMessage service
    )
    {
        service = default;

        var reader = new Utf8JsonReader(utf8Json, isFinalBlock: true, state: default);

        int? id = null;
        int? code = null;
        string? msg = null;
        bool? resultBool = null;

        bool seenResult = false;
        bool seenStream = false;
        bool seenData = false;

        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            // Top-level: "result", "id", "code", "msg", "error", "stream", "data", "ping", "pong"
            if (reader.ValueTextEquals("id"))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var v)) id = v;
                else SkipValue(ref reader);
            }
            else if (reader.ValueTextEquals("result"))
            {
                seenResult = true;
                reader.Read();
                if (reader.TokenType == JsonTokenType.True) resultBool = true;
                else if (reader.TokenType == JsonTokenType.False) resultBool = false;
                else if (reader.TokenType == JsonTokenType.Null) resultBool = null;
                else SkipValue(ref reader);
            }
            // else if (reader.ValueTextEquals("code"))
            // {
            //     reader.Read();
            //     if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var v)) code = v;
            //     else SkipValue(ref reader);
            // }
            // else if (reader.ValueTextEquals("msg"))
            // {
            //     reader.Read();
            //     if (reader.TokenType == JsonTokenType.String) msg = reader.GetString();
            //     else SkipValue(ref reader);
            // }
            else if (reader.ValueTextEquals("error"))
            {
                // error can be object: {"error":{"code":..,"msg":".."},"id":..}
                reader.Read();
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    ParseErrorObject(ref reader, ref code, ref msg);
                }
                else
                {
                    SkipValue(ref reader);
                }
            }
            else if (reader.ValueTextEquals("stream"))
            {
                seenStream = true;
                reader.Read();
                SkipValue(ref reader); // don't care here
            }
            else if (reader.ValueTextEquals("data"))
            {
                seenData = true;
                reader.Read();
                SkipValue(ref reader); // don't care here
            }
            else if (reader.ValueTextEquals("ping"))
            {
                reader.Read();
                SkipValue(ref reader);
                service = new BinanceServiceMessage(BinanceMsgKind.Ping, id, code, msg, resultBool);
                return BinanceMsgKind.Ping;
            }
            else if (reader.ValueTextEquals("pong"))
            {
                reader.Read();
                SkipValue(ref reader);
                service = new BinanceServiceMessage(BinanceMsgKind.Pong, id, code, msg, resultBool);
                return BinanceMsgKind.Pong;
            }
            else
            {
                // Skip unknown property value quickly
                reader.Read();
                SkipValue(ref reader);
            }

            // Early classification shortcuts:
            // - If we see stream+data => market data (combined stream wrapper)
            // - If we see result or code/msg/id pattern => service
            if (seenStream && seenData)
                return BinanceMsgKind.MarketData;
        }

        // Service patterns:
        if (seenResult && id is not null)
        {
            service = new BinanceServiceMessage(BinanceMsgKind.ServiceAck, id, null, null, resultBool);
            return BinanceMsgKind.ServiceAck;
        }

        if ((code is not null || msg is not null) && id is not null)
        {
            service = new BinanceServiceMessage(BinanceMsgKind.ServiceError, id, code, msg, null);
            return BinanceMsgKind.ServiceError;
        }

        return BinanceMsgKind.Unknown;
    }

    private static void ParseErrorObject(ref Utf8JsonReader reader, ref int? code, ref string? msg)
    {
        // reader is positioned at StartObject token
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return;
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            if (reader.ValueTextEquals("code"))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var v)) code = v;
                else SkipValue(ref reader);
            }
            else if (reader.ValueTextEquals("msg"))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.String) msg = reader.GetString();
                else SkipValue(ref reader);
            }
            else
            {
                reader.Read();
                SkipValue(ref reader);
            }
        }
    }

    private static void SkipValue(ref Utf8JsonReader r)
    {
        // r currently at a value token or start of object/array
        if (r.TokenType != JsonTokenType.StartObject && r.TokenType != JsonTokenType.StartArray)
            return;

        int depth = 0;
        do
        {
            if (r.TokenType == JsonTokenType.StartObject || r.TokenType == JsonTokenType.StartArray) depth++;
            else if (r.TokenType == JsonTokenType.EndObject || r.TokenType == JsonTokenType.EndArray) depth--;
        } while (depth > 0 && r.Read());
    }
}