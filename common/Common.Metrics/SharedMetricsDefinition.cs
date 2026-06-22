using Prometheus;

namespace QuantInfra.Common.Metrics;

public static class SharedMetricsDefinition
{
    private static readonly Dictionary<string, Histogram> ReceiveMessageHops = new();
    public static Histogram GetIncomingMessageHop(string serviceName, bool isSingleHost,
        bool includeServiceName = false,
        int start = 100, int width = 100, int count = 10)
    {
        if (ReceiveMessageHops.TryGetValue(serviceName, out var hist)) return hist;

        hist = Prometheus.Metrics.CreateHistogram(
            includeServiceName ? $"{serviceName.Replace('-', '_')}_receive_message_hop" : "receive_message_hop",
            "Time difference between upstream sending a message and the component receiving it, " + (isSingleHost ? "us" : "ms"),
            new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(start, width, count) }
        );
        ReceiveMessageHops[serviceName] = hist;
        return hist;
    }
    
    private static readonly Dictionary<string, Histogram> ProcessingDelays = new();
    public static Histogram GetProcessingDelayHistogram(string serviceName, bool includeServiceName = false,
        int start = 20, int width = 20, int count = 10)
    {
        if (ProcessingDelays.TryGetValue(serviceName, out var hist)) return hist;
        
        hist = Prometheus.Metrics.CreateHistogram(
            includeServiceName ? $"{serviceName.Replace('-', '_')}_processing_delay" : "processing_delay",
            "Input disruptor waiting time, us",
            new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(start, width, count) }
        );
        ProcessingDelays.Add(serviceName, hist);
        return hist;
    }
    
    private static readonly Dictionary<string, Histogram> ProcessingTimes = new();
    public static Histogram GetProcessingTime(string serviceName, bool includeServiceName = false,
        int start = 20, int width = 20, int count = 10)
    {
        if (ProcessingTimes.TryGetValue(serviceName, out var hist)) return hist;
        
        hist = Prometheus.Metrics.CreateHistogram(
            includeServiceName ? $"{serviceName.Replace('-', '_')}_processing_time" : "processing_time",
            "Time between input and output disruptor, us",
            new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(start, width, count) }
        );
        ProcessingTimes.Add(serviceName, hist);
        return hist;
    }
    
    private static readonly Dictionary<string, Histogram> SendingDelays = new();
    public static Histogram GetSendingDelay(string serviceName, bool includeServiceName = false,
        int start = 20, int width = 20, int count = 10)
    {
        if (SendingDelays.TryGetValue(serviceName, out var hist)) return hist;
        
        hist = Prometheus.Metrics.CreateHistogram(
            includeServiceName ? $"{serviceName.Replace('-', '_')}_sending_delay" : "sending_delay",
            "Output disruptor waiting time, us",
            new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(start, width, count) }
        );
        SendingDelays.Add(serviceName, hist);
        return hist;
    }
    
    
    private static readonly Dictionary<string, Histogram> TotalProcessingTimes = new();
    public static Histogram GetTotalProcessingTime(string serviceName, bool includeServiceName = false,
        int start = 100, int width = 100, int count = 10)
    {
        if (TotalProcessingTimes.TryGetValue(serviceName, out var hist)) return hist;
        
        hist = Prometheus.Metrics.CreateHistogram(
            includeServiceName ? $"{serviceName.Replace('-', '_')}_total_processing_time" : "total_processing_time",
            "Time between receiving a message and sending it downstream, us",
            new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(start, width, count) }
        );
        TotalProcessingTimes.Add(serviceName, hist);
        return hist;
    }
    
    private static readonly Dictionary<string, Counter> DownstreamSenderMessages = new();
    public static Counter GetDownstreamSenderMessages(string serviceName, bool includeServiceName = false)
    {
        if (DownstreamSenderMessages.TryGetValue(serviceName, out var cnt)) return cnt;
        
        cnt = Prometheus.Metrics.CreateCounter(
            includeServiceName ? $"{serviceName.Replace('-', '_')}_downstream_sender_processed_messages" : "downstream_sender_processed_messages",
            "Number of messages processed by the downstream sender",
            new CounterConfiguration()
        );
        DownstreamSenderMessages.Add(serviceName, cnt);
        return cnt;
    }
    
    private static readonly Dictionary<string, Counter> PersistedMessages = new();
    public static Counter GetPersistedMessages(string serviceName, bool includeServiceName = false)
    {
        if (PersistedMessages.TryGetValue(serviceName, out var cnt)) return cnt;
        
        cnt = Prometheus.Metrics.CreateCounter(
            includeServiceName ? $"{serviceName.Replace('-', '_')}_persisted_messages" : "persisted_messages",
            "Number of persisted events",
            new CounterConfiguration()
        );
        PersistedMessages.Add(serviceName, cnt);
        return cnt;
    }
    
    private static readonly Dictionary<string, Counter> NumberOfCommits = new();
    public static Counter GetNumberOfCommits(string serviceName, bool includeServiceName = false)
    {
        if (NumberOfCommits.TryGetValue(serviceName, out var cnt)) return cnt;
        
        cnt = Prometheus.Metrics.CreateCounter(
            includeServiceName ? $"{serviceName.Replace('-', '_')}_number_of_commits" : "number_of_commits",
            "Number of database commits",
            new CounterConfiguration()
        );
        NumberOfCommits.Add(serviceName, cnt);
        return cnt;
    }
    
    
    private static readonly Dictionary<string, Histogram> PersistTimes = new();
    public static Histogram GetPersistTime(string serviceName, bool includeServiceName = false,
        int start = 100, int width = 100, int count = 10)
    {
        if (PersistTimes.TryGetValue(serviceName, out var hist)) return hist;
        
        hist = Prometheus.Metrics.CreateHistogram(
            includeServiceName ? $"{serviceName.Replace('-', '_')}_persist_time" : "persist_time",
            "Time required to persist events, us",
            new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(start, width, count) }
        );
        PersistTimes.Add(serviceName, hist);
        return hist;
    }
}