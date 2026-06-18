namespace QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;

public class ControlMessage
{
    protected ControlMessage() { }

    protected ControlMessage(string clientId, MessageType type, long sessionId, long sequence)
    {
        ClientId = clientId;
        MessageType = type;
        SessionId = sessionId;
        Sequence = sequence;
    }
        
    
    public string ClientId { get; protected init; }
    public MessageType MessageType { get; protected init; }
    public long SessionId { get; protected init; }
    public long Sequence { get; protected init; }


    public static ControlMessage FillGap(string senderCompId, long sessionId, long expectedSequence) =>
        new(senderCompId, MessageType.FillGap, sessionId, expectedSequence);
}