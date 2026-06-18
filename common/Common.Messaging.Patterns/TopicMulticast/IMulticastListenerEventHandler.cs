namespace QuantInfra.Common.Messaging.Patterns.TopicMulticast;

public interface IMulticastListenerEventHandler
{
    void HandleIncomingMessage(DownstreamMessage message);
}