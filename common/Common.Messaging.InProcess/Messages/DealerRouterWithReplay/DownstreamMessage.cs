namespace QuantInfra.Common.Messaging.InProcess.Messages.DealerRouterWithReplay;

public class DownstreamMessage : QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay.DownstreamMessage,
    IInProcessMessage
{
    public DownstreamMessage() { } // Required for instantiating messages from WAL using reflection
    
    internal DownstreamMessage(string senderCompId, MessageType messageType, long sessionId, long sequenceNumber, object? data) 
        : base(messageType, sessionId, sequenceNumber, null)
    {
        SenderCompId = senderCompId;
        Data = data;
    }


    public new string? Payload { get => base.Payload; set => base.Payload = value; }
    public object? Data { get; set; }
}