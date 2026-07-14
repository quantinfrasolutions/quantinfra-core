namespace QuantInfra.Connectors.Binance.Common;

public class BinanceUsdmOrderBookSubscriptionListView : BinanceUsdmOrderBookSubscription
{
    public BinanceUsdmOrderBookSubscriptionListView() { }
    
    public BinanceUsdmOrderBookSubscriptionListView(BinanceUsdmOrderBookSubscription s, QuantInfra.Sdk.StaticData.Contract contract) 
        : base(s.ClientName, s.SubscriptionId, s.ContractId, s.Symbol, s.Frequency, s.Levels)
    {
        Ticker = contract.Ticker;
    }
    
    public string Ticker { get; init; }
    public bool IsOk { get; set; }
}