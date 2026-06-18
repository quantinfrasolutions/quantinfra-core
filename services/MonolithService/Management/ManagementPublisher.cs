using Common.Metrics;
using NodaTime;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.InProcess;
using TransportMessage = QuantInfra.Common.Messaging.InProcess.TransportMessage;

namespace QuantInfra.Services.MonolithService.Management;

public class ManagementPublisher(Topology topology, IClock clock) : IPublisher
{
    private readonly long _sessionId = clock.GetCurrentInstant().ToUnixTimeMilliseconds();
    private long _sequenceNumber = 0;
    
    public void Dispose()
    { }

    public void PublishUnwrappedObject(object o)
    {
        var msg = new TransportMessage("management", MessageType.DataMessage,
            _sessionId, _sequenceNumber++, 
            MetricsUtils.GetUnixMicro(),
            o
        );
        topology.SendTopicMulticastMessage("management", msg);
    }

    public void PublishUnwrappedObjectWithReceiptionSwMicro(object o, long swReceivedAt)
    {
        throw new NotImplementedException();
    }

    public void PublishUnwrappedString(Type type, string typeName, string data)
    {
        throw new NotImplementedException();
    }

    public void PublishWrappedMessage(IMessage message)
    {
        throw new NotImplementedException();
    }
}

public class ManagementPublisherFactory(Topology topology, IClock clock) : IPublisherFactory
{
    private readonly Lazy<ManagementPublisher> _publisher = new(() => new(topology, clock));
    public IPublisher GetPublisher(string name)
    {
        if (name != "management") throw new NotSupportedException();
        return _publisher.Value;
    }
}