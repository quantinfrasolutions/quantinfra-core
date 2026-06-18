namespace QuantInfra.Common.Messaging.InProcess;

public class TransportMessage : ITransportMessage, IInProcessMessage
{
    public TransportMessage() { }
    
    public TransportMessage(string senderCompId, MessageType messageType, long sessionId, long sequenceNumber, long sendingTimestamp, object data)
    {
        SenderCompId = senderCompId;
        MessageType = messageType;
        SessionId = sessionId;
        SequenceNumber = sequenceNumber;
        SendingTimestamp = sendingTimestamp;
        Data = data;
    }

    public string SenderCompId { get; private set; }
    public MessageType MessageType { get; private set; }
    public long SessionId { get; private set; }
    public long SequenceNumber { get; private set; }
    public long SendingTimestamp { get; private set; } = 0;
    public string Payload { get; set; }
    public object? Data { get; set; }

    public void ConstructFromLog(string senderCompId, MessageType messageType, long sessionId, long sequenceNumber,
        string payload)
    {
        SenderCompId = senderCompId;
        MessageType = messageType;
        SessionId = sessionId;
        SequenceNumber = sequenceNumber;
        Payload = payload;
    }
}