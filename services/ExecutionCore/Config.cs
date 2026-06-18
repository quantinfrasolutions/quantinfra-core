namespace QuantInfra.Services.ExecutionCore;

public class Config
{
    public string ExecutionServiceName { get; set; }
    public bool WritePerformanceMetrics { get; set; }
    public bool EnableHeartbeatsLogging { get; set; }
    /// <summary>
    /// When all components are deployed to a single host, zmq sending vs receiving time will be measured in microseconds
    /// </summary>
    public bool SingleHost { get; set; }

    public bool UseSingleThreadForInputDisruptor { get; set; } = false;
    public bool Monolith { get; set; } = false;
}