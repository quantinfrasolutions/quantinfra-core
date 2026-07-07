// using System;
// using System.Collections.Generic;
// using QuantInfra.Sdk.Backtesting;
//
// namespace QuantInfra.Services.BacktestingCore.Actions;
//
// public record class OptimizationOptions();
//
// public class Optimize : IStrategyTestAction
// {
//     public string Name => "optimize";
//     
//     public void Run(TestUnit unit, ITestExecutorFactory teFactory, IActionProgressTracker tracker,
//         IMetricsCalculator? metricsCalculator, ITestResultsPersister persister)
//     {
//         var options = unit.GetParams<OptimizationOptions>();
//         if (options is null) throw new ArgumentException("Options are null");
//         
//         var executor = teFactory.CreateExecutorInstance(null, tracker);
//         executor.Run();
//         metricsCalculator?.Calculate(executor); // TODO
//         persister.PersistResult(unit, executor.GetResult());
//     }
//
//     public IReadOnlyCollection<MarketDataRequirement> GetMarketDataRequirements(TestUnit unit)
//     {
//         throw new System.NotImplementedException();
//     }
//
//     public ActionParamsValidationResult ValidateParams(string? options, IReadOnlyCollection<string> availableStrategyTypes)
//     {
//         throw new System.NotImplementedException();
//     }
//
//     public string GetSampleParams()
//     {
//         throw new System.NotImplementedException();
//     }
//
//     
// }