using System;
using Prometheus;

namespace QuantInfra.Domain.HostedStrategies;

internal static class MetricsDefinition
{
    private static readonly Lazy<Histogram> _onExchangeBarProcessingTime = new(() => Metrics.CreateHistogram(
        "on_exchange_bar_total",
        "Total time spent inside OnExchangeBar, us",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(20, 20, 10) }
    ));
    public static Histogram OnExchangeBarProcessingTime => _onExchangeBarProcessingTime.Value;
    
    private static readonly Lazy<Histogram> _onExchangeBarServiceTime = new(() => Metrics.CreateHistogram(
        "on_exchange_bar_service",
        "Service time inside OnExchangeBar, us",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(20, 20, 10) }
    ));
    public static Histogram OnExchangeBarServiceTime => _onExchangeBarServiceTime.Value;
    
    private static readonly Lazy<Histogram> _onExchangeBarAggTime = new(() => Metrics.CreateHistogram(
        "on_exchange_bar_agg",
        "Bar aggregation time inside OnExchangeBar, us",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(20, 20, 10) }
    ));
    public static Histogram OnExchangeBarAggTime => _onExchangeBarAggTime.Value;
    
    private static readonly Lazy<Histogram> _onExchangeBarStrategiesTime = new(() => Metrics.CreateHistogram(
        "on_exchange_bar_strategies",
        "Strategies calculation time inside OnExchangeBar, us",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(20, 20, 10) }
    ));
    public static Histogram OnExchangeBarStrategiesTime => _onExchangeBarStrategiesTime.Value;
}