using Prometheus;

namespace QuantInfra.Common.ServiceBase;

public static class MetricsDefinition
{
    private static readonly Lazy<Histogram> _parseWaitTime = new(() => Prometheus.Metrics.CreateHistogram(
        "parse_wait_time",
        "Time spent by incoming messages in the input disruptor before being parsed, us",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(20, 20, 10) }
    ));
    public static Histogram ParseWaitTime => _parseWaitTime.Value;
    
    private static readonly Lazy<Histogram> _parseTime = new(() => Prometheus.Metrics.CreateHistogram(
        "parse_time",
        "Time spent for parsing incoming messages, us",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(20, 20, 10) }
    ));
    public static Histogram ParseTime => _parseTime.Value;
    
    private static readonly Lazy<Histogram> _bplDelay = new(() => Prometheus.Metrics.CreateHistogram(
        "business_processing_delay",
        "Delay before receiving a message and passing it to BPL, us",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(20, 20, 10) }
    ));
    public static Histogram BplDelay => _bplDelay.Value;
    
    private static readonly Lazy<Histogram> _bplTime = new(() => Prometheus.Metrics.CreateHistogram(
        "business_processing_time",
        "BPL processing time, us",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(20, 20, 10) }
    ));
    public static Histogram BplTime => _bplTime.Value;
    
    private static readonly Lazy<Histogram> _walWaitTime = new(() => Prometheus.Metrics.CreateHistogram(
        "wal_wait_time",
        "Delay before writing a message to WAL, us",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(20, 20, 10) }
    ));
    public static Histogram WalWaitTime => _walWaitTime.Value;
    
    private static readonly Lazy<Histogram> _walTime = new(() => Prometheus.Metrics.CreateHistogram(
        "wal_time",
        "WAL operations time, us",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(20, 20, 10) }
    ));
    public static Histogram WalTime => _walTime.Value;
    
    private static readonly Lazy<Histogram> _stateTime = new(() => Prometheus.Metrics.CreateHistogram(
        "state_time",
        "State persistence time, us",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(20, 20, 10) }
    ));
    public static Histogram StateTime => _stateTime.Value;
}