using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("Tests.Integration.MessagingPatterns")]

namespace QuantInfra.Common.Messaging.Patterns.TopicMulticast;

public class DownstreamMessage : ITransportMessage
{
    public const string ResponsesTopicName = "responses";
    public string TopicName { get; protected set; }
    public string SenderCompId { get; internal protected set; }
    public MessageType MessageType { get; protected set; }
    public long SessionId { get; protected set; }
    public long SequenceNumber { get; protected set; }
    public long SendingTimestamp { get; protected set; }
    public string? Payload { get; protected set; }
    

    protected DownstreamMessage(string senderCompId, string topicName, MessageType messageType, long sessionId,
        long sequenceNumber, string? payload)
    {
        TopicName = topicName;
        SenderCompId = senderCompId;
        MessageType = messageType;
        SessionId = sessionId;
        SequenceNumber = sequenceNumber;
        Payload = payload;
    }

    protected DownstreamMessage(DownstreamMessage message)
    {
        TopicName = message.TopicName;
        SenderCompId = message.SenderCompId;
        MessageType = message.MessageType;
        SessionId = message.SessionId;
        SequenceNumber = message.SequenceNumber;
        Payload = message.Payload;
    }
    
    protected DownstreamMessage() { }
    
    
    public void ConstructFromLog(string senderCompId, MessageType messageType, long sessionId, long sequenceNumber, string payload)
    {
        SenderCompId = senderCompId;
        MessageType = messageType;
        SessionId = sessionId;
        SequenceNumber = sequenceNumber;
        Payload = payload;
    }

    // public static DownstreamMessage CreateSessionStartMessage(string senderCompId, string topicName, long sessionId, long sequenceNumber, string payload) => 
    //     new(senderCompId, topicName, MessageType.SessionStart, sessionId, sequenceNumber, payload);
    //
    // public static DownstreamMessage CreateDataMessage(string senderCompId, string topicName, long sessionId, long sequence, string payload) =>
    //     new(senderCompId, topicName, MessageType.DataMessage, sessionId, sequence, payload);
    //
    // public static DownstreamMessage CreateResponseMessage(string senderCompId, string payload) =>
    //     new(senderCompId, ResponsesTopicName, MessageType.DataMessage, 0, 0, payload);

    public override string ToString()
    {
        return $"{nameof(SenderCompId)}: {SenderCompId}, {nameof(TopicName)}: {TopicName}, {nameof(MessageType)}: {MessageType}, {nameof(SessionId)}: {SessionId}, {nameof(SequenceNumber)}: {SequenceNumber}, {nameof(SendingTimestamp)}: {SendingTimestamp}";
    }
}