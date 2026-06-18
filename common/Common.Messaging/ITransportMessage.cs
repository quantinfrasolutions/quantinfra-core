namespace QuantInfra.Common.Messaging;

public interface ITransportMessage
{
    string SenderCompId { get; }
    MessageType MessageType { get; }
    long SessionId { get; }
    long SequenceNumber { get; }
    long SendingTimestamp { get; }
    string Payload { get; }
    
    void ConstructFromLog(string senderCompId, MessageType messageType, long sessionId, long sequenceNumber,
        string payload);
}