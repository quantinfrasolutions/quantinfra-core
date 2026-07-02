using System.Collections.Generic;
using BacktestingCore.Providers;
using NLog.Config;
using QuantInfra.Common.Backtesting.Abstractions;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Sdk.Backtesting;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Services.BacktestingCore.Providers;

namespace QuantInfra.Services.BacktestingCore.Executor
{
    public class TestExecutorFactory(
        TestExecutorOptions options,
        ITestMarketDataProvider candlesStorage,
        TestStaticDataRepository sdProvider,
        LoggingConfiguration loggingConfiguration,
        IHostedStrategiesFactory strategiesFactory
    ) : ITestExecutorFactory
    {
        private int _strategyId = 10000;
        
        public IBacktestRunner CreateExecutorInstance(IReadOnlyCollection<BacktestedStrategyConfig> configs, IActionProgressTracker? tracker = null, TestExecutorOptions? optionsOverride = null) => 
            new TestExecutor(
                this,
                optionsOverride ?? options,
                candlesStorage, 
                sdProvider,
                configs,
                loggingConfiguration,
                strategiesFactory,
                tracker
            );
        
        public int GetNewStrategyId() => _strategyId++;
    }
}
