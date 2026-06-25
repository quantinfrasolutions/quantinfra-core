using System.Collections.Generic;
using BacktestingCore.Providers;
using NLog.Config;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Sdk.Backtesting;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Services.BacktestingCore.Providers;

namespace QuantInfra.Services.BacktestingCore.Executor
{
    public class TestExecutorFactory(
        TestExecutorOptions options,
        TestStaticDataRepository sdProvider,
        LoggingConfiguration loggingConfiguration,
        IHostedStrategiesFactory strategiesFactory
    )
        : ITestExecutorFactory
    {
        public IBacktestRunner CreateExecutorInstance(IReadOnlyCollection<StrategyConfig> configs, TestExecutorOptions? optionsOverride = null) => 
            new TestExecutor(
            optionsOverride ?? options,
            candlesStorage, 
            sdProvider,
            configs,
            loggingConfiguration,
            strategiesFactory
        );
    }
}
