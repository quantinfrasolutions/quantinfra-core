// using System;
// using System.Collections.Generic;
// using BacktestingCore.Providers;
//
// namespace QuantInfra.Services.BacktestingCore.Executor
// {
//     public class TestExecutorT<TResult>: TestExecutor, IStrategyTestingAction<TResult> 
//         where TResult:IStrategyTestResult
//     {
//         private readonly ITestResultCalculator<TResult> _resultCalculator;
//         private List<TResult> _testResults = null;
//
//         public TestExecutorT(
//             TestExecutorOptions options,
//             ITestMarketDataProvider candlesStorage,
//             TestStaticDataRepository staticDataProvider,
//             IReadOnlyCollection<CreateStrategyRequest> strategies,
//             LoggingConfiguration logConfiguration,
//             IProfiler profiler,
//             ITestResultCalculator<TResult> resultCalculator,
//             IHostedStrategiesFactory strategiesFactory
//         ) : base(options, candlesStorage, staticDataProvider, strategies, logConfiguration, profiler, strategiesFactory
//         )
//         {
//             _resultCalculator = resultCalculator;
//         }
//
//
//         public override void Run()
//         {
//             base.Run();
//             _testResults = (List<TResult>)CalculateTestResults(Options.DaysInYear, Options.SkipOutliers);
//         }
//         
//
//         public IEnumerable<TResult> CalculateTestResults(int daysInYear = 252, int skipResults = 0, double h0mean = 0D)
//         {
//             throw new NotImplementedException();
//             // var positionCloses = Results.PositionCloseCloses
//             //     .GroupBy(p => p.AccountId)
//             //     .ToDictionary(
//             //         gr => gr.Key,
//             //         gr => gr.OrderBy(p => p.CloseDt).ToList()
//             //     );
//             //
//             // var trades = Results.Trades
//             //     .GroupBy(p => p.AccountId)
//             //     .ToDictionary(
//             //         gr => gr.Key,
//             //         gr => gr.OrderBy(p => p.Dt).ToList()
//             //     );
//             //
//             // var returns = Results.Returns;
//             //
//             // // returns always contain records for all strategies, even if there were no trades
//             // return returns
//             //     .Select(kv => _resultCalculator.Calculate(
//             //         kv.Key,
//             //         kv.Key, // TODO
//             //         kv.Value,
//             //         positionCloses.TryGetValue(kv.Key, out var closes) ? closes : new List<Position>(),
//             //         trades.TryGetValue(kv.Key, out var t) ? t : new List<Trade>(),
//             //         daysInYear: daysInYear,
//             //         skipResults: skipResults,
//             //         h0mean: h0mean
//             //     ))
//             //     .ToList();
//         }
//         
//         public void PersistTestResults(ITestResultPersister<TResult>? persister = null, PersistOptions? options = null, bool flush = true)
//         {
//             options ??= new PersistOptions();
//             if (options.SaveTestResults) persister?.SaveTestResults();
//             base.PersistTestResults(persister, options, flush);
//         }
//         
//         public IEnumerable<TResult> GetTestResults() => _testResults;
//     }
// }