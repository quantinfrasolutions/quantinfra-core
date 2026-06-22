using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("Tests.Integration.MessagingPatterns")]

namespace QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;

public class DownstreamMessage : ITransportMessage
{
    public string SenderCompId { get; internal protected set; }
    public MessageType MessageType { get; protected set; }
    public long SessionId { get; protected set; }
    public long SequenceNumber { get; protected set; }
    public long SendingTimestamp { get;protected set; }
    public string? Payload { get; protected set; }
    

    protected DownstreamMessage(MessageType messageType, long sessionId, long sequenceNumber, string? payload)
    {
        MessageType = messageType;
        SessionId = sessionId;
        SequenceNumber = sequenceNumber;
        Payload = payload;
    }

    protected DownstreamMessage(DownstreamMessage message)
    {
        SenderCompId = message.SenderCompId;
        MessageType = message.MessageType;
        SessionId = message.SessionId;
        SequenceNumber = message.SequenceNumber;
        Payload = message.Payload;
    }
    
    protected DownstreamMessage() { }
    
    
    public void ConstructFromLog(string senderCompId, MessageType messageType, long sessionId, long sequenceNumber,
        string payload)
    {
        SenderCompId = senderCompId;
        MessageType = messageType;
        SessionId = sessionId;
        SequenceNumber = sequenceNumber;
        Payload = payload;
    }

    public void LogSendingTime(long timestamp)
    {
        SendingTimestamp = timestamp;
    }
}