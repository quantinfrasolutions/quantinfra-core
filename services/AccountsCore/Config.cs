using Microsoft.Extensions.Logging;
using NodaTime;

namespace QuantInfra.Services.AccountsCore;

public class Config
{
    public string AccountServiceName { get; set; }
    public Duration HeartbeatInterval { get; set; } = Duration.FromSeconds(1);
    public bool WritePerformanceMetrics { get; set; }
    public Duration MtmUtcOffset { get; set; } = Duration.Zero;
    public Duration MtmJobDelay { get; set; } = Duration.FromMinutes(1);
    public bool PersistEventsAndProjections { get; set; } = true;
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    public int HydrationBatchSize { get; set; } = 10000;
    public bool DisableLoggingOnReplay { get; set; } = true;
    public bool EnableEventFlowValidation { get; set; } = false;
    /// <summary>
    /// When all components are deployed to a single host, zmq sending vs receiving time will be measured in microseconds
    /// </summary>
    public bool SingleHost { get; set; } = false;
    
    public bool Monolith { get; set; } = false;
    public bool UseSingleThread { get; set; } = false;
    
    public int[] ReceiveMessageHopHistParams { get; set; } = [100, 100, 10];
    public int[] ProcessingDelayParams { get; set; } = [20, 20, 10];
    public int[] ProcessingTimeParams { get; set; } = [20, 20, 10];
    public int[] PersistTimeParams { get; set; } = [100, 100, 10];
    public int[] BplDelayParams { get; set; } = [20, 20, 10];
    public int[] BplTimeParams { get; set; } = [20, 20, 10];
    public int[] StateTimeParams { get; set; } = [20, 20, 10];
}