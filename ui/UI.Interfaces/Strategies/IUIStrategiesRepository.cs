using QuantInfra.Common.Interfaces.Api.Strategies;
using QuantInfra.Common.Strategies;
using QuantInfra.Common.Strategies.Abstractions;

namespace UI.Interfaces.Strategies;

public interface IUiStrategiesRepository
{
    Task<IEnumerable<StrategyViewBrief>> GetStrategies(StrategiesFilter? filter = null);
    Task CreateStrategy(CreateStrategyRequest strategy);
    Task StartStrategy(int strategyId);
    Task StopStrategy(int strategyId, bool force);
    Task<StrategyViewBrief> GetStrategy(int strategyId);
    Task<StrategyTypeDescription> GetStrategyClassDescription(string className);
    Task<ValidateStrategyResult> ValidateNewStrategy(CreateStrategyRequest request);
}