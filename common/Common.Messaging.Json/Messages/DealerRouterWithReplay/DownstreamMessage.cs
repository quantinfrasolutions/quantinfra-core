namespace QuantInfra.Common.Messaging.Json.Messages.DealerRouterWithReplay;

public class DownstreamMessage : QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay.DownstreamMessage
{
    public DownstreamMessage() { } // Required for reconstructing messages from WAL
    internal DownstreamMessage(string senderCompId, MessageType messageType, long sessionId, long sequenceNumber, string? payload) 
        : base(messageType, sessionId, sequenceNumber, payload)
    {
        SenderCompId = senderCompId;
    }
}