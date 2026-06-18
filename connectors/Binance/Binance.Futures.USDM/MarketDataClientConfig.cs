using GenericWebSocketClient;
using QuantInfra.Connectors.Common;

namespace Binance.Futures.USDM;

public class MarketDataClientConfig : BaseConfig
{
    public int DatasourceId { get; set; }
    public int TradingSessionId { get; set; }
    public bool WritePerformanceMetrics { get; set; }
    public bool EnableLogging { get; set; }
    public int ParsersCount { get; set; } = 4;
    public string ClientName { get; set; }
    public string RestUri { get; set; }
}