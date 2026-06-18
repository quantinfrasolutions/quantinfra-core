using System.Collections.Generic;
using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Domain.StrategyRecordsStateManager;

public interface IStrategyRecordsStore
{
    Dictionary<int, Strategy> StrategyRecords { get; set; }
}

class DefaultStore : IStrategyRecordsStore
{
    public Dictionary<int, Strategy> StrategyRecords { get; set; } = new();
}