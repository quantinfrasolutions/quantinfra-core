using QuantInfra.Connectors.Common;

namespace QuantInfra.Connectors.Binance.Futures.Usdm;

public class MarketDataClientConfig : BaseConfig
{
    public int DatasourceId { get; set; }
    public int TradingSessionId { get; set; }
    public bool WritePerformanceMetrics { get; set; }
    public bool EnableLogging { get; set; }
    public int ParsersCount { get; set; } = 4;
    public string ClientName { get; set; }
    public string RestUri { get; set; }
    public int[] ReceiveBarDelayParams { get; set; } = [50, 50, 10];
    public int[] ReceiveObDelayParams { get; set; } = [50, 50, 10];
    public int[] ReceiveClosedBarDelayParams { get; set; } = [50, 50, 10];
    
}