using QuantInfra.Common.Strategies;
using QuantInfra.Common.Strategies.Abstractions;

namespace UI.Interfaces.Strategies;

public interface IUiStrategyClassesRepository
{
    Task<IEnumerable<StrategyTypeDescription>> GetAvailableStrategyClasses();
}