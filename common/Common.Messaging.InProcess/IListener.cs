namespace QuantInfra.Common.Messaging.InProcess;

public interface IListener
{
    void ReceiveMessage(ITransportMessage message, string? topicName = null);
}