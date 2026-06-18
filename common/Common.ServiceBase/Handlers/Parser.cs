using Common.Metrics;
using Disruptor;
using Prometheus;
using QuantInfra.Common.Messaging;

namespace QuantInfra.Common.ServiceBase.Handlers;

public class Parser : IEventHandler<IncomingDisruptorMessage>
{
    public readonly IMessageFactory MessageFactory;
    private readonly Histogram? _parseWaitTime;
    private readonly Histogram? _parseTime;

    public Parser(IMessageFactory messageFactory, ParserConfig config)
    {
        MessageFactory = messageFactory;
        if (config?.WritePerformanceMetrics == true)
        {
            _parseWaitTime = MetricsDefinition.ParseWaitTime;
            _parseTime = MetricsDefinition.ParseTime;
        }
    }


    public void OnEvent(IncomingDisruptorMessage data, long sequence, bool endOfBatch)
    {
        if (data.Skip) return;

        if (!string.IsNullOrEmpty(data.TransportMessage?.Payload))
        {
            var now = MetricsUtils.GetUnixMicro();
            if (data.SwReceivedAt != 0 && _parseWaitTime != null) _parseWaitTime.Observe(now - data.SwReceivedAt);
            data.SetParsedMessage(MessageFactory.Parse(data.TransportMessage.Payload));
            if (data.SwReceivedAt != 0 && _parseTime != null) _parseTime.Observe(MetricsUtils.GetUnixMicro() - now);
        }
    }
}

public class ParserConfig
{
    public bool WritePerformanceMetrics { get; set; }
}