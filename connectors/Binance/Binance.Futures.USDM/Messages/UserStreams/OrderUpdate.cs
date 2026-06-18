using System.Globalization;
using System.Text.Json.Serialization;
using NodaTime;
using QuantInfra.Connectors.Binance.Common;
using QuantInfra.Sdk.Trading.ExternalAccounts;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Messages.UserStreams;

public class OrderUpdate : BaseStreamMessage
{
    [JsonPropertyName("T")] public long TransactionTime { get; set; }
    [JsonPropertyName("o")] public OrderData Order { get; set; }

    public ExternalExecutionReport ToExternalExecutionReport(int accountId, long? orderId = null)
    {
        var execType = Order.ExecutionType.GetExecType();
        
        return new(
            Order.ClientOrderId,
            orderId,
            Order.OrderId.ToString(CultureInfo.InvariantCulture),
            accountId,
            Order.Symbol,
            Order.OrderStatus.ToOrdStatus(),
            Order.OrderType.GetOrdType(),
            Order.Side.GetSide(),
            execType == ExecType.Fill ? Order.LastFilledQuantity : null,
            Order.LastFilledPrice != 0 ? Order.LastFilledPrice : null,
            Order.OriginalQuantity,
            Order.FilledAccumulatedQuantity,
            Math.Max(Order.OriginalQuantity - Order.FilledAccumulatedQuantity, 0),
            Order.OriginalPrice != 0 ? Order.OriginalPrice : null,
            Order.StopPrice != 0 ? Order.StopPrice : null,
            Order.TimeInForce.ToTimeInForce(),
            Order.GTD != 0 ? Instant.FromUnixTimeMilliseconds(Order.GTD) : null,
            execType,
            null, null, null, 
            Instant.FromUnixTimeMilliseconds(TransactionTime),
            Order.LastFilledPrice * Order.LastFilledQuantity
        );
    }

    public ExternalTradeRecord ToExternalTradeRecord(int accountId)
    {
        var execType = Order.ExecutionType.GetExecType();
        if (execType != ExecType.Fill) throw new InvalidOperationException($"ExecType is {Order.ExecutionType}");

        return new(
            Order.TradeId.ToString(CultureInfo.InvariantCulture),
            Order.OrderId.ToString(CultureInfo.InvariantCulture),
            Order.Symbol,
            accountId,
            Order.Side.GetSide(),
            Order.LastFilledQuantity,
            Order.LastFilledPrice,
            Order.Commission,
            Order.CommissionAsset,
            Instant.FromUnixTimeMilliseconds(TransactionTime),
            Order.LastFilledPrice * Order.LastFilledQuantity
        );
    }
}

