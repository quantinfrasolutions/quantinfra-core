using QuantInfra.Common.Messaging;

namespace QuantInfra.Tests.Mocks;

public class MockTransport<TMessage> : ITransport<TMessage>
{
    public List<TMessage> SentMessages { get; } = new();
    public void SendMessage(TMessage message) => SentMessages.Add(message);
}