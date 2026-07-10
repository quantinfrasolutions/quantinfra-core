namespace QuantInfra.Backtesting.LocalTestServerWrapper;

public class Config
{
    public string StrategyTesterCliPath { get; init; } = "StrategyTesterCli.dll";
    public bool UseEnv { get; init; } = true;
    public string WorkingDirPath { get; set; } = ".";
    public string? ConfigFilePath { get; init; } = null;
    public List<string>? Args { get; set; } = null;
}