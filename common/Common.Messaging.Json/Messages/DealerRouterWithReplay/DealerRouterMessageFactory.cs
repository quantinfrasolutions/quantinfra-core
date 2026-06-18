namespace QuantInfra.Common.Messaging.Json.Messages.DealerRouterWithReplay;

public class DealerRouterMessageFactory(string senderCompId, JsonMessageFactory messageFactory) : 
    QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay.IDealerRouterMessageFactory
{
    public QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay.DownstreamMessage CreateSessionStartMessage(long sessionId) => 
        new DownstreamMessage(senderCompId, MessageType.SessionStart, sessionId, 0, null);

    public QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay.DownstreamMessage CreateDataMessage(long sessionId, long sequence, object content) =>
        new DownstreamMessage(senderCompId, MessageType.DataMessage, sessionId, sequence, messageFactory.SerializeAsString(content));

    public QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay.DownstreamMessage CreateDataMessage(string senderCompIdOverride, long sessionId, long sequence, object content) =>
        new DownstreamMessage(senderCompIdOverride, MessageType.DataMessage, sessionId, sequence, messageFactory.SerializeAsString(content));

    public QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay.DownstreamMessage CreateSequenceResetMessage(long sessionId, long sequence) =>
        new DownstreamMessage(senderCompId, MessageType.SequenceReset, sessionId, sequence, null);
}