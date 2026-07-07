using System;
using System.Collections.Generic;
using System.Linq;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Backtesting;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.Positions;

namespace QuantInfra.Services.BacktestingCore.Metrics;

public record SimpleMetricsOptions(int DaysInYear = 252);

public record BaseMetrics(
    Guid TestId,
    int? StrategyId,
    string? Label,
    double TotalPnL,
    double MaximumDrawdown,
    double SharpeRatio,
    double CalmarRatio,
    double SortinoRatio,
    double RecoveryFactor,
    double AvgTrade,
    double TotalPnLNet,
    double APY,
    double NumTradesPerYear
) : ITestMetrics;

public class SimpleMetricsCalculator : IMetricsCalculator<SimpleMetricsOptions, BaseMetrics>
{
    public string Name => "base";
    
    public BaseMetrics Get(SimpleMetricsOptions options, Guid testId, int? strategyId, string? label,
        IEnumerable<Trade> trades, IEnumerable<Position> positionCloses, IEnumerable<SharePriceHistory> returns)
    {
        var returnsEnumerated = returns.ToList();

        var totalPnL = returnsEnumerated.Count > 0 ? (double)returnsEnumerated.Last().SharePrice - 1 : double.NaN;
        var maximumDrawdown = MaxDrawdown(returnsEnumerated);

        var duration = returnsEnumerated.Count > 1
            ? (returnsEnumerated.Last().Dt - returnsEnumerated.First().Dt).TotalDays / 365
            : double.NaN;
        
        var dailyReturns = returnsEnumerated.Select(r => (double)r.DailyReturn).ToList();
        
        return new(testId, strategyId, label,
            totalPnL,
            maximumDrawdown,
            SharpeRatio(dailyReturns, options.DaysInYear),
            CalmarRatio(totalPnL, maximumDrawdown),
            SortinoRatio(totalPnL, dailyReturns),
            RecoveryFactor(totalPnL, maximumDrawdown),
            Average(positionCloses
                .Select(p => p.OpenPrice == 0 
                    ? double.NaN 
                    : (double)(p.ClosePrice / p.OpenPrice - 1)! * p.Side.GetSign()
                )
                .ToList()
            ),
            totalPnL,
            totalPnL / duration,
            trades.Count() / duration
        );
    }

    public void CalculateAndPersist(ITestResultsPersister persister, TestUnit testUnit, int? strategyId, string? label, IEnumerable<Trade> trades, IEnumerable<Position> positionCloses,
        IEnumerable<SharePriceHistory> returns)
    {
        var options = testUnit.GetMetricsCalculatorOptions<SimpleMetricsOptions>();
        if (options == null) throw new InvalidOperationException("Could not retrieve SimpleMetricsOptions from the test unit");
        var metrics = Get(options, testUnit.TestId, strategyId, label, trades, positionCloses, returns);
        persister.PersistMetrics(testUnit, metrics);
    }
    
    public string GetSampleOptions() => TestUnit.SerializeParams(new SimpleMetricsOptions());
    

    public static double SharpeRatio(IReadOnlyCollection<double> returns, double numOfObservationsPerYear = 252)
    {
        // Algorithmic methodology ref:
        // https://www.quantstart.com/articles/Sharpe-Ratio-for-Algorithmic-Trading-Performance-Measurement/
        return numOfObservationsPerYear == 0
            ? double.NaN
            : Average(returns) / PopulationStandardDeviation(returns) * Math.Sqrt(numOfObservationsPerYear);
    }

    public static double Sum(IReadOnlyCollection<double> list) =>
        list.Count > 0 ? list.Sum() : double.NaN;

    public static double Average(IReadOnlyCollection<double> list)
    {
        return list.Count > 0 ? list.Average() : double.NaN;
    }

    public static double RecoveryFactor(double totalPnL, double maximumDrawdown) =>
        totalPnL / Math.Abs(maximumDrawdown);

    public static double AverageProfit(IReadOnlyCollection<double> returns) =>
        Average(returns.Where(x => x > 0).ToList());

    public static double AverageLoss(IReadOnlyCollection<double> returns) =>
        Average(returns.Where(x => x < 0).ToList());

    public static double TotalPositiveReturns(IReadOnlyCollection<double> returns) =>
        Sum(returns.Where(x => x > 0).ToList());

    public static double TotalNegativeReturns(IReadOnlyCollection<double> returns) =>
        Sum(returns.Where(x => x < 0).ToList());

    public static double ProfitableDaysPercent(IReadOnlyCollection<double> list)
    {
        return list.Count == 0 ? 0 : (double)list.Count(x => x > 0) / list.Count;
    }

    public static double CalmarRatio(double totalPnL, double maximumDrawdown, double riskFreeRate = 0) =>
        (totalPnL - riskFreeRate) / maximumDrawdown;

    public static double SortinoRatio(double totalPnL, IReadOnlyCollection<double> list, double riskFreeRate = 0)
    {
        return (totalPnL - riskFreeRate) / PopulationStandardDeviation(list.Where(x => x < 0).ToList());
    }

    public static double ProfitFactor(IReadOnlyCollection<double> list)
    {
        return Math.Abs(TotalPositiveReturns(list) / TotalNegativeReturns(list));
    }

    public static double MaxDrawdown(IReadOnlyCollection<SharePriceHistory> returns) =>
        (double)returns.Max(x => -Math.Min(x.SharePrice / x.HWM - 1, 0));
    

    public static double MaxDrawDownTime(IReadOnlyList<SharePriceHistory> returns)
    {
        if (returns.Count == 0)  return 0;
        int maxDdDays = 0;
        var maxDdDaysArray = new List<int>(){0};
        for (var i = 1; i < returns.Count; i++)
        {
            if (returns[i].SharePrice < returns[i].HWM)
            {
                maxDdDays++;
            }
            else
            {
                maxDdDaysArray.Add(maxDdDays);
                maxDdDays = 0;
            }
        }   
        
        return maxDdDaysArray.Max() / returns.Count;
    }

    public static double SampleStandardDeviation(IReadOnlyCollection<double> data) =>
        StandardDeviation(data, data.Count - 1);
    
    public static double PopulationStandardDeviation(IReadOnlyCollection<double> data) =>
        StandardDeviation(data, data.Count);
    
    private static double StandardDeviation(IReadOnlyCollection<double> values, int denominator)
    {
        if (values.Count < 2) return double.NaN;

        double mean = values.Average();
        double sum = 0;

        foreach (double x in values)
        {
            double d = x - mean;
            sum += d * d;
        }

        return Math.Sqrt(sum / denominator);
    }
}