using System.Text.Json.Serialization;

namespace QuantInfra.Connectors.Binance.Common;

public class BinanceOrder
{
    [JsonPropertyName("orderId")] public long OrderId { get; set; }
    [JsonPropertyName("clientOrderId")] public string ClientOrderId { get; set; }
    [JsonPropertyName("symbol")] public string Symbol { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("side")] public string Side { get; set; }
    [JsonPropertyName("origQty")] public decimal OrigQty { get; set; }
    [JsonPropertyName("origType")] public string OrigType { get; set; }
    [JsonPropertyName("price")] public decimal Price { get; set; }
    [JsonPropertyName("stopPrice")] public decimal StopPrice { get; set; }          // please ignore when order type is TRAILING_STOP_MARKET
    [JsonPropertyName("activatePrice")] public decimal ActivatePrice { get; set; }  // activation price, only return with TRAILING_STOP_MARKET order
    [JsonPropertyName("priceRate")] public decimal PriceRate { get; set; }          // callback rate, only return with TRAILING_STOP_MARKET order
    [JsonPropertyName("positionSide")] public string PositionSide { get; set; }
    [JsonPropertyName("reduceOnly")] public bool ReduceOnly { get; set; }
    [JsonPropertyName("closePosition")] public bool ClosePosition { get; set; }     // if Close-All
    [JsonPropertyName("time")] public long Time { get; set; }                       // order time
    [JsonPropertyName("status")] public string Status { get; set; }
    [JsonPropertyName("updateTime")] public long UpdateTime { get; set; }			// update time
    
    [JsonPropertyName("executedQty")] public decimal ExecutedQty { get; set; }
    [JsonPropertyName("avgPrice")] public decimal AvgPrice { get; set; }
    [JsonPropertyName("cumQuote")] public decimal CumQuote { get; set; }
    [JsonPropertyName("timeInForce")] public string TimeInForce { get; set; }
    [JsonPropertyName("workingType")] public string WorkingType { get; set; }
    [JsonPropertyName("priceProtect")] public bool PriceProtect { get; set; }		// if conditional order trigger is protected
    [JsonPropertyName("priceMatch")] public string PriceMatch { get; set; }			// price match mode
    [JsonPropertyName("selfTradePreventionMode")] public string SelfTradePreventionMode { get; set; }	//self trading preventation mode
    [JsonPropertyName("goodTillDate")] public long GoodTillDate { get; set; }		//order pre-set auot cancel time for TIF GTD order

    public override string ToString()
    {
        return $"{nameof(OrderId)}: {OrderId}, {nameof(ClientOrderId)}: {ClientOrderId}, {nameof(Symbol)}: {Symbol}, {nameof(Type)}: {Type}, {nameof(Side)}: {Side}, {nameof(OrigQty)}: {OrigQty}, {nameof(OrigType)}: {OrigType}, {nameof(Price)}: {Price}, {nameof(StopPrice)}: {StopPrice}, {nameof(ActivatePrice)}: {ActivatePrice}, {nameof(PriceRate)}: {PriceRate}, {nameof(PositionSide)}: {PositionSide}, {nameof(ReduceOnly)}: {ReduceOnly}, {nameof(ClosePosition)}: {ClosePosition}, {nameof(Time)}: {Time}, {nameof(Status)}: {Status}, {nameof(UpdateTime)}: {UpdateTime}, {nameof(ExecutedQty)}: {ExecutedQty}, {nameof(AvgPrice)}: {AvgPrice}, {nameof(CumQuote)}: {CumQuote}, {nameof(TimeInForce)}: {TimeInForce}, {nameof(WorkingType)}: {WorkingType}, {nameof(PriceProtect)}: {PriceProtect}, {nameof(PriceMatch)}: {PriceMatch}, {nameof(SelfTradePreventionMode)}: {SelfTradePreventionMode}, {nameof(GoodTillDate)}: {GoodTillDate}";
    }
}