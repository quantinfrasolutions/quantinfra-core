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
            _parseWaitTime = MetricsDefinition.GetParseWaitTime(config.ServiceName, config.Monolith,
                config.ParseWaitTimeParams[0], config.ParseWaitTimeParams[1], config.ParseWaitTimeParams[2]);
            _parseTime = MetricsDefinition.GetParseTime(config.ServiceName, config.Monolith,
                config.ParseTimeParams[0], config.ParseTimeParams[1], config.ParseTimeParams[2]);
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
    public string ServiceName { get; set; }
    public bool Monolith { get; set; }
    public bool WritePerformanceMetrics { get; set; }
    
    public int[] ParseWaitTimeParams { get; set; } = [50, 50, 10];
    public int[] ParseTimeParams { get; set; } = [50, 50, 10];
}