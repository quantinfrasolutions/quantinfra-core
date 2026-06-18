namespace QuantInfra.Common.Messaging.InProcess.Messages.TopicMulticast;

public class MulticastMessageFactory : QuantInfra.Common.Messaging.Patterns.TopicMulticast.IMulticastMessageFactory
{
    public Patterns.TopicMulticast.DownstreamMessage CreateSessionStartMessage(string senderCompId, string topicName, long sessionId, long sequenceNumber, object? content) =>
        new DownstreamMessage(senderCompId, topicName, MessageType.SessionStart, sessionId, sequenceNumber, content);

    public Patterns.TopicMulticast.DownstreamMessage CreateDataMessage(string senderCompId, string topicName, long sessionId, long sequence, object content) =>
        new DownstreamMessage(senderCompId, topicName, MessageType.DataMessage, sessionId, sequence, content);

    public Patterns.TopicMulticast.DownstreamMessage CreateResponseMessage(string senderCompId, object content) =>
        new DownstreamMessage(senderCompId, Patterns.TopicMulticast.DownstreamMessage.ResponsesTopicName, MessageType.DataMessage, 0, 0, content);

    public object? Parse(Patterns.TopicMulticast.DownstreamMessage msg)
    {
        if (msg is not DownstreamMessage message) return null;
        return message.Data;
    }
}