namespace QuantInfra.Common.ServiceBase.BPL;

public class BplConfig
{
    public string ServiceName { get; set; }
    public bool SingleHost { get; set; }
    public bool Monolith { get; set; }
    public bool WritePerformanceMetrics { get; set; }
    
    public int[] ReceiveMessageHopHistParams { get; set; } = [100, 100, 10];
    public int[] ProcessingDelayParams { get; set; } = [20, 20, 10];
    public int[] ProcessingTimeParams { get; set; } = [20, 20, 10];
    public int[] BplDelayParams { get; set; } = [20, 20, 10];
    public int[] BplTimeParams { get; set; } = [20, 20, 10];
    public int[] StateTimeParams { get; set; } = [20, 20, 10];
}