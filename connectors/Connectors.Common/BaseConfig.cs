namespace QuantInfra.Connectors.Common;

public class BaseConfig
{
    public string Uri { get; set; }
    public int BufferSize { get; set; } = 65536;
    public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromHours(24);
    public TimeSpan KeepAliveTimeout { get; set; } = TimeSpan.FromMinutes(4);
}