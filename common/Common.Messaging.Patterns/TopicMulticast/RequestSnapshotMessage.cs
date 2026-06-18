namespace QuantInfra.Common.Messaging.Patterns.TopicMulticast;

public class RequestSnapshotMessage
{
    protected RequestSnapshotMessage() { }

    public RequestSnapshotMessage(string topic)
    {
        Topic = topic;
    }
    
    public string Topic { get; protected init; }
}