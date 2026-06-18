using QuantInfra.Common.Interfaces.Api.Accounts;
using QuantInfra.Common.Interfaces.Api.MarketData;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Common.Interfaces.Api.Strategies;

public class StrategyViewBrief
{
    public StrategyViewBrief(int strategyId, string name, string strategyClassName,
        string @params, IReadOnlyDictionary<string, BriefView<int>> symbols,
        IReadOnlyDictionary<string, BarStorageView> requiredBarStorages, StrategyStatus status, bool useSignalGroups, AccountViewBrief account,
        BriefView<int> currency, string strategyServiceName, LiquidationParameters? liquidationParameters)
    {
        StrategyId = strategyId;
        Name = name;
        StrategyClassName = strategyClassName;
        Params = @params;
        Symbols = symbols;
        RequiredBarStorages = requiredBarStorages;
        Status = status;
        UseSignalGroups = useSignalGroups;
        Account = account;
        Currency = currency;
        StrategyServiceName = strategyServiceName;
        LiquidationParameters = liquidationParameters;
    }

    public int StrategyId { get; init; }
    public string Name { get; init; }
    public string StrategyClassName { get; init; }
    public string StrategyServiceName { get; init; }
    public LiquidationParameters? LiquidationParameters { get; init; }
    public string Params { get; init; }
    public IReadOnlyDictionary<string, BriefView<int>> Symbols { get; init; }
    public IReadOnlyDictionary<string, BarStorageView> RequiredBarStorages { get; init; }
    public StrategyStatus Status { get; init; }
    public bool UseSignalGroups { get; init; }
    public AccountViewBrief Account { get; init; }
    public BriefView<int> Currency { get; init; }
    
    public TParams? DeserializeParams<TParams>() where TParams : class
        => StrategyConfig.DeserializeParams<TParams>(Params);
}