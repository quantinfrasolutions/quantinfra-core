namespace QuantInfra.Ibkr.Common
{
    public class Config
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 4002;
        public int ClientId { get; set; } = 0;
        public int HeartbeatIntervalSec { get; set; } = 0;
        public int HistoricalDataTimeoutSec { get; set; } = 120;
        public int ReconnectIntervalSec { get; set; } = 10;
        public int ReconnectMaxAttempts { get; set; } = 12; // 2 minutes
        public bool WritePerformanceMetrics { get; set; }
        public int DatasourceId { get; set; }
    }
}