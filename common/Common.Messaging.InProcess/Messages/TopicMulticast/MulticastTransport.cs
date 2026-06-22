using Common.Metrics;
using Disruptor.Dsl;
using NodaTime;
using QuantInfra.Common.Messaging.Patterns.TopicMulticast;
using QuantInfra.Common.ServiceBase;

namespace QuantInfra.Common.Messaging.InProcess.Messages.TopicMulticast;

public class MulticastTransport : 
    Listener,
    ITransport<QuantInfra.Common.Messaging.Patterns.TopicMulticast.DownstreamMessage>
{
    private readonly string _serverName;
    private readonly Topology _topology;

    public MulticastTransport(string serverName, Topology topology, Disruptor<IncomingDisruptorMessage> disruptor, IClock clock) : base(disruptor, clock)
    {
        _serverName = serverName;
        _topology = topology;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
        _topology.RegisterMulticastControlHandler(_serverName, this);
    }

    public void SendMessage(QuantInfra.Common.Messaging.Patterns.TopicMulticast.DownstreamMessage message)
    {
        message.LogSendingTime(MetricsUtils.GetUnixMicro());
        _topology.SendTopicMulticastMessage(message.TopicName, message);
    }

    public void ReceiveRequestSnapshotMessage(RequestSnapshotMessage message)
    {
        ReceiveMessage(new TransportMessage("MC-CONTROL", MessageType.DataMessage, 0, 0, 0, message));
    }
}