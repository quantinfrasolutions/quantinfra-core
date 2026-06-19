namespace QuantInfra.Services.MonolithService;

public class Config
{
    /// <summary>
    /// Names of Strategies Services to run. Leave empty to run all.
    /// </summary>
    public List<string> EnabledStrategiesServices { get; set; } = new();
    
    /// <summary>
    /// Names of Strategies Services to disable.
    /// </summary>
    public List<string> DisabledStrategiesServices { get; set; } = new();
    
    /// <summary>
    /// Names of Execution Services to run. Leave empty to run all.
    /// </summary>
    public List<string> EnabledExecutionServices { get; set; } = new();
    
    /// <summary>
    /// Names of Strategies Services to disable.
    /// </summary>
    public List<string> DisabledExecutionServices { get; set; } = new();
    
    /// <summary>
    /// Paths to assemblies containing custom strategies and indicators.
    /// </summary>
    public List<string> StrategyDllPaths { get; set; } = new();
    
    /// <summary>
    /// Enable if you use constant value streams.
    /// </summary>
    public bool EnableMarketDataService { get; set; }
    
    /// <summary>
    /// Enable if you use any market data distributed through Binance Futures /market endpoint (candles, aggregated trades)
    /// </summary>
    public bool EnableBinanceUsdmMarketDataService { get; set; }
    
    /// <summary>
    /// Enable if you use any market data distributed through Binance Futures /public endpoint (order books) 
    /// </summary>
    public bool EnableBinanceUsdmPublicMarketDataService { get; set; }
    
    /// <summary>
    /// Working directory for storing application data
    /// </summary>
    public string WorkingDirPath { get; set; } = ".";
}