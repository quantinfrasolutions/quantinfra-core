using Common.Accounts.Abstractions;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Common.Interfaces.Api.Strategies;

public class CreateStrategyRequest
{
    public string Name { get; set; }
    public string ClassName { get; set; }
    public string Params { get; set; }
    public Dictionary<string, BarStorageConfig> RequiredBarStorages { get; set; }
    public Dictionary<string, int> Symbols { get; set; }
    public LiquidationParameters? LiquidationParameters { get; set; }
    public CreateAccountRequest Account { get; set; }
    public bool StartImmediately { get; set; }
    public bool UseSignalGroups { get; set; }
    public string StrategyServiceName { get; set; }

    public StrategyConfig ToStrategyConfig(int strategyId = 0) => new StrategyConfig(
        strategyId, 
        string.IsNullOrEmpty(Name) ? string.Empty : Name,
        ClassName,
        Params,
        RequiredBarStorages,
        Symbols,
        LiquidationParameters, 
        UseSignalGroups
    );
}