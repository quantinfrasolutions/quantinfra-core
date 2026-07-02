using Microsoft.Extensions.Logging;

namespace QuantInfra.Services.BacktestingCore.Executor;

public class Config
{
    public LogLevel LogLevel { get; set; } = LogLevel.None;
}