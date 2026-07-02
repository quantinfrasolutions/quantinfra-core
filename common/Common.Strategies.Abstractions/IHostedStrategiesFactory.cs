using QuantInfra.Sdk.Strategies;

namespace QuantInfra.Common.Strategies.Abstractions;

public interface IHostedStrategiesFactory
{
    AbstractHostedStrategy CreateHostedStrategy(Strategy config);
    // AbstractHostedExecutionStrategy CreateHostedExecutionStrategy();
    IEnumerable<StrategyTypeDescription> SupportedStrategyClasses { get; }
}