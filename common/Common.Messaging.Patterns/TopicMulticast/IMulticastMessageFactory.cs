namespace QuantInfra.Common.Messaging.Patterns.TopicMulticast;

public interface IMulticastMessageFactory
{
    DownstreamMessage CreateSessionStartMessage(string senderCompId, string topicName, long sessionId, long sequenceNumber, object? content);
    DownstreamMessage CreateDataMessage(string senderCompId, string topicName, long sessionId, long sequence, object content);
    DownstreamMessage CreateResponseMessage(string senderCompId, object content);
    object? Parse(DownstreamMessage msg);
}