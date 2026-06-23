using NodaTime;

namespace QuantInfra.Services.MarketData;

public class Config
{
    public string MarketDataServiceName { get; set; }
    public bool PersistMarketData { get; set; } = true;
    public bool WritePerformanceMetrics { get; set; }
    public Duration ContractPriceUpdateInterval { get; set; } = Duration.FromSeconds(1);
    public bool SingleHost { get; set; } = false;
    public bool Monolith { get; set; } = false;
    
    public int[] ReceiveMessageHopHistParams { get; set; } = [100, 100, 10];
    public int[] ProcessingDelayParams { get; set; } = [20, 20, 10];
    public int[] ProsessingTimeParams { get; set; } = [20, 20, 10];
    public int[] SendingDelayParams { get; set; } = [20, 20, 10];
    public int[] TotalProcessingTimeParams { get; set; } = [100, 100, 10];
    public int[] PersistTimeParams { get; set; } = [100, 100, 10];
}