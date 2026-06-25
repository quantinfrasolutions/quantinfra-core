using QuantInfra.Sdk.Backtesting;

namespace QuantInfra.Services.BacktestingCore.Actions;

public class Backtest : IStrategyTestingAction
{
    public string Name => "test";
    
    public void Run(ITestExecutorFactory teFactory, IMetricsCalculator? metricsCalculator, ITestResultsPersister? persister)
    {
        var executor = teFactory.CreateExecutorInstance();
        executor.Run();
        var results = executor.
    }
}