using Prometheus;

namespace QuantInfra.Common.ServiceBase;

public static class MetricsDefinition
{
    private static readonly Dictionary<string, Histogram> ParseWaitTimes = new();
    public static Histogram GetParseWaitTime(string serviceName, bool includeServiceName = false,
        int start = 20, int width = 20, int count = 10)
    {
        if (ParseWaitTimes.TryGetValue(serviceName, out var hist)) return hist;

        hist = Prometheus.Metrics.CreateHistogram(
            includeServiceName ? $"{serviceName}_parse_wait_time" : "parse_wait_time",
            "Time spent by incoming messages in the input disruptor before being parsed, us",
            new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(start, width, count) }
        );
        ParseWaitTimes[serviceName] = hist;
        return hist;
    }
    
    private static readonly Dictionary<string, Histogram> ParseTimes = new();
    public static Histogram GetParseTime(string serviceName, bool includeServiceName = false,
        int start = 20, int width = 20, int count = 10)
    {
        if (ParseTimes.TryGetValue(serviceName, out var hist)) return hist;

        hist = Prometheus.Metrics.CreateHistogram(
            includeServiceName ? $"{serviceName}_parse_time" : "parse_time",
            "Time spent for parsing incoming messages, us",
            new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(start, width, count) }
        );
        ParseTimes[serviceName] = hist;
        return hist;
    }
    
    private static readonly Dictionary<string, Histogram> BplDelays = new();
    public static Histogram GetBplDelay(string serviceName, bool includeServiceName = false,
        int start = 20, int width = 20, int count = 10)
    {
        if (BplDelays.TryGetValue(serviceName, out var hist)) return hist;

        hist = Prometheus.Metrics.CreateHistogram(
            includeServiceName ? $"{serviceName}_business_processing_delay" : "business_processing_delay",
            "Delay before receiving a message and passing it to BPL, us",
            new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(start, width, count) }
        );
        BplDelays[serviceName] = hist;
        return hist;
    }
    
    private static readonly Dictionary<string, Histogram> BplTimes = new();
    public static Histogram GetBplTime(string serviceName, bool includeServiceName = false,
        int start = 20, int width = 20, int count = 10)
    {
        if (BplTimes.TryGetValue(serviceName, out var hist)) return hist;

        hist = Prometheus.Metrics.CreateHistogram(
            includeServiceName ? $"{serviceName}_business_processing_time" : "business_processing_time",
            "BPL processing time, us",
            new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(start, width, count) }
        );
        BplTimes[serviceName] = hist;
        return hist;
    }
    
    private static readonly Dictionary<string, Histogram> WalWaitTimes = new();
    public static Histogram GetWalWaitTime(string serviceName, bool includeServiceName = false,
        int start = 20, int width = 20, int count = 10)
    {
        if (WalWaitTimes.TryGetValue(serviceName, out var hist)) return hist;

        hist = Prometheus.Metrics.CreateHistogram(
            includeServiceName ? $"{serviceName}_wal_wait_time" : "wal_wait_time",
            "Delay before writing a message to WAL, us",
            new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(start, width, count) }
        );
        WalWaitTimes[serviceName] = hist;
        return hist;
    }
    
    private static readonly Dictionary<string, Histogram> WalTimes = new();
    public static Histogram GetWalTime(string serviceName, bool includeServiceName = false,
        int start = 20, int width = 20, int count = 10)
    {
        if (WalTimes.TryGetValue(serviceName, out var hist)) return hist;

        hist = Prometheus.Metrics.CreateHistogram(
            includeServiceName ? $"{serviceName}_wal_time" : "wal_time",
            "WAL operations time, us",
            new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(start, width, count) }
        );
        WalTimes[serviceName] = hist;
        return hist;
    }
    
    private static readonly Dictionary<string, Histogram> StateTimes = new();
    public static Histogram GetStateTime(string serviceName, bool includeServiceName = false,
        int start = 20, int width = 20, int count = 10)
    {
        if (StateTimes.TryGetValue(serviceName, out var hist)) return hist;

        hist = Prometheus.Metrics.CreateHistogram(
            includeServiceName ? $"{serviceName}_state_time" : "state_time",
            "State persistence time, us",
            new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(start, width, count) }
        );
        StateTimes[serviceName] = hist;
        return hist;
    }
}