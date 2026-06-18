using System.Text.Json.Serialization;

namespace QuantInfra.Binance.Futures.USDM.MarketData;

public partial class OrderBookSnapshot
{
    [JsonIgnore] public int ContractId { get; set; }
}