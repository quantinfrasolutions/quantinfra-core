namespace QuantInfra.Backtesting.LocalMarketDataStorage;

public class Config
{
    public List<string> MarketDataPaths { get; set; }
    public string DateTimeFormat { get; set; } = "uuuu'-'MM'-'dd' 'HH':'mm':'ss";
}