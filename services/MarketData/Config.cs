using NodaTime;

namespace QuantInfra.Services.MarketData;

public class Config
{
    public string MarketDataServiceName { get; set; }
    public bool WritePerformanceMetrics { get; set; }
    public Duration ContractPriceUpdateInterval { get; set; } = Duration.FromSeconds(1);
    public bool Monolith { get; set; } = false;
}