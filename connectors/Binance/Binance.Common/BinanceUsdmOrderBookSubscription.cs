using NodaTime;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Connectors.Binance.Common;

public class BinanceUsdmOrderBookSubscription : IOrderBookSubscription
{
    protected BinanceUsdmOrderBookSubscription() { }
    public BinanceUsdmOrderBookSubscription(string clientName, int subscriptionId, int contractId, string symbol, int frequency,
        int levels)
    {
        if (frequency != 250 && frequency != 500 && frequency != 100)
            throw new NotSupportedException("Only 100, 250, and 500ms frequencies are supported by Binance");
        
        ClientName = clientName;
        SubscriptionId = subscriptionId;
        ContractId = contractId;
        Symbol = symbol;
        Frequency = frequency;
        Levels = levels;
    }

    public int SubscriptionId { get; init; }
    public int ContractId { get; init; }
    public string Symbol { get; init; }
    public int Frequency { get; init; }
    public int Levels { get; init; }
    public string ClientName { get; init; }
    public Instant? LastUpdate { get; set; }
}