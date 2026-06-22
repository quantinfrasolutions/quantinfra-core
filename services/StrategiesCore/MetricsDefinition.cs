using System;
using System.Collections.Generic;
using Prometheus;

namespace QuantInfra.Services.StrategiesCore;

internal static class MetricsDefinition
{
    private static readonly Dictionary<string, Histogram> TotalMarketDataDelays = new();
    public static Histogram GetTotalMarketDataDelay(string serviceName, bool includeServiceName = false,
        int start = 100, int width = 100, int count = 10)
    {
        if (TotalMarketDataDelays.TryGetValue(serviceName, out var hist)) return hist;
        
        hist = Prometheus.Metrics.CreateHistogram(
            includeServiceName ? $"{serviceName}_total_market_data_delay" : "total_market_data_delay",
            "Time difference the source-provided timestamp and start of its processing, ms",
            new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(start, width, count) }
        );
        TotalMarketDataDelays.Add(serviceName, hist);
        return hist;
    }
}