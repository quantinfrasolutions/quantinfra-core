using System.Text.Json;
using NodaTime;

namespace QuantInfra.Common.Messaging;

public class MockMessageFactory : IMessageFactory
{
    public IMessage? WrapObject(object o)
    {
        throw new System.NotImplementedException();
    }

    public IMessage? CreateReceivedMessage(object o, Instant receiveTime)
    {
        throw new System.NotImplementedException();
    }

    public string ContentType { get; }
    public string SerializeAsString(object value)
    {
        return JsonSerializer.Serialize(value);
    }

    public object? Parse(string? payload)
    {
        return JsonSerializer.Deserialize<MockMessage>(payload);
    }
}