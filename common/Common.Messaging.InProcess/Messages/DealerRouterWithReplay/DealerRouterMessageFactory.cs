namespace QuantInfra.Common.Messaging.InProcess.Messages.DealerRouterWithReplay;

public class DealerRouterMessageFactory(string senderCompId) : QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay.IDealerRouterMessageFactory
{
    public Patterns.DealerRouterWithReplay.DownstreamMessage CreateSessionStartMessage(long sessionId) =>
        new DownstreamMessage(senderCompId, MessageType.SessionStart, sessionId, 0, null);

    public Patterns.DealerRouterWithReplay.DownstreamMessage CreateDataMessage(long sessionId, long sequence, object content) =>
        new DownstreamMessage(senderCompId, MessageType.DataMessage, sessionId, sequence, content);

    public Patterns.DealerRouterWithReplay.DownstreamMessage CreateDataMessage(string senderCompId, long sessionId, long sequence, object content) =>
        new DownstreamMessage(senderCompId, MessageType.DataMessage, sessionId, sequence, content);

    public Patterns.DealerRouterWithReplay.DownstreamMessage CreateSequenceResetMessage(long sessionId, long sequence) =>
        new DownstreamMessage(senderCompId, MessageType.SequenceReset, sessionId, sequence, null);
    
    public object? Parse(Patterns.DealerRouterWithReplay.DownstreamMessage msg)
    {
        if (msg is not DownstreamMessage message) return null;
        return message.Data;
    }
}