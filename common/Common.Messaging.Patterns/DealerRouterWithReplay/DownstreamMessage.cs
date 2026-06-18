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

    // public static DownstreamMessage CreateSessionStartMessage(long sessionId) => 
    //     new(MessageType.SessionStart, sessionId, 0, null);
    //
    // public static DownstreamMessage CreateDataMessage(long sessionId, long sequence, string payload) =>
    //     new(MessageType.DataMessage, sessionId, sequence, payload);
    //
    // public static DownstreamMessage CreateDataMessage(string senderCompId, long sessionId, long sequence, string payload) =>
    //     new(MessageType.DataMessage, sessionId, sequence, payload) { SenderCompId = senderCompId };
    //
    // public static DownstreamMessage CreateSequenceResetMessage(long sessionId, long sequence) =>
    //     new(MessageType.SequenceReset, sessionId, sequence, null);
}