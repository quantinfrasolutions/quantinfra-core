using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Connectors.Ibkr.Interfaces;

public class EarliestAvailableHistoricalDataRequest
{
    public int ConId { get; set; }
    public string Exchange { get; set; }
    public SubscriptionType Type { get; set; }
    public bool UseRTH { get; set; }
}