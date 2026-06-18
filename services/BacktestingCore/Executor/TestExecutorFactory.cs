using System.Collections.Generic;
using BacktestingCore.Providers;
using Common.Backtesting;
using Common.Profiling;
using Common.Strategies;
using NLog.Config;
using QuantInfra.Common.StaticData.Abstractions;
using QuantInfra.Common.Strategies.Api;

namespace BacktestingCore.Executor
{
    public class TestExecutorFactory<T> : ITestExecutorFactory<T>
        where T : IStrategyTestResult
    {
        private readonly TestExecutorOptions _options;
        private readonly TestStaticDataRepository _sdProvider;
        private readonly LoggingConfiguration _loggingConfiguration;
        private readonly IProfiler _profiler;
        private readonly ITestResultCalculator<T> _resultCalculator;
        private readonly IHostedStrategiesFactory _strategiesFactory;

        public TestExecutorFactory(
            TestExecutorOptions options,
            TestStaticDataRepository sdProvider,
            LoggingConfiguration loggingConfiguration,
            IProfiler profiler,
            ITestResultCalculator<T> resultCalculator, IHostedStrategiesFactory strategiesFactory)
        {
            _options = options;
            _sdProvider = sdProvider;
            _loggingConfiguration = loggingConfiguration;
            _profiler = profiler;
            _resultCalculator = resultCalculator;
            _strategiesFactory = strategiesFactory;
        }
        
        public ITestExecutor<T> CreateExecutorInstance(IReadOnlyCollection<CreateStrategyRequest> configs, ITestMarketDataProvider candlesStorage, 
            TestExecutorOptions? options = null
        ) => new TestExecutorT<T>(
            options ?? _options,
            candlesStorage, 
            _sdProvider,
            configs,
            _loggingConfiguration,
            _profiler,
            _resultCalculator,
            _strategiesFactory
        );
    }
}
