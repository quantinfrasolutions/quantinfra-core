using QuantInfra.Common.Strategies;

namespace UI.Interfaces.Strategies;

public interface IUiStrategyClassesRepository
{
    Task<IEnumerable<StrategyTypeDescription>> GetAvailableStrategyClasses();
}