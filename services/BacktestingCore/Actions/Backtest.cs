using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Backtesting;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Services.BacktestingCore.Actions;

public record class BacktestOptions(
    IReadOnlyCollection<BacktestedStrategyConfig> Strategies,
    bool SplitMetricsByYear = false,
    bool CalculateTotalMetrics = false
);

public class Backtest : IStrategyTestAction
{
    public string Name => "test";
    
    public void Run(TestUnit unit, ITestExecutorFactory teFactory, IActionProgressTracker tracker, IMetricsCalculator? metricsCalculator, ITestResultsPersister persister)
    {
        var options = unit.GetParams<BacktestOptions>();
        if (options is null) throw new ArgumentException("Options are null");
        
        var executor = teFactory.CreateExecutorInstance(options.Strategies, tracker);
        executor.Run();
        var result = executor.GetResult();
        persister.PersistResult(unit, result);

        if (unit.PersistOptions.SaveMetrics && metricsCalculator != null)
        {
            foreach (var s in result.StrategyConfigs)
                metricsCalculator!.CalculateAndPersist(persister, unit, s.StrategyId, null,
                    result.Trades.Where(t => t.AccountId == s.StrategyId),
                    result.PositionCloses.Where(p => p.AccountId == s.StrategyId),
                    result.Returns.Where(r => r.AccountId == s.StrategyId)
                );
            
            if (options.CalculateTotalMetrics)
                metricsCalculator.CalculateAndPersist(persister, unit, null, null,
                    result.Trades, result.PositionCloses, result.Returns);

            if (options.SplitMetricsByYear)
            {
                var years = Enumerable.Range(unit.Options.StartDt.InUtc().Year, unit.Options.EndDt.InUtc().Year)
                    .ToList();
                if (years.Count > 1)
                {
                    foreach (var year in years)
                    {
                        var start = Instant.FromUtc(year, 1, 1, 0, 0);
                        var end = Instant.FromUtc(year + 1, 12, 31, 23, 59, 59);
                        foreach (var s in result.StrategyConfigs)
                            metricsCalculator.CalculateAndPersist(persister, unit, s.StrategyId, year.ToString(),
                                result.Trades.Where(t => t.AccountId == s.StrategyId && t.Dt >= start && t.Dt < end),
                                result.PositionCloses.Where(p => p.AccountId == s.StrategyId && p.CloseDt >= start && p.CloseDt < end),
                                result.Returns.Where(r => r.AccountId == s.StrategyId && r.Dt >= start && r.Dt < end)
                            );
                        
                        if (options.CalculateTotalMetrics)
                            metricsCalculator.CalculateAndPersist(persister, unit, null, year.ToString(),
                                result.Trades.Where(t => t.Dt >= start && t.Dt < end),
                                result.PositionCloses.Where(p => p.CloseDt >= start && p.CloseDt < end),
                                result.Returns.Where(r => r.Dt >= start && r.Dt < end)
                            );
                    }
                }
            }
        }
    }
    
    public ActionParamsValidationResult ValidateParams(string? options, IReadOnlyCollection<string> availableStrategyTypes)
    {
        if (string.IsNullOrEmpty(options)) return new(false, "Options must contain a field \"Strategies\", containing a collection of BacktestedStrategyConfig");
        try
        {
            var parsedOptions = TestUnit.GetParams<BacktestOptions>(options);
            if (parsedOptions == null) return new(false, "Options must contain a field \"Strategies\", containing a collection of BacktestedStrategyConfig");
            if (parsedOptions.Strategies?.Any() != true) return new(false, "No strategies provided");
            var missingStrategies = parsedOptions.Strategies
                .Where(s => !availableStrategyTypes.Contains(s.ClassName))
                .Select(s => s.ClassName)
                .ToList();
            if (missingStrategies.Any()) return new(false, $"Strategy classes do not exist: {string.Join(',', missingStrategies)}");
            
            return new(true, null);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return new(false, e.Message);
        }
    }
    
    public string GetSampleParams()
    {
        var options = new BacktestOptions([
            new(
                "Optional strategy name", 
                "QuantInfra.Examples.TestStrategy",
                "{ \"MovingAveragePeriod\": 3, \"Direction\": \"Direction.Long\" }",
                new Dictionary<string, BarStorageConfig>()
                {
                    { "main",  new BarStorageConfig() { IdType = IdType.Contract, Id = 12345, Timeframe = Period.FromHours(1) } },
                },
                new Dictionary<string, int>()
                {
                    { "main", 12345 },
                },
                null,
                false,
                840,
                PositionAccounting.Netted,
                true
            )
        ]);
        return TestUnit.SerializeParams(options);
    }
    
    public IReadOnlyCollection<MarketDataRequirement> GetMarketDataRequirements(TestUnit unit)
    {
        var options = unit.GetParams<BacktestOptions>();
        if (options is null) throw new ArgumentException("Options are null");
        
        return options.Strategies.Select(s => new MarketDataRequirement(
            s.AccountCurrencyId,
            s.Symbols.Values.Union(
                s.RequiredBarStorages.Values.Where(bs => bs.IdType == IdType.Contract).Select(bs => bs.Id)
            ).Distinct().ToList(),
            s.RequiredBarStorages.Values.Where(bs => bs.IdType == IdType.Stream).Select(bs => bs.Id).Distinct().ToList()
        )).ToList();
    }
}