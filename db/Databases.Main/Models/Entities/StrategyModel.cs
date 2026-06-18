using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Databases.Main.Models.Entities;

public class StrategyModel : Strategy
{
    public StrategyModel() { }

    public StrategyModel(int strategyId, string name, string className, string? @params,
        IReadOnlyDictionary<string, BarStorageConfig>? barStorages, IReadOnlyDictionary<string, int>? symbols,
        LiquidationParameters? liquidationParameters, bool useSignalGroups,
        StrategyStatus status, int accountId, string? strategyServiceName,
        AccountModel account
    ) : base(strategyId, name, className, @params, barStorages, symbols, liquidationParameters, useSignalGroups,
        status, accountId, strategyServiceName)
    {
        Account = account;
    }
        
    public AccountModel Account { get; init; }
}