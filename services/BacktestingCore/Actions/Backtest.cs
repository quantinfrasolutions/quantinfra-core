using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NodaTime;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Backtesting;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Services.BacktestingCore.Actions;

public record class BacktestOptions
{
    public IReadOnlyCollection<BacktestedStrategyConfig> Strategies { get; init; }
}

public class Backtest : IStrategyTestAction
{
    public string Name => "test";
    
    public void Run(TestUnit unit, ITestExecutorFactory teFactory, IActionProgressTracker tracker, IMetricsCalculator? metricsCalculator, ITestResultsPersister persister)
    {
        var options = unit.GetParams<BacktestOptions>();
        if (options is null) throw new ArgumentException("Options are null");
        
        var executor = teFactory.CreateExecutorInstance(options.Strategies, tracker);
        executor.Run();
        metricsCalculator?.Calculate(executor); // TODO
        persister.PersistResult(unit, executor.GetResult());
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
        var options = new BacktestOptions
        {
            Strategies = [
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
            ]
        };
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