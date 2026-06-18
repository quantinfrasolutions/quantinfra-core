namespace QuantInfra.Connectors.Binance.Common;

public class BinanceUsdmOrderBookSubscriptionRequest
{
    public string Symbol { get; set; }
    public int ContractId { get; set; }
    public int Frequency { get; set; } = 250;
    public int Levels { get; set; } = 100;
}