public class OrderData
{
    [JsonPropertyName("s")] public string Symbol { get; set; }
    [JsonPropertyName("c")] public string ClientOrderId { get; set; }   // special client order id:
                                                                        // starts with "autoclose-": liquidation order
                                                                        // "adl_autoclose": ADL auto close order
                                                                        // "settlement_autoclose-": settlement order for delisting or delivery
    [JsonPropertyName("S")] public string Side { get; set; }
    [JsonPropertyName("o")] public string OrderType { get; set; }
    [JsonPropertyName("f")] public string TimeInForce { get; set; }
    [JsonPropertyName("q")] public decimal OriginalQuantity { get; set; }
    [JsonPropertyName("p")] public decimal OriginalPrice { get; set; }
    [JsonPropertyName("ap")] public decimal AveragePrice { get; set; }
    [JsonPropertyName("sp")] public decimal StopPrice { get; set; }     // Stop Price. Please ignore with TRAILING_STOP_MARKET order
    [JsonPropertyName("x")] public string ExecutionType { get; set; }
    [JsonPropertyName("X")] public string OrderStatus { get; set; }
    [JsonPropertyName("i")] public long OrderId { get; set; }
    [JsonPropertyName("l")] public decimal LastFilledQuantity { get; set; }
    [JsonPropertyName("z")] public decimal FilledAccumulatedQuantity { get; set; }
    [JsonPropertyName("L")] public decimal LastFilledPrice { get; set; }
    [JsonPropertyName("N")] public string CommissionAsset { get; set; }
    [JsonPropertyName("n")] public decimal Commission { get; set; }
    [JsonPropertyName("T")] public long OrderTradeTime { get; set; }
    [JsonPropertyName("t")] public long TradeId { get; set; }
    [JsonPropertyName("b")] public decimal BidNotional { get; set; }
    [JsonPropertyName("a")] public decimal AskNotional { get; set; }
    [JsonPropertyName("m")] public bool IsMaker { get; set; }
    [JsonPropertyName("R")] public bool ReduceOnly { get; set; }
    [JsonPropertyName("wt")] public string StopPriceWorkingType { get; set; }
    [JsonPropertyName("ot")] public string OriginalOrderType { get; set; }
    [JsonPropertyName("ps")] public string PositionSide { get; set; }
    [JsonPropertyName("cp")] public bool ConditionalOrder { get; set; } // If Close-All, pushed with conditional order
    [JsonPropertyName("AP")] public decimal ActivationPrice { get; set; } // only puhed with TRAILING_STOP_MARKET order
    [JsonPropertyName("cr")] public decimal CallbackRate { get; set; }  // only puhed with TRAILING_STOP_MARKET order
    [JsonPropertyName("pP")] public bool PriceProtection { get; set; }
    [JsonPropertyName("si")] public int Si { get; set; }                // ignore
    [JsonPropertyName("ss")] public int Ss { get; set; }                // ignore
    [JsonPropertyName("rp")] public decimal RealizedProfit { get; set; }
    [JsonPropertyName("V")] public string StpMode { get; set; }
    [JsonPropertyName("pm")] public string PriceMatchMode { get; set; }
    [JsonPropertyName("gtd")] public long GTD { get; set; }             // TIF GTD order auto cancel time

    public override string ToString()
    {
        return $"{nameof(Symbol)}: {Symbol}, {nameof(ClientOrderId)}: {ClientOrderId}, {nameof(Side)}: {Side}, {nameof(OrderType)}: {OrderType}, {nameof(TimeInForce)}: {TimeInForce}, {nameof(OriginalQuantity)}: {OriginalQuantity}, {nameof(OriginalPrice)}: {OriginalPrice}, {nameof(AveragePrice)}: {AveragePrice}, {nameof(StopPrice)}: {StopPrice}, {nameof(ExecutionType)}: {ExecutionType}, {nameof(OrderStatus)}: {OrderStatus}, {nameof(OrderId)}: {OrderId}, {nameof(LastFilledQuantity)}: {LastFilledQuantity}, {nameof(FilledAccumulatedQuantity)}: {FilledAccumulatedQuantity}, {nameof(LastFilledPrice)}: {LastFilledPrice}, {nameof(CommissionAsset)}: {CommissionAsset}, {nameof(Commission)}: {Commission}, {nameof(OrderTradeTime)}: {OrderTradeTime}, {nameof(TradeId)}: {TradeId}, {nameof(BidNotional)}: {BidNotional}, {nameof(AskNotional)}: {AskNotional}, {nameof(IsMaker)}: {IsMaker}, {nameof(ReduceOnly)}: {ReduceOnly}, {nameof(StopPriceWorkingType)}: {StopPriceWorkingType}, {nameof(OriginalOrderType)}: {OriginalOrderType}, {nameof(PositionSide)}: {PositionSide}, {nameof(ConditionalOrder)}: {ConditionalOrder}, {nameof(ActivationPrice)}: {ActivationPrice}, {nameof(CallbackRate)}: {CallbackRate}, {nameof(PriceProtection)}: {PriceProtection}, {nameof(Si)}: {Si}, {nameof(Ss)}: {Ss}, {nameof(RealizedProfit)}: {RealizedProfit}, {nameof(StpMode)}: {StpMode}, {nameof(PriceMatchMode)}: {PriceMatchMode}, {nameof(GTD)}: {GTD}";
    }
}