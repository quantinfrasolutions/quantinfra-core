namespace QuantInfra.Services.LocalTestServer;

public class LocalTestServerConfig
{
    public List<string> StrategyAssembliesPaths { get; init; } = new();
    public List<string> PluginAssembliesPaths { get; init; } = new();
    public List<string> MarketDataPaths { get; init; } = new();
    public string WorkingDirectory { get; set; }
}