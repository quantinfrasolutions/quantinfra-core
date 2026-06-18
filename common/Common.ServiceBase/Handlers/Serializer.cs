using System.Runtime.CompilerServices;
using Common.Metrics;
using Disruptor;
using Prometheus;
using QuantInfra.Common.Messaging;

namespace QuantInfra.Common.ServiceBase.Handlers;

public abstract class Serializer : IEventHandler<IncomingDisruptorMessage>
{
    private readonly Histogram? _parseWaitTime;
    private readonly Histogram? _parseTime;

    public Serializer()
    {
        // if (config?.WritePerformanceMetrics == true)
        // {
        //     _parseWaitTime = MetricsDefinition.ParseWaitTime;
        //     _parseTime = MetricsDefinition.ParseTime;
        // }
    }


    public void OnEvent(IncomingDisruptorMessage data, long sequence, bool endOfBatch)
    {
        if (data.Skip) return;

        if (data.TransportMessage != null)
        {
            if (data.IsReplay)
            {
                if (!string.IsNullOrEmpty(data.TransportMessage?.Payload))
                    data.SetParsedMessage(ParseMessage(data.TransportMessage));
            }
            else
            {
                data.SetParsedMessage(SerializeMessage(data.TransportMessage));
            }
        }
    }

    protected abstract object? ParseMessage(ITransportMessage message);
    protected abstract object? SerializeMessage(ITransportMessage msg);
}

// public class ParserConfig
// {
//     public bool WritePerformanceMetrics { get; set; }
// }