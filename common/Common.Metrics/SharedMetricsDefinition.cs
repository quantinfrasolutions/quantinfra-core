using Prometheus;

namespace QuantInfra.Common.Metrics;

public static class SharedMetricsDefinition
{
    private static readonly Lazy<Histogram> _receiveBarHop = new(() => Prometheus.Metrics.CreateHistogram(
        "receive_bar_hop",
        "Time difference between upstream sending a message and the component receiving it, ms",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(100, 100, 10) }
    ));
    public static Histogram ReceiveBarHop => _receiveBarHop.Value;
    
    private static readonly Lazy<Histogram> _processingDelay = new(() => Prometheus.Metrics.CreateHistogram(
        "processing_delay",
        "Input disruptor waiting time, us",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(20, 20, 10) }
    ));
    public static Histogram ProcessingDelay => _processingDelay.Value;
			
    private static readonly Lazy<Histogram> _processingTime  = new(() => Prometheus.Metrics.CreateHistogram(
        "processing_time",
        "Time between input and output disruptor, us",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(10, 10, 10) }
    ));
    public static Histogram ProcessingTime => _processingTime.Value;
    
    private static readonly Lazy<Histogram> _sendingDelay = new(() => Prometheus.Metrics.CreateHistogram(
        "sending_delay",
        "Output disruptor waiting time, us",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(20, 20, 10) }
    ));
    public static Histogram SendingDelay => _sendingDelay.Value;
    
    private static readonly Lazy<Histogram> _totalProcessingTime  = new(() => Prometheus.Metrics.CreateHistogram(
        "total_processing_time",
        "Time between receiving a message and sending it downstream, us",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(100, 100, 10) }
    ));
    public static Histogram TotalProcessingTime => _totalProcessingTime.Value;
    
    private static readonly Lazy<Counter> _downstreamSenderMessages = new(() => Prometheus.Metrics.CreateCounter(
        "downstream_sender_processed_messages",
        "Number of messages processed by the downstream sender",
        new CounterConfiguration()
    ));
    public static Counter DownstreamSenderMessages => _downstreamSenderMessages.Value;
    
    private static readonly Lazy<Counter> _persistedMessages = new(() => Prometheus.Metrics.CreateCounter(
        "persisted_messages",
        "Number of persisted events",
        new CounterConfiguration()
    ));
    public static Counter PersistedMessages => _persistedMessages.Value;
    
    private static readonly Lazy<Counter> _numberOfCommits = new(() => Prometheus.Metrics.CreateCounter(
        "number_of_commits",
        "Number of database commits",
        new CounterConfiguration()
    ));
    public static Counter NumberOfCommits => _numberOfCommits.Value;
    
    private static readonly Lazy<Histogram> _persistTime  = new(() => Prometheus.Metrics.CreateHistogram(
        "persist_time",
        "Time required to persist events, us",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(100, 100, 10) }
    ));
    public static Histogram PersistTime => _persistTime.Value;
}