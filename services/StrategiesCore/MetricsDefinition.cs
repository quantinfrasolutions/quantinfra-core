using System;
using Prometheus;

namespace StrategiesCore;

internal static class MetricsDefinition
{
    private static readonly Lazy<Histogram> _totalMarketDataDelay = new(() => Metrics.CreateHistogram(
        "total_market_data_delay",
        "Time difference the source-provided timestamp and start of its processing, ms",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(20, 20, 10) }
    ));
    public static Histogram TotalMarketDataDelay => _totalMarketDataDelay.Value;
}