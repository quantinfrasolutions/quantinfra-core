namespace QuantInfra.Common.Messaging.Patterns.TopicMulticast;

public interface IRequestSnapshotMessageHandler
{
    void Handle(RequestSnapshotMessage message);
}