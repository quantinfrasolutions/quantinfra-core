namespace QuantInfra.Common.Messaging;

public interface ITransport<TMessage>
{
    void SendMessage(TMessage message);
}