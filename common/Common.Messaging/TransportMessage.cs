namespace QuantInfra.Common.Messaging;

public class TransportMessage : ITransportMessage
{
    public string SenderCompId { get; private set; }
    public MessageType MessageType { get; } = MessageType.DataMessage;
    public long SessionId { get; } = 0;
    public long SequenceNumber { get; } = 0;
    public long SendingTimestamp { get; } = 0;
    public string Payload { get; private set; }
    
    public TransportMessage() { }

    public TransportMessage(string senderCompId, string payload, long sendingTimestamp)
    {
        SenderCompId = senderCompId;
        Payload = payload;
        SendingTimestamp = sendingTimestamp;
    }

    public void ConstructFromLog(string senderCompId, MessageType messageType, long sessionId, long sequenceNumber,
        string payload)
    {
        SenderCompId = senderCompId;
        Payload = payload;
    }
}