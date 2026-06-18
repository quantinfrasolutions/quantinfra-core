namespace QuantInfra.Common.Messaging.InProcess.Messages.TopicMulticast;

public class DownstreamMessage : QuantInfra.Common.Messaging.Patterns.TopicMulticast.DownstreamMessage,
    IInProcessMessage
{
    public DownstreamMessage() { }
    
    internal DownstreamMessage(string senderCompId, string topicName, MessageType messageType, long sessionId, long sequenceNumber, object? content) :
        base(senderCompId, topicName, messageType, sessionId, sequenceNumber, null)
    {
        Data = content;
    }
    
    public new string? Payload { get => base.Payload; set => base.Payload = value; }
    public object? Data { get; set; }
}