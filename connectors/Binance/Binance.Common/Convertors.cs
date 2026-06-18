using System.Globalization;
using Common.Trading;
using NodaTime;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.Trading.ExternalAccounts;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Connectors.Binance.Common;

public static class Convertors
{
    public static string GetBinanceSubscriptionType(this SubscriptionType t) => t switch
    {
        SubscriptionType.Trades => "aggTrade",
        SubscriptionType.Candles1M => "kline_1m",
        _ => throw new NotSupportedException()
    };
    
    public static string ToBinanceString(this Side side) => side switch
    {
        Side.Buy => "BUY",
        Side.Sell => "SELL",
        _ => throw new InvalidCastException($"Side {side} is not supported")
    };

    public static Side GetSide(this string side) => side switch
    {
        "BUY" => Side.Buy,
        "SELL" => Side.Sell,
        _ => throw new InvalidCastException($"Side {side} is not supported")
    };
    
    public static Side GetSide(this BinancePosition position) => position.PositionAmt > 0
        ? Side.Buy
        : (position.PositionAmt < 0 ?
            Side.Sell
            : throw new NotSupportedException("Cannot get side for a zero position"));

    public static string ToBinanceString(this OrdType ordType) => ordType switch
    {
        OrdType.Market => "MARKET",
        OrdType.Limit => "LIMIT",
        OrdType.StopMarket => "STOP_MARKET",
        _ => throw new InvalidCastException($"OrdType {ordType} is not supported")
    };
    
    public static OrdType GetOrdType(this string ordType) => ordType switch
    {
        "MARKET" => OrdType.Market,
        "LIMIT" => OrdType.Limit,
        "STOP_MARKET" => OrdType.StopMarket,
        _ => throw new InvalidCastException($"OrdType {ordType} is not supported")
    };

    public static ExecType GetExecType(this string executionType) => executionType switch
    {
        "NEW" => ExecType.New,
        "CANCELED" => ExecType.Canceled,
        "CALCULATED" => throw new NotImplementedException(), // - Liquidation Execution
        "EXPIRED" => ExecType.Canceled,
        "TRADE" => ExecType.Fill,
        "AMENDMENT" => ExecType.Replaced,
        _ => throw new InvalidCastException($"ExecutionType {executionType} is not supported")
    };

    public static string ToBinanceString(this TimeInForce tif) => tif switch
    {
        TimeInForce.GoodTillCancelled => "GTC",
        _ => throw new InvalidCastException($"TimeInForce {tif} is not supported")
    };
    
    public static TimeInForce ToTimeInForce(this string tif) => tif switch
    {
        "GTC" => TimeInForce.GoodTillCancelled,
        _ => throw new InvalidCastException($"TimeInForce {tif} is not supported")
    };

    public static OrdStatus ToOrdStatus(this string ordStatus) => ordStatus switch
    {
        "NEW" => OrdStatus.New,
        "PARTIALLY_FILLED" => OrdStatus.PartiallyFilled,
        "FILLED" => OrdStatus.Filled,
        "CANCELED" => OrdStatus.Canceled,
        "REJECTED" => OrdStatus.Rejected,
        // EXPIRED
        // EXPIRED_IN_MATCH
        _ => throw new InvalidCastException($"OrdStatus {ordStatus} is not supported")
    };

    public static ExternalExecutionReport ToExternalExecutionReport(this BinanceOrder order, 
        int accountId,
        long? orderId = null,
        ExecType execType = ExecType.OrderStatus, 
        ExecTypeReason? execTypeReason = null,
        RejectReason? rejectReason = null, 
        string? rejectText = null
    ) => new(order.ClientOrderId, orderId, order.OrderId.ToString(CultureInfo.InvariantCulture), accountId, order.Symbol, order.Status.ToOrdStatus(),
            order.Type.GetOrdType(), order.Side.GetSide(), null, null, order.OrigQty, order.ExecutedQty,
            Math.Max(order.OrigQty - order.ExecutedQty, 0),
            order.Price, order.StopPrice, order.TimeInForce.ToTimeInForce(),
            order.GoodTillDate == 0 ? null : Instant.FromUnixTimeMilliseconds(order.GoodTillDate),
            execType, execTypeReason, rejectReason, rejectText, Instant.FromUnixTimeMilliseconds(order.UpdateTime),
            order.CumQuote);

    public static ExternalPositionReport ToExternalPositionReport(this BinancePosition pos, int accountId) =>
        new(accountId, pos.Symbol, pos.PositionAmt, pos.EntryPrice, null);

    public static ExternalTradeRecord ToExternalTradeRecord(this BinanceTrade trade, int accountId) => new(
        trade.Id.ToString(CultureInfo.InvariantCulture),
        trade.OrderId.ToString(CultureInfo.InvariantCulture),
        trade.Symbol,
        accountId,
        trade.Side.GetSide(),
        trade.Qty,
        trade.Price,
        trade.Commission,
        trade.CommissionAsset,
        Instant.FromUnixTimeMilliseconds(trade.Time),
        trade.QuoteQty
    );

    public static RejectReason FromBinanceErrorCode(this int errCode) => errCode switch
    {
        -1111 => RejectReason.IncorrectQuantity, // Precision is over the maximum defined for this asset.
        -1121 => RejectReason.UnknownSymbol, // Invalid symbol.
        -2027 => RejectReason.OrderExceedsLimit, // Exceeded the maximum allowable position at current leverage.
        -4005 => RejectReason.IncorrectQuantity, // Quantity greater than max quantity.
        -4140 => RejectReason.ExchangeClosed, // Invalid symbol status for opening position.
        -4164 => RejectReason.IncorrectQuantity, // Order's notional must be no smaller than 5 (unless you choose reduce only).
        _ => RejectReason.NotSpecified,
    };
}