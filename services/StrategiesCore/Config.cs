namespace QuantInfra.Services.StrategiesCore;

public class Config
{
    public string StrategiesServiceName { get; set; }
    public string AccountsServiceName { get; set; }
    public bool WritePerformanceMetrics { get; set; }
    public int AccountServiceTimeout { get; set; } = 10000;
    public bool EnableHeartbeatsLogging { get; set; } = false;
    /// <summary>
    /// When all components are deployed to a single host, zmq sending vs receiving time will be measured in microseconds
    /// </summary>
    public bool SingleHost { get; set; } = false;

    public bool UseSingleThreadForInputDisruptor { get; set; } = false;
    public bool Monolith { get; set; } = false;
}