namespace QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;

public interface IDealerRouterMessageFactory
{
    DownstreamMessage CreateSessionStartMessage(long sessionId);
    DownstreamMessage CreateDataMessage(long sessionId, long sequence, object content);
    DownstreamMessage CreateDataMessage(string senderCompId, long sessionId, long sequence, object content);
    DownstreamMessage CreateSequenceResetMessage(long sessionId, long sequence);
}